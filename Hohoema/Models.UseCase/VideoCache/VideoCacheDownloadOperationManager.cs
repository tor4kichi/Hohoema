using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.VideoCache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.Messaging;
using Hohoema.Models.Domain.Player.Video;
using Microsoft.Toolkit.Uwp.Connectivity;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using Hohoema.Presentation.Services;
using I18NPortable;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Application;
using Microsoft.AppCenter.Crashes;

namespace Hohoema.Models.UseCase.VideoCache
{
    using TNC = ToastNotificationConstants;

    public sealed class VideoCacheDownloadOperationManager
    {
        public static readonly TimeSpan PROGRESS_UPDATE_INTERVAL = TimeSpan.FromSeconds(1);
        public const int MAX_DOWNLOAD_LINE_ = 1;

        private static readonly ToastNotifierCompat _notifier = ToastNotificationManagerCompat.CreateToastNotifier();


        private readonly VideoCacheManager _videoCacheManager;
        private readonly NicoVideoSessionOwnershipManager _nicoVideoSessionOwnershipManager;
        private readonly VideoCacheSettings _videoCacheSettings;
        private readonly NicoVideoCacheRepository _nicoVideoCacheRepository;
        private readonly NotificationService _notificationService;
        private readonly IMessenger _messenger;

        private readonly AsyncLock _downloadTsakUpdateLock = new AsyncLock();
        private readonly AsyncLock _runningFlagUpdateLock = new AsyncLock();
        private readonly List<ValueTask<bool>> _downloadTasks = new List<ValueTask<bool>>();
        private bool _isRunning = false;

        private DateTime _nextProgressShowTime = DateTime.Now;

        private bool _stopDownloadTaskWithDisallowMeteredNetworkDownload = false;
        private bool _stopDownloadTaskWithChangingSaveFolder = false;
        public bool IsAllowDownload 
        {
            get => _videoCacheSettings.IsAllowDownload;
            private set => _videoCacheSettings.IsAllowDownload = value;
        }

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public VideoCacheDownloadOperationManager(
            VideoCacheManager videoCacheManager,
            NicoVideoSessionOwnershipManager nicoVideoSessionOwnershipManager,
            VideoCacheSettings videoCacheSettings,
            NicoVideoCacheRepository nicoVideoCacheRepository,
            NotificationService notificationService
            )
        {
            _videoCacheManager = videoCacheManager;
            _nicoVideoSessionOwnershipManager = nicoVideoSessionOwnershipManager;
            _videoCacheSettings = videoCacheSettings;
            _nicoVideoCacheRepository = nicoVideoCacheRepository;
            _notificationService = notificationService;
            _messenger = WeakReferenceMessenger.Default;


            _videoCacheManager.Requested += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Requested: Id= {e.VideoId}, RequestQuality= {e.RequestedQuality}");
                LaunchDownaloadOperationLoop();
                TriggerVideoCacheStatusChanged(e.VideoId);

                _notificationService.ShowLiteInAppNotification_Success("CacheVideo_Notification_RequestAdded".Translate());
            };

            _videoCacheManager.Started += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Started: Id= {e.Item.VideoId}, RequestQuality= {e.Item.RequestedVideoQuality}, DownloadQuality= {e.Item.DownloadedVideoQuality}");
                TriggerVideoCacheStatusChanged(e.Item.VideoId);

                _notificationService.ShowLiteInAppNotification($"{"CacheVideo_Notification_DownloadStarted".Translate()}\n{e.Item.Title}", symbol: Windows.UI.Xaml.Controls.Symbol.Download);

                StartBackgroundCacheProgressToast(e.Item);
            };

            _videoCacheManager.Progress += (s, e) => 
            {
                if (_nextProgressShowTime < DateTime.Now)
                {
                    Debug.WriteLine($"[VideoCache] Progress: Id= {e.Item.VideoId}, Progress= {e.Item.GetProgressNormalized():P}");
                    _nextProgressShowTime = DateTime.Now + PROGRESS_UPDATE_INTERVAL;

                    TriggerVideoCacheProgressChanged(e.Item);

                    UpdateBackgroundCacheProgressToast(e.Item);
                }
            };

            _videoCacheManager.Completed += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Completed: Id= {e.Item.VideoId}");
                TriggerVideoCacheStatusChanged(e.Item.VideoId);

                if (e.Item.Status == VideoCacheStatus.Completed)
                { 
                    _notificationService.ShowLiteInAppNotification_Success($"{"CacheVideo_Notification_Completed".Translate()}\n{e.Item.Title}");

                    // 完了をトースト通知で知らせる
                    PopCacheCompletedToast(e.Item);
                    StopBackgroundCacheProgressToast();
                }
            };

            _videoCacheManager.Canceled += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Canceled: Id= {e.VideoId}");
                TriggerVideoCacheStatusChanged(e.VideoId);

                _notificationService.ShowLiteInAppNotification_Success($"{"CacheVideo_Notification_RequestRemoved".Translate()}\n{e.VideoId}");
            };

            _videoCacheManager.Failed += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Failed: Id= {e.Item.VideoId}, FailedReason= {e.VideoCacheDownloadOperationCreationFailedReason}");
                TriggerVideoCacheStatusChanged(e.Item.VideoId);

                _notificationService.ShowLiteInAppNotification_Success($"{"CacheVideo_Notification_Failed".Translate()}\n{e.Item.Title}");

                PopCacheFailedToast(e.Item);
                StopBackgroundCacheProgressToast();

                // 失敗してても次のキャッシュDLが可能な失敗の場合はDLループを起動する
                if (e.VideoCacheDownloadOperationCreationFailedReason 
                    is VideoCacheDownloadOperationFailedReason.CanNotCacheEncryptedContent
                    or VideoCacheDownloadOperationFailedReason.RequirePermission_Admission
                    or VideoCacheDownloadOperationFailedReason.RequirePermission_Ppv
                    or VideoCacheDownloadOperationFailedReason.RequirePermission_Premium
                    or VideoCacheDownloadOperationFailedReason.VideoDeleteFromServer
                )
                {
                    LaunchDownaloadOperationLoop();
                }
            };

            _videoCacheManager.Paused += (s, e) =>
            {
                Debug.WriteLine($"[VideoCache] Paused: Id= {e.Item.VideoId}");
                TriggerVideoCacheStatusChanged(e.Item.VideoId);

                _notificationService.ShowLiteInAppNotification($"{"CacheVideo_Notification_Paused".Translate()}\n{e.Item.Title}", symbol: Windows.UI.Xaml.Controls.Symbol.Pause);

                UpdateBackgroundCacheProgressToast(e.Item, isPause: true);
            };


            App.Current.Suspending += async (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] App Suspending.");
                var defferl = e.SuspendingOperation.GetDeferral();
                try
                {
                    StopBackgroundCacheProgressToast();

                    while (_stopDownloadTaskWithChangingSaveFolder)
                    {
                        await Task.Delay(1);
                    }

                    await _videoCacheManager.PauseAllDownloadOperationAsync();
                }
                catch (Exception ex) { ErrorTrackingManager.TrackError(ex); }
                finally
                {
                    defferl.Complete();
                }
            };

            App.Current.Resuming += (s, e) =>
            {
                Debug.WriteLine($"[VideoCache] App Resuming.");
                try
                {
                    LaunchDownaloadOperationLoop();
                }
                catch (Exception ex) { ErrorTrackingManager.TrackError(ex); }
            };

            _nicoVideoSessionOwnershipManager.AvairableOwnership += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] AvairableOwnership");

                LaunchDownaloadOperationLoop();
            };

            new[]
            {
                Observable.FromEventPattern(
                    h => NetworkHelper.Instance.NetworkChanged += h,
                    h => NetworkHelper.Instance.NetworkChanged -= h
                    ).ToUnit(),
                _videoCacheSettings.ObserveProperty(x => x.IsAllowDownloadOnMeteredNetwork).ToUnit()
            }
            .Merge()
            .Subscribe(_ => UpdateConnectionType())
            .AddTo(_disposables);

            WeakReferenceMessenger.Default.Register<Events.StartCacheSaveFolderChangingAsyncRequestMessage>(this, (r, m) => 
            {
                async Task<long> Shutdown()
                {
                    await ShutdownDownloadOperationLoop();
                    return 0;
                }

                _stopDownloadTaskWithChangingSaveFolder = true;
                m.Reply(Shutdown());
            });

            WeakReferenceMessenger.Default.Register<Events.EndCacheSaveFolderChangingMessage>(this, (r, m) =>
            {
                _stopDownloadTaskWithChangingSaveFolder = false;
                LaunchDownaloadOperationLoop();
            });

            // Initialize
            UpdateConnectionType();
        }





        public void SuspendDownload()
        {
            IsAllowDownload = false;
            ShutdownDownloadOperationLoop();
        }

        public void ResumeDownload()
        {
            IsAllowDownload = true;
            LaunchDownaloadOperationLoop();
        }




        private bool CanLaunchDownloadOperationLoop()
        {
            if (!InternetConnection.IsInternet())
            {
                return false;
            }
            if (_stopDownloadTaskWithDisallowMeteredNetworkDownload)
            {
                Debug.WriteLine("CacheDL Looping: disallow download with metered network, loop exit.");
                return false;
            }
            else if (_stopDownloadTaskWithChangingSaveFolder)
            {
                Debug.WriteLine("CacheDL Looping: stopping download from save folder changing, loop exit.");
                return false;
            }
            else if (IsAllowDownload is false)
            {
                Debug.WriteLine("CacheDL Looping: stopping download from user action, loop exit.");
                return false;
            }

            return true;
        }

        private async void LaunchDownaloadOperationLoop()
        {
            using (await _runningFlagUpdateLock.LockAsync())
            {
                if (_isRunning) 
                {
                    Debug.WriteLine("CacheDL Looping: already running, loop skiped.");
                    return; 
                }

                _isRunning = true;
            }

            Debug.WriteLine("CacheDL Looping: loop started.");

            try
            {
                await DownloadLoopAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("CacheDL Looping: exit with Exception.");
                Debug.WriteLine(e.ToString());
            }

            using (await _runningFlagUpdateLock.LockAsync())
            {
                _isRunning = false;
            }

            Debug.WriteLine("CacheDL Looping: loop completed.");
        }

        private async Task DownloadLoopAsync()
        {
            using (await _downloadTsakUpdateLock.LockAsync())
            {
                if (!CanLaunchDownloadOperationLoop())
                {
                    return;
                }

                foreach (var _ in Enumerable.Range(_downloadTasks.Count, Math.Max(0, MAX_DOWNLOAD_LINE_ - _downloadTasks.Count)))
                {
                    _downloadTasks.Add(DownloadNextAsync());

                    Debug.WriteLine("CacheDL Looping: add task");
                }

                while (_downloadTasks.Count > 0)
                {
                    (var index, var result) = await ValueTaskSupplement.ValueTaskEx.WhenAny(_downloadTasks);

                    var doneTask = _downloadTasks[index];
                    _downloadTasks.Remove(doneTask);

                    Debug.WriteLine("CacheDL Looping: remove task");

                    if (result
                        && CanLaunchDownloadOperationLoop()
                        && _videoCacheManager.HasPendingOrPausingVideoCacheItem()
                        )
                    {
                        _downloadTasks.Add(DownloadNextAsync());

                        Debug.WriteLine("CacheDL Looping: add task");
                    }
                }
            }
        }



        private async ValueTask<bool> DownloadNextAsync()
        {
            try
            {
                var result = await _videoCacheManager.PrepareNextCacheDownloadingTaskAsync();
                if (result.IsSuccess is false)
                {
                    return false;
                }

                await result.DownloadAsync();
            }
            catch (Exception e)
            {
                Debug.WriteLine("CacheDL Looping: has exception.");
                Debug.WriteLine(e.ToString());
                return false;
            }

            return true;
        }


        private Task ShutdownDownloadOperationLoop()
        {
            return _videoCacheManager.PauseAllDownloadOperationAsync();
        }


        private void UpdateConnectionType()
        {
            if (_videoCacheSettings.IsAllowDownloadOnMeteredNetwork is false
                && NetworkHelper.Instance.ConnectionInformation.ConnectionType == ConnectionType.Data
                )
            {
                _stopDownloadTaskWithDisallowMeteredNetworkDownload = true;
            }
            else
            {
                _stopDownloadTaskWithDisallowMeteredNetworkDownload = false;
            }

            if (_stopDownloadTaskWithDisallowMeteredNetworkDownload
                || !NetworkHelper.Instance.ConnectionInformation.IsInternetAvailable
                )
            {
                ShutdownDownloadOperationLoop();
            }
            else
            {
                LaunchDownaloadOperationLoop();
            }
        }


        private void TriggerVideoCacheStatusChanged(string videoId)
        {
            var item = _videoCacheManager.GetVideoCache(videoId);
            var message = new Events.VideoCacheStatusChangedMessage((videoId, item?.Status, item));
            _messenger.Send<Events.VideoCacheStatusChangedMessage, string>(message, videoId);
            _messenger.Send<Events.VideoCacheStatusChangedMessage>(message);
        }

        private void TriggerVideoCacheProgressChanged(VideoCacheItem item)
        {
            var message = new Events.VideoCacheProgressChangedMessage(item);
            _messenger.Send<Events.VideoCacheProgressChangedMessage, string>(message, item.VideoId);
            _messenger.Send<Events.VideoCacheProgressChangedMessage>(message);
        }




        #region Toast Notification

        private const string HohoemaCacheToastGroupId = "HohoemaCache";
        private const string CacheProgressToastTag = "HohoemaProgress";
        private uint _progressSequenceNumber = 0;
        private ToastContent _progressToastContent = null;
        private Windows.UI.Notifications.ToastNotification _progressToastNotification;

        private void PopCacheCompletedToast(VideoCacheItem item)
        {
            var video = _nicoVideoCacheRepository.Get(item.VideoId);
            var toastContentBuilder = new ToastContentBuilder()
                .SetToastScenario(ToastScenario.Default)
                .AddText("CacheVideo_ToastNotification_Completed".Translate())
                .AddText(item.Title)
                .AddButton(new ToastButton()
                    .SetContent("CacheVideo_ToastNotification_PlayVideo".Translate())
                    .AddArgument(TNC.ToastArgumentKey_Action, TNC.ToastArgumentValue_Action_Play)
                    .AddArgument(TNC.ToastArgumentKey_Id, item.VideoId)
                    .SetHintActionId(TNC.ToastArgumentValue_Action_Play)
                    .SetBackgroundActivation())
                .AddButton(new ToastButton()
                    .SetContent("CacheVideo_ToastNotification_CloseToast".Translate())
                    .SetDismissActivation());

            if (video?.ThumbnailUrl != null)
            {
                toastContentBuilder.AddInlineImage(new Uri(video.ThumbnailUrl), hintRemoveMargin: true);                
            }

            toastContentBuilder.Show();
        }


        private void StartBackgroundCacheProgressToast(VideoCacheItem item)
        {
            StopBackgroundCacheProgressToast();

            _progressSequenceNumber = 0;

            var toastContentBuilder = new ToastContentBuilder()
                .SetToastScenario(ToastScenario.Reminder)
                .AddVisualChild(new AdaptiveProgressBar()
                {
                    Title = item.Title,
                    Value = new BindableProgressBarValue(TNC.ProgressBarBindableValueKey_ProgressValue),
                    ValueStringOverride = new BindableString(TNC.ProgressBarBindableValueKey_ProgressValueOverrideString),
                    Status = new BindableString(TNC.ProgressBarBindableValueKey_ProgressStatus)
                })
                ;

            _progressToastContent = toastContentBuilder.GetToastContent();

            _progressToastNotification = new ToastNotification(toastContentBuilder.GetXml());
            _progressToastNotification.Tag = CacheProgressToastTag;
            _progressToastNotification.Group = HohoemaCacheToastGroupId;
            _progressToastNotification.Failed += _progressToastNotification_Failed;
            _progressToastNotification.SuppressPopup = true;
            _progressToastNotification.Data = new NotificationData();
            _progressToastNotification.Data.Values[TNC.ProgressBarBindableValueKey_ProgressValue] = ((double)item.GetProgressNormalized()).ToString();
            _progressToastNotification.Data.Values[TNC.ProgressBarBindableValueKey_ProgressValueOverrideString] = Math.Floor(item.GetProgressNormalized() * 100).ToString("F0") + "%";
            _progressToastNotification.Data.Values[TNC.ProgressBarBindableValueKey_ProgressStatus] = "CacheVideo_ToastNotification_Downloading".Translate();
            _progressToastNotification.Data.SequenceNumber = _progressSequenceNumber++;
            _notifier.Show(_progressToastNotification);
        }

        private void UpdateBackgroundCacheProgressToast(VideoCacheItem item, bool isPause = false)
        {
            if (_progressToastContent == null)
            {
                return;
            }

            var data = new Dictionary<string, string>
            {
                { TNC.ProgressBarBindableValueKey_ProgressValue, ((double)item.GetProgressNormalized()).ToString() },
                { TNC.ProgressBarBindableValueKey_ProgressValueOverrideString, Math.Floor(item.GetProgressNormalized() * 100).ToString("F0") + "%" },
                { TNC.ProgressBarBindableValueKey_ProgressStatus, !isPause ? "CacheVideo_ToastNotification_Downloading".Translate() : "CacheVideo_ToastNotification_DownloadPausing".Translate() },
            };

            var result = _notifier.Update(new NotificationData(data, _progressSequenceNumber++), CacheProgressToastTag, HohoemaCacheToastGroupId);
            if (result != Windows.UI.Notifications.NotificationUpdateResult.Succeeded)
            {
                _progressToastContent = null;
                _progressToastNotification = null;
            }
        }

        private void StopBackgroundCacheProgressToast()
        {
            if (_progressToastNotification == null) { return; }

            _notifier.Hide(_progressToastNotification);
            _progressToastNotification.Failed -= _progressToastNotification_Failed;
            _progressToastContent = null;
            _progressToastNotification = null;
        }

        private void _progressToastNotification_Failed(Windows.UI.Notifications.ToastNotification sender, Windows.UI.Notifications.ToastFailedEventArgs args)
        {
            _progressToastContent = null;
            _progressToastNotification = null;
            sender.Failed -= _progressToastNotification_Failed;
        }


        private void PopCacheFailedToast(VideoCacheItem item)
        {
            new ToastContentBuilder()
                .SetToastScenario(ToastScenario.Reminder)
                .AddText("CacheVideo_ToastNotification_Failed".Translate())
                .AddText(item.Title)
                .AddText(item.FailedReason.Translate())
                .AddButton(new ToastButton()
                    .SetContent("CacheVideo_ToastNotification_CacheDelete".Translate())
                    .AddArgument(TNC.ToastArgumentKey_Action, TNC.ToastArgumentValue_Action_Delete)
                    .AddArgument(TNC.ToastArgumentKey_Id, item.VideoId)
                    .SetBackgroundActivation()
                    .SetHintActionId(TNC.ToastArgumentValue_Action_Delete))
                .AddButton(new ToastButton()
                    .SetContent("CacheVideo_ToastNotification_CloseToast".Translate())
                    .SetDismissActivation())
                .Show();
        }


        #endregion Toast Notification


    }
}

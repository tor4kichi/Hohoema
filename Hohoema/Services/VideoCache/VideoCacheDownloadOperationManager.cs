#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Text;
using Hohoema.Helpers;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player.Video;
using Hohoema.Models.VideoCache;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Connectivity;
using Microsoft.Toolkit.Uwp.Notifications;
using NiconicoToolkit.Video;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using ZLogger;

namespace Hohoema.Services.VideoCache;

using TNC = ToastNotificationConstants;

public sealed class VideoCacheDownloadOperationManager
{
    public static readonly TimeSpan PROGRESS_UPDATE_INTERVAL = TimeSpan.FromSeconds(1);
    public const int MAX_DOWNLOAD_LINE_ = 1;

    private static readonly ToastNotifierCompat _notifier = ToastNotificationManagerCompat.CreateToastNotifier();
    private readonly ILogger<VideoCacheDownloadOperationManager> _logger;
    private readonly VideoCacheManager _videoCacheManager;
    private readonly NicoVideoSessionOwnershipManager _nicoVideoSessionOwnershipManager;
    private readonly VideoCacheSettings _videoCacheSettings;
    private readonly NotificationService _notificationService;
    private readonly NicoVideoProvider _nicoVideoProvider;
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
        ILoggerFactory loggerFactory,
        VideoCacheManager videoCacheManager,
        NicoVideoSessionOwnershipManager nicoVideoSessionOwnershipManager,
        VideoCacheSettings videoCacheSettings,
        NotificationService notificationService,
        NicoVideoProvider nicoVideoProvider
        )
    {
        _logger = loggerFactory.CreateLogger<VideoCacheDownloadOperationManager>();
        _videoCacheManager = videoCacheManager;
        _nicoVideoSessionOwnershipManager = nicoVideoSessionOwnershipManager;
        _videoCacheSettings = videoCacheSettings;
        _notificationService = notificationService;
        _nicoVideoProvider = nicoVideoProvider;
        _messenger = WeakReferenceMessenger.Default;


        _videoCacheManager.Requested += (s, e) => 
        {
            _logger.ZLogDebug("Requested: Id= {0}, RequestQuality= {1}", e.VideoId, e.RequestedQuality);
            LaunchDownaloadOperationLoop();
            TriggerVideoCacheStatusChanged(e.VideoId);

            _notificationService.ShowLiteInAppNotification_Success("CacheVideo_Notification_RequestAdded".Translate());
        };

        _videoCacheManager.Started += (s, e) => 
        {

            _logger.ZLogDebug("Started: Id= {0}, RequestQuality= {1}, DownloadQuality= {2}", e.Item.VideoId, e.Item.RequestedVideoQuality, e.Item.DownloadedVideoQuality);
            TriggerVideoCacheStatusChanged(e.Item.VideoId);

            _notificationService.ShowLiteInAppNotification(ZString.Format("{0}\n{1}", "CacheVideo_Notification_DownloadStarted".Translate(), e.Item.Title), symbol: Windows.UI.Xaml.Controls.Symbol.Download);

            StartBackgroundCacheProgressToast(e.Item);
        };

        _videoCacheManager.Progress += (s, e) => 
        {
            if (_nextProgressShowTime < DateTime.Now)
            {
                _logger.ZLogDebug("Progress: Id= {0}, Progress= {1:P}", e.Item.VideoId, e.Item.GetProgressNormalized());
                _nextProgressShowTime = DateTime.Now + PROGRESS_UPDATE_INTERVAL;

                TriggerVideoCacheProgressChanged(e.Item);

                UpdateBackgroundCacheProgressToast(e.Item);
            }
        };

        _videoCacheManager.Completed += (s, e) => 
        {
            _logger.ZLogDebug("Completed: Id= {0}", e.Item.VideoId);
            TriggerVideoCacheStatusChanged(e.Item.VideoId);

            if (e.Item.Status == VideoCacheStatus.Completed)
            { 
                _notificationService.ShowLiteInAppNotification_Success(ZString.Format("{0}\n{1}", "CacheVideo_Notification_Completed".Translate(), e.Item.Title));

                // 完了をトースト通知で知らせる
                PopCacheCompletedToast(e.Item);
                StopBackgroundCacheProgressToast();
            }
        };

        _videoCacheManager.Canceled += (s, e) => 
        {
            _logger.ZLogDebug("Canceled: Id= {0}", e.VideoId);
            TriggerVideoCacheStatusChanged(e.VideoId);

            _notificationService.ShowLiteInAppNotification_Success(ZString.Format("{0}\n{1}", "CacheVideo_Notification_RequestRemoved".Translate(), e.VideoId));
        };

        _videoCacheManager.Failed += (s, e) => 
        {
            _logger.ZLogDebug("Failed: Id= {0}, FailedReason= {1}", e.Item.VideoId, e.VideoCacheDownloadOperationCreationFailedReason);
            TriggerVideoCacheStatusChanged(e.Item.VideoId);

            _notificationService.ShowLiteInAppNotification_Success(ZString.Format("{0}\n{1}", "CacheVideo_Notification_Failed".Translate(), e.Item.Title));

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
            _logger.ZLogDebug("Paused: Id= {0}", e.Item.VideoId);
            TriggerVideoCacheStatusChanged(e.Item.VideoId);

            _notificationService.ShowLiteInAppNotification(ZString.Format("{0}\n{1}", "CacheVideo_Notification_Paused".Translate(), e.Item.Title), symbol: Windows.UI.Xaml.Controls.Symbol.Pause);

            UpdateBackgroundCacheProgressToast(e.Item, isPause: true);
        };


        App.Current.Suspending += async (s, e) => 
        {
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
            catch (Exception ex) 
            {
                _logger.ZLogError(ex, "Cache operation suspending failed.");
            }
            finally
            {
                defferl.Complete();
            }
        };

        App.Current.Resuming += (s, e) =>
        {
            try
            {
                LaunchDownaloadOperationLoop();
            }
            catch (Exception ex) 
            {
                _logger.ZLogError(ex, "Cache operation resuming failed.");
            }
        };

        _nicoVideoSessionOwnershipManager.AvairableOwnership += (s, e) => 
        {
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
            _logger.ZLogDebug("CacheDL Looping: disallow download with metered network, loop exit.");
            return false;
        }
        else if (_stopDownloadTaskWithChangingSaveFolder)
        {
            _logger.ZLogDebug("CacheDL Looping: stopping download from save folder changing, loop exit.");
            return false;
        }
        else if (IsAllowDownload is false)
        {
            _logger.ZLogDebug("CacheDL Looping: stopping download from user action, loop exit.");
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
                _logger.ZLogDebug("CacheDL Looping: already running, loop skiped.");
                return; 
            }

            _isRunning = true;
        }

        _logger.ZLogDebug("CacheDL Looping: loop started.");

        try
        {
            await DownloadLoopAsync();
        }
        catch (Exception e)
        {
            _logger.ZLogError(e,"CacheDL Looping: exit with Exception.");
        }

        using (await _runningFlagUpdateLock.LockAsync())
        {
            _isRunning = false;
        }

        _logger.ZLogDebug("CacheDL Looping: loop completed.");
    }

    private async Task DownloadLoopAsync()
    {
        using (await _downloadTsakUpdateLock.LockAsync())
        {
            if (!CanLaunchDownloadOperationLoop()
                || !_videoCacheManager.HasPendingOrPausingVideoCacheItem()
                )
            {
                return;
            }

            foreach (var _ in Enumerable.Range(_downloadTasks.Count, Math.Max(0, MAX_DOWNLOAD_LINE_ - _downloadTasks.Count)))
            {
                _downloadTasks.Add(DownloadNextAsync());

                _logger.ZLogDebug("CacheDL Looping: add task");
            }

            while (_downloadTasks.Count > 0)
            {
                (var index, var result) = await ValueTaskSupplement.ValueTaskEx.WhenAny(_downloadTasks);

                var doneTask = _downloadTasks[index];
                _downloadTasks.Remove(doneTask);

                _logger.ZLogDebug("CacheDL Looping: remove task");

                if (result
                    && CanLaunchDownloadOperationLoop()
                    && _videoCacheManager.HasPendingOrPausingVideoCacheItem()
                    )
                {
                    _downloadTasks.Add(DownloadNextAsync());

                    _logger.ZLogDebug("CacheDL Looping: add task");
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
            _logger.ZLogError(e, e.Message);
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


    private void TriggerVideoCacheStatusChanged(VideoId videoId)
    {
        var item = _videoCacheManager.GetVideoCache(videoId);
        var message = new Events.VideoCacheStatusChangedMessage((videoId, item?.Status, item));
        _messenger.Send(message, videoId);
        _messenger.Send(message);
    }

    private void TriggerVideoCacheProgressChanged(VideoCacheItem item)
    {
        var message = new Events.VideoCacheProgressChangedMessage(item);
        _messenger.Send(message, item.VideoId);
        _messenger.Send(message);
    }




    #region Toast Notification

    private const string HohoemaCacheToastGroupId = "HohoemaCache";
    private const string CacheProgressToastTag = "HohoemaProgress";
    private uint _progressSequenceNumber = 0;
    private ToastContent _progressToastContent = null;
    private Windows.UI.Notifications.ToastNotification _progressToastNotification;

    private async void PopCacheCompletedToast(VideoCacheItem item)
    {
        var thumbnail = await _nicoVideoProvider.ResolveThumbnailUrlAsync(item.VideoId);
        var toastContentBuilder = new ToastContentBuilder()
            .SetToastScenario(ToastScenario.Default)
            .AddText("CacheVideo_ToastNotification_Completed".Translate())
            .AddText(item.Title)
            .AddButton(new ToastButton("CacheVideo_ToastNotification_PlayVideo".Translate(), TNC.MakePlayVideoToastArguments(item.VideoId).ToString())
                .SetHintActionId(TNC.ToastArgumentValue_Action_PlayVideo)
                .SetBackgroundActivation())
            .AddButton(new ToastButton()
                .SetContent("CacheVideo_ToastNotification_CloseToast".Translate())
                .SetDismissActivation());

        if (thumbnail != null)
        {
            toastContentBuilder.AddInlineImage(new Uri(thumbnail), hintRemoveMargin: true);                
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
            .AddButton(new ToastButton("CacheVideo_ToastNotification_CacheDelete".Translate(), TNC.MakeDeleteCacheToastArguments(item.VideoId).ToString())
                .SetBackgroundActivation()
                .SetHintActionId(TNC.ToastArgumentValue_Action_DeleteCache))
            .AddButton(new ToastButton()
                .SetContent("CacheVideo_ToastNotification_CloseToast".Translate())
                .SetDismissActivation())
            .Show();
    }


    #endregion Toast Notification


}

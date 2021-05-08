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

namespace Hohoema.Models.UseCase.VideoCache
{
    public sealed class VideoCacheDownloadOperationManager
    {
        public static readonly TimeSpan PROGRESS_UPDATE_INTERVAL = TimeSpan.FromSeconds(0.5);
        public const int MAX_DOWNLOAD_LINE_ = 1;

        private readonly VideoCacheManager _videoCacheManager;
        private readonly NicoVideoSessionOwnershipManager _nicoVideoSessionOwnershipManager;
        private readonly VideoCacheSettings _videoCacheSettings;
        private readonly IMessenger _messenger;

        private AsyncLock _downloadTsakUpdateLock = new AsyncLock();
        private AsyncLock _runningFlagUpdateLock = new AsyncLock();
        private List<ValueTask<bool>> _downloadTasks = new List<ValueTask<bool>>();
        private bool _isRunning = false;

        private DateTime _nextProgressShowTime = DateTime.Now;

        private bool _notifyUsingMobileDataNetworkDownload = false;

        private bool _stopDownloadTaskWithDisallowMeteredNetworkDownload = false;
        private bool _stopDownloadTaskWithChangingSaveFolder = false;
        public bool IsAllowDownload 
        {
            get => _videoCacheSettings.IsAllowDownload;
            private set => _videoCacheSettings.IsAllowDownload = value;
        }

        CompositeDisposable _disposables = new CompositeDisposable();

        public VideoCacheDownloadOperationManager(
            VideoCacheManager videoCacheManager,
            NicoVideoSessionOwnershipManager nicoVideoSessionOwnershipManager,
            VideoCacheSettings videoCacheSettings
            )
        {
            _videoCacheManager = videoCacheManager;
            _nicoVideoSessionOwnershipManager = nicoVideoSessionOwnershipManager;
            _videoCacheSettings = videoCacheSettings;
            _messenger = WeakReferenceMessenger.Default;


            _videoCacheManager.Requested += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Requested: Id= {e.VideoId}, RequestQuality= {e.RequestedQuality}");
                LaunchDownaloadOperationLoop();
                TriggerVideoCacheStatusChanged(e.VideoId);
            };

            _videoCacheManager.Started += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Started: Id= {e.Item.VideoId}, RequestQuality= {e.Item.RequestedVideoQuality}, DownloadQuality= {e.Item.DownloadedVideoQuality}");
                TriggerVideoCacheStatusChanged(e.Item.VideoId);
            };

            _videoCacheManager.Progress += (s, e) => 
            {
                if (_nextProgressShowTime < DateTime.Now)
                {
                    Debug.WriteLine($"[VideoCache] Progress: Id= {e.Item.VideoId}, Progress= {e.Item.GetProgressNormalized():P}");
                    _nextProgressShowTime = DateTime.Now + PROGRESS_UPDATE_INTERVAL;

                    TriggerVideoCacheProgressChanged(e.Item);
                }
            };

            _videoCacheManager.Completed += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Completed: Id= {e.Item.VideoId}");
                TriggerVideoCacheStatusChanged(e.Item.VideoId);
            };

            _videoCacheManager.Canceled += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Canceled: Id= {e.VideoId}");
                TriggerVideoCacheStatusChanged(e.VideoId);
            };

            _videoCacheManager.Failed += (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] Failed: Id= {e.Item.VideoId}, FailedReason= {e.VideoCacheDownloadOperationCreationFailedReason}");
                TriggerVideoCacheStatusChanged(e.Item.VideoId);

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
            };


            App.Current.Suspending += async (s, e) => 
            {
                Debug.WriteLine($"[VideoCache] App Suspending.");
                var defferl = e.SuspendingOperation.GetDeferral();
                try
                {
                    while (_stopDownloadTaskWithChangingSaveFolder)
                    {
                        await Task.Delay(1);
                    }

                    await _videoCacheManager.PauseAllDownloadOperationAsync();
                }
                finally
                {
                    defferl.Complete();
                }
            };

            App.Current.Resuming += (s, e) =>
            {
                Debug.WriteLine($"[VideoCache] App Resuming.");
                LaunchDownaloadOperationLoop();
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

        async Task DownloadLoopAsync()
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

                if (_notifyUsingMobileDataNetworkDownload is true)
                {
                    _notifyUsingMobileDataNetworkDownload = false;
                    // TODO: 課金データ通信状況でダウンロードを開始したことを通知
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


        Task ShutdownDownloadOperationLoop()
        {
            return _videoCacheManager.PauseAllDownloadOperationAsync();
        }


        void UpdateConnectionType()
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

            if (_stopDownloadTaskWithDisallowMeteredNetworkDownload is false
                && NetworkHelper.Instance.ConnectionInformation.ConnectionCost?.NetworkCostType != Windows.Networking.Connectivity.NetworkCostType.Fixed)
            {
                // データ料金が発生する状況でのDLを開始する場合に通知を飛ばす
                _notifyUsingMobileDataNetworkDownload = true;
            }
            else
            {
                _notifyUsingMobileDataNetworkDownload = false;
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


        void TriggerVideoCacheStatusChanged(string videoId)
        {
            var item = _videoCacheManager.GetVideoCache(videoId);
            var message = new Events.VideoCacheStatusChangedMessage((videoId, item?.Status, item));
            _messenger.Send<Events.VideoCacheStatusChangedMessage, string>(message, videoId);
            _messenger.Send<Events.VideoCacheStatusChangedMessage>(message);
        }

        void TriggerVideoCacheProgressChanged(VideoCacheItem item)
        {
            var message = new Events.VideoCacheProgressChangedMessage(item);
            _messenger.Send<Events.VideoCacheProgressChangedMessage, string>(message, item.VideoId);
            _messenger.Send<Events.VideoCacheProgressChangedMessage>(message);
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
    }
}

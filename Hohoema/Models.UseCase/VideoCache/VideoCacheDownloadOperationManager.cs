using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.VideoCache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace Hohoema.Models.UseCase.VideoCache
{
    public sealed class VideoCacheDownloadOperationManager
    {
        private readonly VideoCacheManager _videoCacheManager;

        public VideoCacheDownloadOperationManager(VideoCacheManager videoCacheManager)
        {
            _videoCacheManager = videoCacheManager;
            _videoCacheManager.Requested += _videoCacheManager_Requested;
            _videoCacheManager.Started += _videoCacheManager_Started;
            _videoCacheManager.Progress += _videoCacheManager_Progress;
            _videoCacheManager.Completed += _videoCacheManager_Completed;
            _videoCacheManager.Canceled += _videoCacheManager_Canceled;
            _videoCacheManager.Failed += _videoCacheManager_Failed;
            _videoCacheManager.Paused += _videoCacheManager_Paused;

            App.Current.Suspending += ApplicationSuspending;
            App.Current.Resuming += ApplicationResuming;

            if (_videoCacheManager.HasPendingOrPausingVideoCacheItem())
            {
                LaunchDownaloadTask();
            }


            // TODO: 通信状態が回復した時や従量課金通信が変更されたタイミングに合わせてDL処理の再試行を掛ける
            _messenger = WeakReferenceMessenger.Default;
        }

        IMessenger _messenger;

        private void _videoCacheManager_Requested(object sender, VideoCacheRequestedEventArgs e)
        {
            Debug.WriteLine($"[VideoCache] Requested: Id= {e.VideoId}, RequestQuality= {e.RequestedQuality}");
            LaunchDownaloadTask();

            TriggerVideoCacheStatusChanged(e.VideoId);
        }

        private void _videoCacheManager_Started(object sender, VideoCacheStartedEventArgs e)
        {
            Debug.WriteLine($"[VideoCache] Started: Id= {e.Item.VideoId}, RequestQuality= {e.Item.RequestedVideoQuality}, DownloadQuality= {e.Item.DownloadedVideoQuality}");

            TriggerVideoCacheStatusChanged(e.Item.VideoId);
        }

        DateTime _nextProgressShowTime = DateTime.Now;

        private void _videoCacheManager_Progress(object sender, VideoCacheProgressEventArgs e)
        {
            var progress = e.Item.GetProgressNormalized();
            
            if (_nextProgressShowTime < DateTime.Now)
            {
                Debug.WriteLine($"[VideoCache] Progress: Id= {e.Item.VideoId}, Progress= {progress:P}");
                _nextProgressShowTime = DateTime.Now + TimeSpan.FromSeconds(1);

                TriggerVideoCacheProgressChanged(e.Item);
            }
        }

        private void _videoCacheManager_Completed(object sender, VideoCacheCompletedEventArgs e)
        {
            Debug.WriteLine($"[VideoCache] Completed: Id= {e.Item.VideoId}");

            TriggerVideoCacheStatusChanged(e.Item.VideoId);
        }

        private void _videoCacheManager_Canceled(object sender, VideoCacheCanceledEventArgs e)
        {
            Debug.WriteLine($"[VideoCache] Canceled: Id= {e.VideoId}");

            TriggerVideoCacheStatusChanged(e.VideoId);
        }

        private void _videoCacheManager_Failed(object sender, VideoCacheFailedEventArgs e)
        {
            Debug.WriteLine($"[VideoCache] Failed: Id= {e.Item.VideoId}, FailedReason= {e.VideoCacheDownloadOperationCreationFailedReason}");

            TriggerVideoCacheStatusChanged(e.Item.VideoId);
        }

        private void _videoCacheManager_Paused(object sender, VideoCachePausedEventArgs e)
        {
            Debug.WriteLine($"[VideoCache] Paused: Id= {e.Item.VideoId}");

            TriggerVideoCacheStatusChanged(e.Item.VideoId);
        }

        void TriggerVideoCacheStatusChanged(string videoId)
        {
            var item = _videoCacheManager.GetVideoCache(videoId);
            _messenger.Send<Events.VideoCacheStatusChangedMessage, string>(new((item?.Status, item)), videoId);
        }

        void TriggerVideoCacheProgressChanged(VideoCacheItem item)
        {
            _messenger.Send<Events.VideoCacheProgressChangedMessage, string>(new(item), item.VideoId);
        }

        private async void ApplicationSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var defferl = e.SuspendingOperation.GetDeferral();
            try
            {
                await _videoCacheManager.PauseAllDownloadOperationAsync();
            }
            finally
            {
                defferl.Complete();
            }
        }

        private void ApplicationResuming(object sender, object e)
        {
            LaunchDownaloadTask();
        }

        

        public static int DownloadLine { get; set; } = 1;

        private static List<ValueTask<bool>> _downloadTasks = new List<ValueTask<bool>>();
        private static AsyncLock _downloadTsakUpdateLock = new AsyncLock();

        private async void LaunchDownaloadTask()
        {
            Debug.WriteLine("CacheDL Looping: loop started.");

            using (await _downloadTsakUpdateLock.LockAsync())
            {
                foreach (var _ in Enumerable.Range(_downloadTasks.Count, Math.Max(0, DownloadLine - _downloadTasks.Count)))
                {
                    _downloadTasks.Add(DownloadNextAsync());

                    Debug.WriteLine("CacheDL Looping: add task");
                }
            }

            while (_downloadTasks.Count > 0)
            {
                (int index, bool result) = await ValueTaskSupplement.ValueTaskEx.WhenAny(_downloadTasks);

                using (await _downloadTsakUpdateLock.LockAsync())
                {
                    var doneTask = _downloadTasks[index];
                    _downloadTasks.Remove(doneTask);

                    Debug.WriteLine("CacheDL Looping: remove task");

                    if (result)
                    {
                        _downloadTasks.Add(DownloadNextAsync());

                        Debug.WriteLine("CacheDL Looping: add task");
                    }
                }
            }

            Debug.WriteLine("CacheDL Looping: loop completed.");
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
            finally
            {
                
            }

            return true;
        }
    }
}

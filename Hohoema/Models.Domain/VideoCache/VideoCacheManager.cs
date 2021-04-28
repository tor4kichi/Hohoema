using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.Player.Video;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Search;
using XTSSharp;
using Hohoema.Models.Domain.Niconico;

namespace Hohoema.Models.Domain.VideoCache
{
    public struct VideoCacheRequestedEventArgs
    {
        public string VideoId { get; set; }
        public NicoVideoCacheQuality RequestedQuality { get; set; }
    }

    public struct VideoCacheStartedEventArgs
    {
        public VideoCacheItem Item { get; set; }
    }

    public struct VideoCacheCanceledEventArgs
    {
        public string VideoId { get; set; }
    }


    public struct VideoCacheProgressEventArgs
    {
        public VideoCacheItem Item { get; set; }
    }

    public struct VideoCacheCompletedEventArgs
    {
        public VideoCacheItem Item { get; set; }
    }

    public struct VideoCacheFailedEventArgs
    {
        public VideoCacheItem Item { get; set; }
        public VideoCacheDownloadOperationFailedReason VideoCacheDownloadOperationCreationFailedReason { get; set; }
    }

    public struct VideoCachePausedEventArgs
    {
        public VideoCacheItem Item { get; set; }
    }

    public struct VideoCacheResumedEventArgs
    {
        public VideoCacheItem Item { get; set; }
    }



    public sealed class VideoCacheManager : IDisposable
    {
        public const string HohoemaVideoCacheExt = ".hohoema_cv";
        public const string HohoemaVideoCacheProgressExt = ".hohoema_cv.progress";


        public static Func<string, Task<string>> ResolveVideoFileNameWithoutExtFromVideoId { get; set; } = (id) => Task.FromResult(id);
        public static Func<Task<bool>> CheckUsageAuthorityAsync { get; set; } = () => Task.FromResult(true);

        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoSessionOwnershipManager _videoSessionOwnershipManager;
        private readonly VideoCacheItemRepository _videoCacheItemRepository;
        private readonly VideoCacheSettings _videoCacheSettings;

        public Xts Xts { get; }
        public StorageFolder VideoCacheFolder { get; set; }

        CompositeDisposable _disposables = new CompositeDisposable();
        Dictionary<string, IVideoCacheDownloadOperation> _currentDownloadOperations = new Dictionary<string, IVideoCacheDownloadOperation>();
        AsyncLock _updateLock = new AsyncLock();


        public event EventHandler<VideoCacheRequestedEventArgs> Requested;
        public event EventHandler<VideoCacheCanceledEventArgs> Canceled;
        public event EventHandler<VideoCacheStartedEventArgs> Started;
        public event EventHandler<VideoCacheProgressEventArgs> Progress;
        public event EventHandler<VideoCacheCompletedEventArgs> Completed;
        public event EventHandler<VideoCacheFailedEventArgs> Failed;        
        public event EventHandler<VideoCachePausedEventArgs> Paused;

        public VideoCacheManager(
            NiconicoSession niconicoSession, 
            NicoVideoSessionOwnershipManager videoSessionOwnershipManager,
            VideoCacheSettings videoCacheSettings,
            VideoCacheItemRepository videoCacheItemRepository
            )
        {
            _niconicoSession = niconicoSession;
            _videoSessionOwnershipManager = videoSessionOwnershipManager;
            _videoCacheSettings = videoCacheSettings;
            _videoCacheItemRepository = videoCacheItemRepository;
        }

        #region interface IDisposable

        public void Dispose()
        {
            ((IDisposable)_disposables).Dispose();
        }

        #endregion

        public long? GetMaxVideoCacheStorageSize()
        {
            return _videoCacheSettings.MaxVideoCacheStorageSize;
        }

        public long GetCacheStorageSize()
        {
            return _videoCacheSettings.CachedStorageSize = _videoCacheItemRepository.SumVideoCacheSize();
        }

        internal async Task<bool> CheckStorageCapacityLimitReachedAsync()
        {
            var storageSize = GetCacheStorageSize();
            var maxStroageSize = GetMaxVideoCacheStorageSize();
            if (maxStroageSize is not null and var maxSize)
            {
                return storageSize > maxSize;
            }
            else
            {
                return false;
            }
        }

       
        public async Task<MediaSource> GetCacheVideoMediaSource(VideoCacheItem item)
        {
            if (_niconicoSession.IsPremiumAccount is false) 
                throw new VideoCacheException("VideoCacheItem is can not play. premium account required.");
            if (item.IsCompleted is false) 
                throw new VideoCacheException("VideoCacheItem is can not play. not completed download the cache.");

            var file = await GetCacheVideoFileAsync(item.VideoId);
            var stream = new XtsStream(await file.OpenStreamForReadAsync(), Xts);
            var ms = MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "movie/mp4");
            return ms;
        }


        public async Task PushCacheRequestAsync(string videoId, NicoVideoCacheQuality requestCacheQuality)
        {
            using (await _updateLock.LockAsync())
            {
                var entity = _videoCacheItemRepository.GetVideoCache(videoId);
                VideoCacheStatus? prevStatus = entity?.Status;
                entity ??= new VideoCacheEntity() { VideoId = videoId, Status = VideoCacheStatus.Pending };

                switch (entity.Status)
                {
                    case VideoCacheStatus.Pending:

                        break;
                    case VideoCacheStatus.Downloading:
                        if (!_currentDownloadOperations.ContainsKey(videoId))
                        {
                            entity.Status = VideoCacheStatus.Pending;
                        }
                        break;
                    case VideoCacheStatus.DownloadPaused:
                        if (entity.DownloadedVideoQuality != requestCacheQuality)
                        {
                            entity.Status = VideoCacheStatus.Pending;
                        }
                        break;
                    case VideoCacheStatus.Completed:
                        if (entity.RequestedVideoQuality != entity.DownloadedVideoQuality)
                        {
                            entity.Status = VideoCacheStatus.Pending;
                        }
                        break;
                    case VideoCacheStatus.CompletedFromOldCache_NotEncypted:
                        entity.Status = VideoCacheStatus.Pending;
                        break;
                    case VideoCacheStatus.CompletedFromOldCache_Encrypted:
                        entity.Status = VideoCacheStatus.Pending;
                        break;
                    case VideoCacheStatus.Failed:
                        entity.Status = VideoCacheStatus.Pending;
                        break;
                }

                if (entity.Status is VideoCacheStatus.Pending)
                {
                    entity.FailedReason = VideoCacheDownloadOperationFailedReason.None;
                    entity.RequestedVideoQuality = requestCacheQuality;

                    if (prevStatus is not VideoCacheStatus.Pending)
                    {
                        entity.RequestedAt = DateTime.Now;
                    }

                    Requested?.Invoke(this, new VideoCacheRequestedEventArgs() { VideoId = videoId, RequestedQuality = requestCacheQuality });
                }

                _videoCacheItemRepository.UpdateVideoCache(entity);
            }
        }

        public async Task CancelCacheRequestAsync(string videoId)
        {
            using (await _updateLock.LockAsync())
            {
                // ダウンロードを中止
                if (_currentDownloadOperations.Remove(videoId, out var op))
                {
                    await op.StopAndDeleteDownloadedAsync();
                    (op as IDisposable).Dispose();
                }

                // ファイルとして削除
                if (await GetProgressCacheVideoFileAsync(videoId) is not null and var progressFile)
                {
                    await progressFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }

                if (await GetCacheVideoFileAsync(videoId) is not null and var completedFile)
                {
                    await completedFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }

                // ローカルDBから削除
                if (_videoCacheItemRepository.DeleteVideoCache(videoId) is true) 
                {
                    Canceled?.Invoke(this, new VideoCacheCanceledEventArgs() { VideoId = videoId });
                }
            }
        }

        private static VideoCacheItem EntityToItem(VideoCacheEntity entity)
        {
            return new VideoCacheItem(
                entity.VideoId,
                entity.RequestedVideoQuality,
                entity.DownloadedVideoQuality,
                entity.Status,
                entity.FailedReason,
                entity.RequestedAt,
                entity.TotalBytes,
                entity.ProgressBytes,
                entity.SortIndex
                );
        }

        private void UpdateVideoCacheEntity(VideoCacheItem item)
        {
            var entity = _videoCacheItemRepository.GetVideoCache(item.VideoId);

            if (entity == null) { return; }

            entity.RequestedVideoQuality = item.RequestedVideoQuality;
            entity.DownloadedVideoQuality = item.DownloadedVideoQuality;
            entity.Status = item.Status;
            entity.FailedReason = item.FailedReason;
            entity.RequestedAt = item.RequestedAt;
            entity.TotalBytes = item.TotalBytes;
            entity.ProgressBytes = item.ProgressBytes;
            entity.SortIndex = item.SortIndex;

            _videoCacheItemRepository.UpdateVideoCache(entity);
        }



        public bool HasPendingOrPausingVideoCacheItem()
        {
            return _videoCacheItemRepository.FindByStatus(VideoCacheStatus.DownloadPaused).Any()
                || _videoCacheItemRepository.FindByStatus(VideoCacheStatus.Pending).Any()
                ;
        }

        private VideoCacheItem GetNextDownloadVideoCacheItem()
        {
            if (_videoCacheItemRepository.FindByStatus(VideoCacheStatus.DownloadPaused).OrderBy(x => x.SortIndex).ThenBy(x => x.RequestedAt).FirstOrDefault() is not null and var pausingNextDLItem)
            {
                return EntityToItem(pausingNextDLItem);
            }

            if (_videoCacheItemRepository.FindByStatus(VideoCacheStatus.Pending).OrderBy(x => x.SortIndex).ThenBy(x => x.RequestedAt).FirstOrDefault() is not null and var pendingNextDLItem)
            {
                return EntityToItem(pendingNextDLItem);
            }

            return null;
        }


        public async ValueTask<PrepareNextVideoCacheDownloadingResult> PrepareNextCacheDownloadingTaskAsync()
        {
            using (await _updateLock.LockAsync())
            {
                var item = GetNextDownloadVideoCacheItem();
                if (item is null)
                {
                    var result = PrepareNextVideoCacheDownloadingResult.Failed(string.Empty, null, VideoCacheDownloadOperationFailedReason.NoMorePendingOrPausingItem);
                    return result;
                }

                try
                {
                    var opCreationResult = await CreateDownloadOperationAsync(item);

                    if (opCreationResult.IsSuccess)
                    {
                        var op = opCreationResult.DownloadOperation;

                        op.Started += (s, e) => 
                        {
                            this.Started?.Invoke(this, new VideoCacheStartedEventArgs() { Item = (s as IVideoCacheDownloadOperation).VideoCacheItem });
                        };

                        op.Paused += (s, e) => 
                        {
                            var op = s as IVideoCacheDownloadOperation;
                            var item = op.VideoCacheItem;
                            (op as IDisposable).Dispose();

                            item.Status = VideoCacheStatus.CompletedFromOldCache_NotEncypted;
                            UpdateVideoCacheEntity(item);

                            this.Paused?.Invoke(this, new VideoCachePausedEventArgs() { Item = item });
                        };

                        op.Progress += (s, e) => 
                        {
                            var op = s as IVideoCacheDownloadOperation;
                            OnProgress(op, e);

                            Progress?.Invoke(this, new VideoCacheProgressEventArgs() { Item = op.VideoCacheItem });
                        };

                        op.Completed += async (s, e) => 
                        {
                            await FinishDownloadOperation(s as IVideoCacheDownloadOperation);

                            this.Completed?.Invoke(this, new VideoCacheCompletedEventArgs() { Item = item });
                        };

                        _currentDownloadOperations.Add(item.VideoId, op);

                        item.Status = VideoCacheStatus.Downloading;
                        UpdateVideoCacheEntity(item);

                        var result = PrepareNextVideoCacheDownloadingResult.Success(item.VideoId, item, op);
                        return result;
                    }
                    else
                    {
                        item.FailedReason = opCreationResult.FailedReason;
                        item.Status = VideoCacheStatus.Failed;
                        UpdateVideoCacheEntity(item);

                        Failed?.Invoke(this, new VideoCacheFailedEventArgs() { Item = item, VideoCacheDownloadOperationCreationFailedReason = item.FailedReason });

                        var result = PrepareNextVideoCacheDownloadingResult.Failed(item.VideoId, item, opCreationResult.FailedReason);
                        return result;
                    }

                }
                catch (Exception ex)
                {
                    item.FailedReason = VideoCacheDownloadOperationFailedReason.Unknown;
                    item.Status = VideoCacheStatus.Failed;
                    UpdateVideoCacheEntity(item);

                    Failed?.Invoke(this, new VideoCacheFailedEventArgs() { Item = item, VideoCacheDownloadOperationCreationFailedReason = item.FailedReason });

                    var result = PrepareNextVideoCacheDownloadingResult.Failed(item.VideoId, item, item.FailedReason);
                    throw new VideoCacheException($"Failed video cache download starting. videoId: {item?.VideoId}", ex);
                }
            }
        }


        internal void CleanupVideoCacheOperation(IVideoCacheDownloadOperation op)
        {
            var item = op.VideoCacheItem;
            _currentDownloadOperations.Remove(item.VideoId);
        }

        private Task<StorageFile> GetProgressCacheVideoFileAsync(VideoCacheItem item, CreationCollisionOption creationCollisionOption)
        {
            return GetProgressCacheVideoFileAsync(item.VideoId, creationCollisionOption);
        }


        private async Task<StorageFile> GetProgressCacheVideoFileAsync(string videoId, CreationCollisionOption creationCollisionOption)
        {
            // キャッシュ保存先フォルダから指定動画IDのファイルを検索し、存在すればOpenIfExistsで開き、無ければ作成
            var fileNameWithExt = Path.ChangeExtension(await ResolveVideoFileNameWithoutExtFromVideoId(videoId), HohoemaVideoCacheProgressExt);
            return await VideoCacheFolder.CreateFileAsync(fileNameWithExt, creationCollisionOption);
        }

        private async Task<StorageFile> GetProgressCacheVideoFileAsync(string videoId)
        {
            // キャッシュ保存先フォルダから指定動画IDのファイルを検索し、存在すればOpenIfExistsで開き、無ければ作成
            var fileNameWithExt = Path.ChangeExtension(await ResolveVideoFileNameWithoutExtFromVideoId(videoId), HohoemaVideoCacheProgressExt);
            return await VideoCacheFolder.GetFileAsync(fileNameWithExt);
        }


        private Task<StorageFile> GetCacheVideoFileAsync(VideoCacheItem item)
        {
            return GetCacheVideoFileAsync(item.VideoId);
        }

        private async Task<StorageFile> GetCacheVideoFileAsync(string videoId)
        {
            // キャッシュ保存先フォルダから指定動画IDのファイルを開く
            var fileNameWithExt = Path.ChangeExtension(await ResolveVideoFileNameWithoutExtFromVideoId(videoId), HohoemaVideoCacheExt);
            return await VideoCacheFolder.GetFileAsync(fileNameWithExt);
        }


        private void OnProgress(IVideoCacheDownloadOperation op, VideoCacheDownloadOperationProgress progress)
        {
            op.VideoCacheItem.SetProgress(progress);

            UpdateVideoCacheEntity(op.VideoCacheItem);            
        }

        private async Task FinishDownloadOperation(IVideoCacheDownloadOperation op)
        {
            using (await _updateLock.LockAsync())
            {
                var item = op.VideoCacheItem;
                _currentDownloadOperations.Remove(item.VideoId);

                (op as IDisposable).Dispose();

                if (item.Status is VideoCacheStatus.DownloadPaused)
                {
                    // do nothing
                }
                else if (item.TotalBytes == item.ProgressBytes)
                {
                    item.Status = VideoCacheStatus.Completed;
                }
                else
                {
                    item.Status = VideoCacheStatus.Failed;
                }

                UpdateVideoCacheEntity(item);

                if (item.Status is VideoCacheStatus.Completed)
                {
                    var file = await GetProgressCacheVideoFileAsync(item.VideoId, CreationCollisionOption.OpenIfExists);
                    if (file is not null)
                    {
                        await file.RenameAsync(Path.ChangeExtension(file.Name, null));
                        Debug.WriteLine("complete: " + file.Path);
                    }
                }
            }
        }


        public async ValueTask PauseAllDownloadOperationAsync()
        {
            using (await _updateLock.LockAsync())
            {
                foreach (var op in _currentDownloadOperations.Values)
                {
                    await op.PauseAsync();
                }

                _currentDownloadOperations.Clear();
            }
        }
        
        internal async ValueTask<VideoCacheDownloadOperationCreationResult> CreateDownloadOperationAsync(VideoCacheItem item)
        {
            if (item.IsCompleted) return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.None);

            if (Helpers.InternetConnection.IsInternet() is false) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.InternetUnavairable); }

            if (await CheckUsageAuthorityAsync() is false) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.NoUsageAuthority); }

            if (await this.CheckStorageCapacityLimitReachedAsync() is true) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.StorageCapacityLimitReached); }

            var videoSessionOwnershipRentResult = await _videoSessionOwnershipManager.TryRentVideoSessionOwnershipAsync(item.VideoId, isPriorityRent: false);
            if (videoSessionOwnershipRentResult is null) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.RistrictedDownloadLineCount); }

            try
            {
                var watchData = await _niconicoSession.Context.Video.GetDmcWatchResponseAsync(item.VideoId);
                if (watchData?.DmcWatchResponse?.Media?.Delivery is not null and var deliverly)
                {
                    if (deliverly.Encryption is not null)
                    {
                        return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.CanNotCacheEncryptedContent);
                    }
                }
                else if (watchData?.DmcWatchResponse?.Media?.Delivery is null && watchData?.DmcWatchResponse?.Media?.DeliveryLegacy is not null)
                {
                    return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.Unknown);
                }


                NicoVideoCacheQuality candidateDownloadingQuality = item.RequestedVideoQuality;
                if (item.Status == VideoCacheStatus.DownloadPaused)
                {
                    candidateDownloadingQuality = item.DownloadedVideoQuality;
                }
                else
                {
                    var avairableQualities = watchData.DmcWatchResponse.Media.Delivery.Movie.Session.Videos.Select(x => NicoVideoCacheQualityHelper.QualityIdToCacheQuality(x)).ToArray();
                    if (avairableQualities.Length == 0) { throw new Exception("キャッシュ用画質Enumの変換に失敗"); }

                    while (avairableQualities.Contains(candidateDownloadingQuality) is false && candidateDownloadingQuality is not NicoVideoCacheQuality.Unknown)
                    {
                        candidateDownloadingQuality = NicoVideoCacheQualityHelper.GetOneLowerQuality(candidateDownloadingQuality);
                    }

                    // 未指定または画質が見つからなかった場合は一番高画質を自動指定
                    if (candidateDownloadingQuality is NicoVideoCacheQuality.Unknown)
                    {
                        candidateDownloadingQuality = avairableQualities.First();
                    }
                }

                var outputFile = await GetProgressCacheVideoFileAsync(item, 
                    item.Status is VideoCacheStatus.DownloadPaused
                        ? CreationCollisionOption.OpenIfExists
                        : CreationCollisionOption.ReplaceExisting
                        );

                var dmcVideoStreamingSession = new DmcVideoStreamingSession(NicoVideoCacheQualityHelper.CacheQualityToQualityId(candidateDownloadingQuality), watchData, _niconicoSession, videoSessionOwnershipRentResult);
                var op = new VideoCacheDownloadOperation(this, item, dmcVideoStreamingSession, new VideoCacheDownloadOperationOutputWithEncryption(outputFile, Xts));

                return new VideoCacheDownloadOperationCreationResult(op);
            }
            catch
            {
                videoSessionOwnershipRentResult.Dispose();
                return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.Unknown);
            }
        }


        #region Migarate Legacy


        public bool HasNotEncrypedtLegacyVideoCacheItem()
        {
            return _videoCacheItemRepository.FindByStatus(VideoCacheStatus.CompletedFromOldCache_NotEncypted) is not null;
        }

        private VideoCacheItem GetNextNotEncrypedtLegacyVideoCacheItem()
        {
            if (_videoCacheItemRepository.FindByStatus(VideoCacheStatus.CompletedFromOldCache_NotEncypted).OrderBy(x => x.SortIndex).ThenBy(x => x.RequestedAt).FirstOrDefault() is not null and var pausingNextDLItem)
            {
                return EntityToItem(pausingNextDLItem);
            }

            return null;
        }

        internal async Task PushCacheRequestAsync_Legacy(string videoId, NicoVideoCacheQuality quality, StorageFile regacyFile)
        {
            if (regacyFile.FileType is ".mp4")
            {
                var fileNameWithExt = Path.ChangeExtension(await ResolveVideoFileNameWithoutExtFromVideoId(videoId), HohoemaVideoCacheExt);
                await regacyFile.RenameAsync(fileNameWithExt);

                var entity = new VideoCacheEntity() { VideoId = videoId, RequestedVideoQuality = quality, Status = VideoCacheStatus.CompletedFromOldCache_NotEncypted };
                _videoCacheItemRepository.UpdateVideoCache(entity);
            }
            else
            {
                var entity = new VideoCacheEntity() { VideoId = videoId, RequestedVideoQuality = quality, Status = VideoCacheStatus.Pending };
                _videoCacheItemRepository.UpdateVideoCache(entity);
            }
        }

        public async Task<PrepareNextVideoCacheDownloadingResult> PrepareNextEncryptLegacyVideoTaskAsync()
        {
            var item = GetNextNotEncrypedtLegacyVideoCacheItem();

            var inputFile = await GetCacheVideoFileAsync(item.VideoId);
            var outputFile = await GetProgressCacheVideoFileAsync(item.VideoId, CreationCollisionOption.ReplaceExisting);
            var op = new VideoCacheMigretedFileEncryptOperation(item, inputFile, new VideoCacheDownloadOperationOutputWithEncryption(outputFile, Xts));

            op.Started += (s, e) =>
            {
                this.Started?.Invoke(this, new VideoCacheStartedEventArgs() { Item = (s as IVideoCacheDownloadOperation).VideoCacheItem });
            };

            op.Paused += (s, e) =>
            {
                var op = s as IVideoCacheDownloadOperation;
                var item = op.VideoCacheItem;
                (op as IDisposable).Dispose();

                item.Status = VideoCacheStatus.DownloadPaused;
                UpdateVideoCacheEntity(item);

                this.Paused?.Invoke(this, new VideoCachePausedEventArgs() { Item = item });
            };

            op.Progress += (s, e) =>
            {
                var op = s as IVideoCacheDownloadOperation;
                OnProgress(op, e);

                Progress?.Invoke(this, new VideoCacheProgressEventArgs() { Item = op.VideoCacheItem });
            };

            op.Completed += async (s, e) =>
            {
                await FinishDownloadOperation(s as IVideoCacheDownloadOperation);

                this.Completed?.Invoke(this, new VideoCacheCompletedEventArgs() { Item = item });
            };

            var result = PrepareNextVideoCacheDownloadingResult.Success(item.VideoId, item, op);
            return result;
        }


        #endregion
    }
}

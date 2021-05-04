﻿using Hohoema.Models.Helpers;
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
using Uno.Disposables;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;

namespace Hohoema.Models.Domain.VideoCache
{
    public struct VideoCacheRequestedEventArgs
    {
        public string VideoId { get; set; }
        public NicoVideoQuality RequestedQuality { get; set; }
    }

    public struct VideoCacheStartedEventArgs
    {
        public VideoCacheItem Item { get; set; }
    }

    public struct VideoCacheCanceledEventArgs
    {
        public string VideoId { get; set; }
        public VideoCacheCancelReason Reason { get; internal set; }
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

    public struct VideoCacheMaxStorageSizeChangedEventArgs
    {
        public long? OldMaxSize { get; set; }
        public long? NewMaxSize { get; set; }
        public bool IsCapacityLimitReached { get; set; }
    }


    public enum VideoCacheCancelReason
    {
        User,
        DeleteFromServer,
    }


    public sealed class VideoCacheManager : IDisposable
    {
        public const string HohoemaVideoCacheExt = ".hohoema_cv";

        public static Func<string, NicoVideoQuality, Task<string>> ResolveVideoFileNameWithoutExtFromVideoId { get; set; } = (id, q) => Task.FromResult($"[{id}-{q.ToString().ToLower()}]");
        
        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoSessionOwnershipManager _videoSessionOwnershipManager;
        private readonly VideoCacheItemRepository _videoCacheItemRepository;
        private readonly VideoCacheSettings _videoCacheSettings;

        public Xts Xts { get; private set; }
        public StorageFolder VideoCacheFolder { get; set; }

        public void SetXts(Xts xts)
        {
            Xts = xts;
        }

        CompositeDisposable _disposables = new CompositeDisposable();
        Dictionary<string, IVideoCacheDownloadOperation> _currentDownloadOperations = new Dictionary<string, IVideoCacheDownloadOperation>();
        AsyncLock _updateLock = new AsyncLock();
        Dictionary<string, VideoCacheItem> _videoItemMap = new ();


        public event EventHandler<VideoCacheRequestedEventArgs> Requested;
        public event EventHandler<VideoCacheCanceledEventArgs> Canceled;
        public event EventHandler<VideoCacheStartedEventArgs> Started;
        public event EventHandler<VideoCacheProgressEventArgs> Progress;
        public event EventHandler<VideoCacheCompletedEventArgs> Completed;
        public event EventHandler<VideoCacheFailedEventArgs> Failed;        
        public event EventHandler<VideoCachePausedEventArgs> Paused;

        public event EventHandler<VideoCacheMaxStorageSizeChangedEventArgs> MaxStorageSizeChanged;

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

        #region Capacity Managing



        public long? MaxCacheStorageSize
        {
            get => _videoCacheSettings.MaxVideoCacheStorageSize;
            set
            {
                var oldValue = _videoCacheSettings.MaxVideoCacheStorageSize;
                if (_videoCacheSettings.MaxVideoCacheStorageSize != value)
                {
                    _videoCacheSettings.MaxVideoCacheStorageSize = value;
                    MaxStorageSizeChanged?.Invoke(this, new VideoCacheMaxStorageSizeChangedEventArgs() { OldMaxSize = oldValue, NewMaxSize = value, IsCapacityLimitReached = CheckStorageCapacityLimitReached() });
                }
            }
        }

        public long GetCurrentryTotalCachedSize()
        {
            return _videoCacheSettings.CachedStorageSize = _videoCacheItemRepository.SumVideoCacheSize();
        }

        internal bool CheckStorageCapacityLimitReached()
        {
            var storageSize = _videoCacheSettings.CachedStorageSize = _videoCacheItemRepository.SumVideoCacheSize();
            var maxStroageSize = MaxCacheStorageSize;
            if (maxStroageSize is not null and var maxSize)
            {
                return storageSize > maxSize;
            }
            else
            {
                return false;
            }
        }        

        #endregion

        #region Delete on Server

        public Task<bool> DeleteFromNiconicoServer(string videoId)
        {
            return CancelCacheRequestAsync_Internal(videoId, VideoCacheCancelReason.DeleteFromServer);
        }


        #endregion


        public bool IsCacheDownloadAuthorized()
        {
#if DEBUG
            return true;
#else
            return _niconicoSession.IsPremiumAccount is true;
#endif
        }        

        public VideoCacheItem GetVideoCache(string videoId)
        {
            if (_videoItemMap.TryGetValue(videoId, out var val))
            {
                return val;
            }

            var entity = _videoCacheItemRepository.GetVideoCache(videoId);
            if (entity == null) { return null; }

            return _videoItemMap[videoId] = EntityToItem(this, entity);
        }

        public VideoCacheStatus? GetVideoCacheStatus(string videoId)
        {
            return _videoCacheItemRepository.GetVideoCache(videoId)?.Status;
        }

        public int GetCacheRequestCount()
        {
            return _videoCacheItemRepository.GetTotalCount();
        }

        public List<VideoCacheItem> GetCacheRequestItemsRange(int head, int count, VideoCacheStatus? status = null, bool decsending = false)
        {
            return _videoCacheItemRepository.GetItemsOrderByRequestedAt(head, count, status, decsending).Select(x => EntityToItem(this, x)).ToList();
        }


        public async Task<MediaSource> GetCacheVideoMediaSource(VideoCacheItem item)
        {
#if !DEBUG
            if (_niconicoSession.IsPremiumAccount is false) 
                throw new VideoCacheException("VideoCacheItem is can not play. premium account required.");
#endif
            if (item.IsCompleted is false) 
                throw new VideoCacheException("VideoCacheItem is can not play. not completed download the cache.");

            var file = await GetCacheVideoFileAsync(item);
            var stream = new XtsStream(await file.OpenStreamForReadAsync(), Xts);
            var ms = MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "movie/mp4");
            return ms;
        }


        public async Task PushCacheRequestAsync(string videoId, NicoVideoQuality requestCacheQuality)
        {
            using (await _updateLock.LockAsync())
            {
                var entity = _videoCacheItemRepository.GetVideoCache(videoId);

                VideoCacheStatus? prevStatus = entity?.Status;
                entity ??= new VideoCacheEntity() { VideoId = videoId };
                if (entity.FileName is null)
                {
                    try
                    {
                        entity.FileName = Path.ChangeExtension(await ResolveVideoFileNameWithoutExtFromVideoId(videoId, requestCacheQuality), HohoemaVideoCacheExt);
                    }
                    catch
                    {
                        entity.FileName = Path.ChangeExtension(videoId, HohoemaVideoCacheExt);
                    }
                }

                entity.Status = prevStatus ?? VideoCacheStatus.Pending;
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
                        if (requestCacheQuality != NicoVideoQuality.Unknown && entity.DownloadedVideoQuality != requestCacheQuality)
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
                }

                _videoCacheItemRepository.UpdateVideoCache(entity);
                if (GetVideoCache(videoId) is not null and var item)
                {
                    item.Status = entity.Status;
                    item.FailedReason = entity.FailedReason;
                    item.RequestedAt = entity.RequestedAt;
                    item.RequestedVideoQuality = entity.RequestedVideoQuality;
                }

                if (entity.Status is VideoCacheStatus.Pending)
                {
                    Requested?.Invoke(this, new VideoCacheRequestedEventArgs() { VideoId = videoId, RequestedQuality = requestCacheQuality });
                }
            }
        }

        public Task<bool> CancelCacheRequestAsync(string videoId)
        {
            return CancelCacheRequestAsync_Internal(videoId, VideoCacheCancelReason.User);
        }

        private async Task<bool> CancelCacheRequestAsync_Internal(string videoId, VideoCacheCancelReason reason)
        {
            using (await _updateLock.LockAsync())
            {
                // ダウンロードを中止
                try
                {
                    if (_currentDownloadOperations.Remove(videoId, out var op))
                    {
                        await op.StopAndDeleteDownloadedAsync();
                        (op as IDisposable).Dispose();
                    }
                }
                catch 
                {
#if DEBUG
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
#endif
                }

                // ファイルとして削除
                try
                { 
                    if (await GetCacheVideoFileAsync(videoId) is not null and var completedFile)
                    {
                        await completedFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
                catch
                {
                }

                _videoItemMap.Remove(videoId);

                // ローカルDBから削除
                if (_videoCacheItemRepository.DeleteVideoCache(videoId) is true)
                {
                    Canceled?.Invoke(this, new VideoCacheCanceledEventArgs() { VideoId = videoId, Reason = reason });

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static VideoCacheItem EntityToItem(VideoCacheManager manager, VideoCacheEntity entity)
        {
            return new VideoCacheItem(
                manager,
                entity.VideoId,
                entity.FileName,
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
            return _videoCacheItemRepository.ExistsByStatus(VideoCacheStatus.Downloading)
                || _videoCacheItemRepository.ExistsByStatus(VideoCacheStatus.DownloadPaused)
                || _videoCacheItemRepository.ExistsByStatus(VideoCacheStatus.Pending)
                ;
        }

        private VideoCacheItem GetNextDownloadVideoCacheItem()
        {
            VideoCacheEntity entity = null;
            if (_videoCacheItemRepository.FindByStatus(VideoCacheStatus.Downloading).OrderBy(x => x.SortIndex).ThenBy(x => x.RequestedAt).Where(x => !_currentDownloadOperations.ContainsKey(x.VideoId)).FirstOrDefault() is not null and var progressButNotDownloadStartedNextDLItem)
            {
                entity = progressButNotDownloadStartedNextDLItem;
            }
            else if (_videoCacheItemRepository.FindByStatus(VideoCacheStatus.DownloadPaused).OrderBy(x => x.SortIndex).ThenBy(x => x.RequestedAt).FirstOrDefault() is not null and var pausingNextDLItem)
            {
                entity = pausingNextDLItem;
            }
            else if (_videoCacheItemRepository.FindByStatus(VideoCacheStatus.Pending).OrderBy(x => x.SortIndex).ThenBy(x => x.RequestedAt).FirstOrDefault() is not null and var pendingNextDLItem)
            {
                entity = pendingNextDLItem;
            }

            if (entity is not null)
            {
                if (_videoItemMap.TryGetValue(entity.VideoId, out var val))
                {
                    return val;
                }

                return _videoItemMap[entity.VideoId] = EntityToItem(this, entity);
            }
            else
            {
                return null;
            }
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
                            var op = (s as VideoCacheDownloadOperation);
                            var item = op.VideoCacheItem;
                            item.Status = VideoCacheStatus.Downloading;
                            UpdateVideoCacheEntity(item);

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
                            op.VideoCacheItem.SetProgress(e);

                            UpdateVideoCacheEntity(op.VideoCacheItem);


                            Progress?.Invoke(this, new VideoCacheProgressEventArgs() { Item = op.VideoCacheItem });
                        };

                        op.Completed += async (s, e) => 
                        {
                            using (await _updateLock.LockAsync())
                            {
                                var item = op.VideoCacheItem;
                                _currentDownloadOperations.Remove(item.VideoId);

                                (op as IDisposable).Dispose();
                            }

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
                                Debug.WriteLine("complete: " + item.FileName);
                            }

                            this.Completed?.Invoke(this, new VideoCacheCompletedEventArgs() { Item = item });
                        };

                        try
                        {
                            _currentDownloadOperations.Add(item.VideoId, op);
                        }
                        catch
                        {
                            opCreationResult.DownloadOperation.TryDispose();
                            throw;
                        }

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
                    _currentDownloadOperations.Remove(item.VideoId);

                    item.FailedReason = VideoCacheDownloadOperationFailedReason.Unknown;
                    item.Status = VideoCacheStatus.Failed;
                    UpdateVideoCacheEntity(item);

                    Failed?.Invoke(this, new VideoCacheFailedEventArgs() { Item = item, VideoCacheDownloadOperationCreationFailedReason = item.FailedReason });

                    var result = PrepareNextVideoCacheDownloadingResult.Failed(item.VideoId, item, item.FailedReason);
                    throw new VideoCacheException($"Failed video cache download starting. videoId: {item?.VideoId}", ex);
                }
            }
        }

        private Task<StorageFile> GetCacheVideoFileAsync(string videoId)
        {
            var entity = _videoCacheItemRepository.GetVideoCache(videoId);
            if (entity is null) { return null; }
            try
            {
                return VideoCacheFolder.GetFileAsync(entity.FileName).AsTask();
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private Task<StorageFile> GetCacheVideoFileAsync(VideoCacheItem item)
        {
            return VideoCacheFolder.GetFileAsync(item.FileName).AsTask();
        }

        public class ResumeInfo
        {
            public ResumeInfo(IEnumerable<string> pausedItems)
            {
                PausedVideoIdList = pausedItems.ToList();
            }

            public ResumeInfo()
            {
                PausedVideoIdList = new List<string>();
            }

            public IReadOnlyCollection<string> PausedVideoIdList { get; }
        }


        public async Task<ResumeInfo> PauseAllDownloadOperationAsync()
        {
            using (await _updateLock.LockAsync())
            {
                if (_currentDownloadOperations.Count == 0)
                {
                    return new ResumeInfo();
                }

                ResumeInfo resumeInfo = new ResumeInfo(_currentDownloadOperations.Keys);
                foreach (var op in _currentDownloadOperations.Values)
                {
                    await op.PauseAsync();
                }

                _currentDownloadOperations.Clear();

                return resumeInfo;
            }
        }
        
        internal async ValueTask<VideoCacheDownloadOperationCreationResult> CreateDownloadOperationAsync(VideoCacheItem item)
        {
            if (item.IsCompleted) return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.None);

            if (Helpers.InternetConnection.IsInternet() is false) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.InternetUnavairable); }

            if (IsCacheDownloadAuthorized() is false) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.NoUsageAuthority); }

            if (this.CheckStorageCapacityLimitReached() is true) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.StorageCapacityLimitReached); }

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


                NicoVideoQuality candidateDownloadingQuality = item.RequestedVideoQuality;
#if !DEBUG
                if (item.Status is VideoCacheStatus.DownloadPaused or VideoCacheStatus.Downloading)
#else
                if (false)
#endif
                {
                    // Note: プレミアム会員のみ利用可能なので item.DownloadedVideoQuality が利用不可であることは想定しない
                    candidateDownloadingQuality = item.DownloadedVideoQuality;
                }
                else
                {
                    var avairableQualities = watchData.DmcWatchResponse.Media.Delivery.Movie.Session.Videos.Select(x => NicoVideoCacheQualityHelper.QualityIdToCacheQuality(x)).ToArray();
                    if (avairableQualities.Length == 0) { throw new Exception("キャッシュ用画質Enumの変換に失敗"); }

                    while (avairableQualities.Contains(candidateDownloadingQuality) is false && candidateDownloadingQuality is not NicoVideoQuality.Unknown)
                    {
                        candidateDownloadingQuality = NicoVideoCacheQualityHelper.GetOneLowerQuality(candidateDownloadingQuality);
                    }

                    // 未指定または画質が見つからなかった場合はもっと高い画質を指定
                    if (candidateDownloadingQuality is NicoVideoQuality.Unknown)
                    {
                        candidateDownloadingQuality = avairableQualities.First();
                    }
                }

                StorageFile outputFile = null;
                if (item.Status is VideoCacheStatus.DownloadPaused or VideoCacheStatus.Downloading)
                {
                    outputFile = await GetCacheVideoFileAsync(item);
                    if (outputFile == null)
                    {
                        throw new InvalidOperationException("DLをポーズしてるはずなのにファイルが無い");
                    }
                }
                else
                {
                    outputFile = await VideoCacheFolder.CreateFileAsync(item.FileName, CreationCollisionOption.ReplaceExisting);
                }

                if (outputFile is null)
                {
                    throw new InvalidOperationException("キャッシュ出力ファイルの指定が不正");
                }

                // ファイルアクセスが出来るようになるまで待機
                // アプリ再起動後など
                foreach (var count in Enumerable.Range(1, 5))
                {
                    try
                    {
                        using (var temp = await outputFile.OpenStreamForWriteAsync()) { }
                        break;
                    }
                    catch (Exception ex) when (ex is FileLoadException)
                    {
                        if (count == 5)
                        {
                            throw;
                        }

                        await Task.Delay(500);
                    }
                }

                item.DownloadedVideoQuality = candidateDownloadingQuality;
                if (item.RequestedVideoQuality == NicoVideoQuality.Unknown)
                {
                    item.FileName = await ResolveVideoFileNameWithoutExtFromVideoId(item.VideoId, item.DownloadedVideoQuality);
                }

                UpdateVideoCacheEntity(item);

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

        /// <summary>
        /// 指定ファイルをDBにインポートする<br/> 
        /// 同一IDが存在する場合は上書き保存される。
        /// </summary>
        /// <param name="videoId"></param>
        /// <param name="quality"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        internal async Task ImportCacheRequestAsync(string videoId, NicoVideoQuality quality, StorageFile file)
        {
            var prop = await file.GetBasicPropertiesAsync();
            if (_videoItemMap.TryGetValue(videoId, out var item))
            {
                item.RequestedVideoQuality = NicoVideoQuality.Unknown;
                item.DownloadedVideoQuality = quality;
                item.Status = VideoCacheStatus.Completed;
                item.FileName = file.Name;
                item.TotalBytes = (long)prop.Size;
                item.ProgressBytes = (long)prop.Size;
                item.RequestedAt = file.DateCreated.DateTime;
            }
            else
            {
                item = new VideoCacheItem(this, videoId, file.Name, NicoVideoQuality.Unknown, quality, VideoCacheStatus.Completed, VideoCacheDownloadOperationFailedReason.None, file.DateCreated.DateTime, (long)prop.Size, (long)prop.Size, 0);
                _videoItemMap.Add(videoId, item);
            }

            UpdateVideoCacheEntity(item);
        }

        #region Migarate Legacy

        internal void PushCacheRequest_Legacy(string videoId, NicoVideoQuality quality)
        {
            var entity = new VideoCacheEntity() { VideoId = videoId, RequestedVideoQuality = quality, Status = VideoCacheStatus.Pending };
            _videoCacheItemRepository.UpdateVideoCache(entity);
        }

#endregion
    }
}

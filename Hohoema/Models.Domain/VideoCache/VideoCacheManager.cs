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
using Uno.Disposables;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using System.Security.Cryptography;
using System.Collections;
using Microsoft.Toolkit.Uwp.Helpers;
using Hohoema.Models.Domain.Niconico.Video;

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


    public struct VideoCacheContentIdReplacedEventArgs
    {
        public string SourceVideoId { get; set; }
        public string ReplacedVideoId { get; set; }
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
        public const string HohoemaVideoCacheHashExt = ".hohoema_cv_hash";

        public static Func<string, ValueTask<string>> ResolveVideoTitle { get; set; } = (id) => throw new NotImplementedException();

        public static async ValueTask<string> ResolveVideoFileName(string id, NicoVideoQuality quality)
        {
            var title = await ResolveVideoTitle(id);
            return $"{title.ToSafeFilePath()} [{id}-{quality}]";
        }

        static HashAlgorithm MakeHashAlgrorithm() => SHA512.Create();


        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoSessionOwnershipManager _videoSessionOwnershipManager;
        private readonly VideoCacheItemRepository _videoCacheItemRepository;
        private readonly NicoVideoCacheRepository _nicoVideoCacheRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
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

        public event EventHandler<VideoCacheContentIdReplacedEventArgs> VideoIdReplaced;


        public VideoCacheManager(
            NiconicoSession niconicoSession, 
            NicoVideoSessionOwnershipManager videoSessionOwnershipManager,
            VideoCacheSettings videoCacheSettings,
            VideoCacheItemRepository videoCacheItemRepository,
            NicoVideoProvider nicoVideoProvider,
            NicoVideoCacheRepository nicoVideoCacheRepository
            )
        {
            _niconicoSession = niconicoSession;
            _videoSessionOwnershipManager = videoSessionOwnershipManager;
            _videoCacheSettings = videoCacheSettings;
            _videoCacheItemRepository = videoCacheItemRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _nicoVideoCacheRepository = nicoVideoCacheRepository;
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
        }

        public NicoVideoQuality DefaultCacheQuality
        {
            get => _videoCacheSettings.DefaultCacheQuality;
        }

        public long UpdateCurrentryTotalCachedSize()
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
            EnsureNonNumberVideoId(ref videoId);

            return CancelCacheRequestAsync_Internal(videoId, VideoCacheCancelReason.DeleteFromServer);
        }


        #endregion

        /// <summary>
        /// videoIdが数字で始まるIDの場合にsm/so等で始まる動画IDに置き換える。
        /// </summary>
        /// <param name="videoId"></param>
        private void EnsureNonNumberVideoId(ref string videoId)
        {
            if (videoId.All(x => char.IsDigit(x)))
            {
                var id = _nicoVideoCacheRepository.GetVideoId(videoId);
                if (id != null)
                {
                    videoId = id;
                }
                else
                {
                    Debug.WriteLine("数字のみのIDを動画IDに変換できなかった: " + videoId);
#if DEBUG
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
#endif
                }
            }
        }

        private async Task<(bool IsNumber, string RealVideoId)> GetVideoIdIfNumberVideoIdAsync(string videoId)
        {
            if (videoId.All(x => char.IsDigit(x)))
            {
                var info = await _nicoVideoProvider.GetNicoVideoInfo(videoId);
                if (info != null)
                {
                    return (true, info.VideoId);
                }
            }

            return (false, null);
        }


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
            EnsureNonNumberVideoId(ref videoId);

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
            EnsureNonNumberVideoId(ref videoId);

            return _videoCacheItemRepository.GetVideoCache(videoId)?.Status;
        }

        public int GetCacheRequestCount()
        {
            return _videoCacheItemRepository.GetTotalCount();
        }

        public List<VideoCacheItem> GetCacheRequestItemsRange(int head, int count, VideoCacheStatus? status = null, bool decsending = false)
        {
            return _videoCacheItemRepository
                .GetItemsOrderByRequestedAt(head, count, status, decsending)
                .Select(x => EntityToItem(this, x))
                .ToList();
        }


        public async Task<MediaSource> GetCacheVideoMediaSource(VideoCacheItem item)
        {
            // require online
            if (!Helpers.InternetConnection.IsInternet())
            {
                throw new VideoCacheException("VideoCacheItem is can not play, required internet connection.");
            }

            // require login with niconico Premium member
#if !DEBUG
            if (_niconicoSession.IsPremiumAccount is false) 
            {
                throw new VideoCacheException("VideoCacheItem is can not play. premium account required.");
            }
#endif

            // require download complted
            if (item.IsCompleted is false)
            {
                throw new VideoCacheException("VideoCacheItem is can not play. not completed download the cache.");
            }

            // require watch permission
            var watchData = await _niconicoSession.Context.Video.GetDmcWatchResponseAsync(item.VideoId);
            if (watchData?.DmcWatchResponse?.Media?.Delivery is null)
            {
                throw new VideoCacheException("VideoCacheItem is can not play, require content access permission. reason : " + watchData?.DmcWatchResponse?.OkReason);
            }

#if DEBUG
            Stopwatch sw = Stopwatch.StartNew();
#endif

            // require same hash
            var file = await GetCacheVideoFileAsync(item);
            if (!await IsFileContainsValidHashAsync(file))
            {
                throw new VideoCacheException("VideoCacheItem is can not play, invalid Hash.");
            }

#if DEBUG
            sw.Stop();
            Debug.WriteLine("compute hash elapsed time is " + sw.Elapsed);
#endif

            var stream = new XtsStream(await file.OpenStreamForReadAsync(), Xts);
            if (stream.Length != item.TotalBytes)
            {
                stream.Dispose();
                throw new VideoCacheException("VideoCacheItem is can not play, require same size");
            }

            var ms = MediaSource.CreateFromStream(stream.AsRandomAccessStream(), "movie/mp4");
            await ms.OpenAsync();
            if (ms.Duration?.TotalSeconds - watchData.DmcWatchResponse.Video.Duration >= 2.0)
            {
                ms.Dispose();
                throw new VideoCacheException("VideoCacheItem is can not play, require same duration");
            }

            // ok
            return ms;
        }



        public async Task PushCacheRequestAsync(string videoId, NicoVideoQuality requestCacheQuality)
        {
            using (await _updateLock.LockAsync())
            {
                EnsureNonNumberVideoId(ref videoId);

                var entity = _videoCacheItemRepository.GetVideoCache(videoId);

                VideoCacheStatus? prevStatus = entity?.Status;
                entity ??= new VideoCacheEntity() { VideoId = videoId };
                if (entity.FileName is null)
                {
                    try
                    {
                        entity.FileName = Path.ChangeExtension(await ResolveVideoFileName(videoId, requestCacheQuality), HohoemaVideoCacheExt);
                        entity.Title = await ResolveVideoTitle(videoId);
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
                    item.Title = entity.Title;
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
            EnsureNonNumberVideoId(ref videoId);

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
                        await op.CancelAsync();
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
                    var file = await GetCacheVideoFileAsync(videoId);
                    if (file is not null)
                    {
                        await DeleteHashFile(file);
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }
                catch { }

                _videoItemMap.Remove(videoId);

                // ローカルDBから削除                
                if (_videoCacheItemRepository.DeleteVideoCache(videoId) is true)
                {
                    UpdateCurrentryTotalCachedSize();

                    Canceled?.Invoke(this, new VideoCacheCanceledEventArgs() { VideoId = videoId, Reason = reason });
                    
                    return true;
                }
                else
                {
                    return false;
                }
            }
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
                            var op = s as IVideoCacheDownloadOperation;
                            item.Status = VideoCacheStatus.Downloading;
                            UpdateVideoCacheEntity(item);

                            UpdateCurrentryTotalCachedSize();

                            this.Started?.Invoke(this, new VideoCacheStartedEventArgs() { Item = item });
                        };

                        op.Progress += (s, e) => 
                        {
                            var op = s as IVideoCacheDownloadOperation;
                            op.VideoCacheItem.SetProgress(e);

                            UpdateVideoCacheEntity(op.VideoCacheItem);

                            Progress?.Invoke(this, new VideoCacheProgressEventArgs() { Item = op.VideoCacheItem });
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

                        var result = PrepareNextVideoCacheDownloadingResult.Success(item.VideoId, item, op, StartDownload);
                        return result;
                    }
                    else
                    {
                        if (opCreationResult.FailedReason != VideoCacheDownloadOperationFailedReason.RistrictedDownloadLineCount)
                        {
                            item.FailedReason = opCreationResult.FailedReason;
                            item.Status = VideoCacheStatus.Failed;

                            UpdateVideoCacheEntity(item);

                            Failed?.Invoke(this, new VideoCacheFailedEventArgs() { Item = item, VideoCacheDownloadOperationCreationFailedReason = item.FailedReason });
                        }

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
                        videoSessionOwnershipRentResult.Dispose();
                        return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.CanNotCacheEncryptedContent);
                    }
                }
                else if (watchData?.DmcWatchResponse?.Media?.Delivery is null)
                {
                    videoSessionOwnershipRentResult.Dispose();

                    Debug.WriteLine(watchData.DmcWatchResponse.OkReason);
                    var reason = watchData.DmcWatchResponse.OkReason switch
                    {
                        "PREMIUM_ONLY_VIDEO_PREVIEW_SUPPORTED" => VideoCacheDownloadOperationFailedReason.RequirePermission_Premium,
                        "CHANNEL_ADMISSION_PREVIEW_SUPPORTED" => VideoCacheDownloadOperationFailedReason.RequirePermission_Admission,
                        _ => VideoCacheDownloadOperationFailedReason.Unknown,
                    };

                    return new VideoCacheDownloadOperationCreationResult(reason);
                }
                /*
                else if (watchData.DmcWatchResponse.Payment.Video.IsAdmission && !watchData.DmcWatchResponse.Payment.Preview.Admission.IsEnabled)
                {
                    videoSessionOwnershipRentResult.Dispose();
                    return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.RequirePermission_Admission);
                }
                else if (watchData.DmcWatchResponse.Payment.Video.IsPpv && !watchData.DmcWatchResponse.Payment.Preview.Ppv.IsEnabled)
                {
                    videoSessionOwnershipRentResult.Dispose();
                    return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.RequirePermission_Ppv);
                }
                else if (watchData.DmcWatchResponse.Payment.Video.IsPremium)
                {
                    videoSessionOwnershipRentResult.Dispose();
                    return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason.RequirePermission_Ppv);
                }
                */



                NicoVideoQuality candidateDownloadingQuality = DefaultCacheQuality;
                if (item.DownloadedVideoQuality is not NicoVideoQuality.Unknown) 
                {
                    candidateDownloadingQuality = item.DownloadedVideoQuality;
                }
                else if (item.RequestedVideoQuality is not NicoVideoQuality.Unknown)
                {
                    candidateDownloadingQuality = item.RequestedVideoQuality;
                }
#if !DEBUG
                if (item.Status is VideoCacheStatus.DownloadPaused or VideoCacheStatus.Downloading)
#else
                if (false)
#endif
                {
                    // Note: プレミアム会員のみ利用可能なので item.DownloadedVideoQuality が利用不可であることは想定しない
#pragma warning disable CS0162 // 到達できないコードが検出されました
                    candidateDownloadingQuality = item.DownloadedVideoQuality;
#pragma warning restore CS0162 // 到達できないコードが検出されました
                }
                else
                {
                    var avairableQualities = watchData.DmcWatchResponse.Media.Delivery.Movie.Session.Videos.Select(x => NicoVideoCacheQualityHelper.QualityIdToCacheQuality(x)).ToArray();
                    if (avairableQualities.Length == 0) { throw new Models.Infrastructure.HohoemaExpception("キャッシュ用画質Enumの変換に失敗"); }

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


                item.DownloadedVideoQuality = candidateDownloadingQuality;
                if (item.RequestedVideoQuality == NicoVideoQuality.Unknown)
                {
                    item.FileName = Path.ChangeExtension(await ResolveVideoFileName(item.VideoId, item.DownloadedVideoQuality), HohoemaVideoCacheExt);
                }


                StorageFile outputFile = null;
                if (item.Status is VideoCacheStatus.DownloadPaused or VideoCacheStatus.Downloading
                    && item.DownloadedVideoQuality == candidateDownloadingQuality
                    )
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

        internal async Task StartDownload(IVideoCacheDownloadOperation op)
        {
            var item = op.VideoCacheItem;

            try
            {
                var result = await op.DownloadAsync();

                using (await _updateLock.LockAsync())
                {
                    _currentDownloadOperations.Remove(item.VideoId);

                    (op as IDisposable).Dispose();
                }

                if (result == VideoCacheDownloadOperationCompleteState.DownloadPaused
                    || result == VideoCacheDownloadOperationCompleteState.ReturnDownloadSessionOwnership
                    )
                {
                    // do nothing
                    item.Status = VideoCacheStatus.DownloadPaused;
                }
                else if (result == VideoCacheDownloadOperationCompleteState.Completed)
                {
                    if (item.TotalBytes != item.ProgressBytes)
                    {
                        throw new VideoCacheException("Incomplete download, different TotalBytes and ProgressBytes.");
                    }

                    try
                    {
                        var file = await GetCacheVideoFileAsync(item.VideoId);
                        await HashComputeAndWritingToHashFileAsync(file);
                        item.Status = VideoCacheStatus.Completed;
                    }
                    catch
                    {
                        Debug.WriteLine("Failed Hash compute or writing,");
                        item.Status = VideoCacheStatus.Failed;
                        throw;
                    }
                }
                else if (result == VideoCacheDownloadOperationCompleteState.DownloadCanceledWithUser)
                {
                    item.Status = VideoCacheStatus.Failed;
                }
                else
                {
                    throw new VideoCacheException("op.DownloadAsync() returned unknown kind : " + result.ToString());
                }

                Debug.WriteLine("complete: " + item.FileName);
            }
            catch
            {
                // 不明なエラー
                item.Status = VideoCacheStatus.Failed;
                this.Failed?.Invoke(this, new VideoCacheFailedEventArgs()
                {
                    Item = item,
                    VideoCacheDownloadOperationCreationFailedReason = VideoCacheDownloadOperationFailedReason.Unknown,
                });
                throw;
            }
            finally
            {
                UpdateVideoCacheEntity(item);

                if (item.Status == VideoCacheStatus.DownloadPaused)
                {
                    this.Paused?.Invoke(this, new VideoCachePausedEventArgs() { Item = item });
                }
                else
                {
                    this.Completed?.Invoke(this, new VideoCacheCompletedEventArgs() { Item = item });
                }
            }
        }







        private static VideoCacheItem EntityToItem(VideoCacheManager manager, VideoCacheEntity entity)
        {
            return new VideoCacheItem(
                manager,
                entity.VideoId,
                entity.FileName,
                entity.Title,
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

            entity.FileName = item.FileName;
            entity.Title = item.Title;
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




        private async Task<StorageFile> GetCacheVideoFileAsync(string videoId)
        {
            var entity = _videoCacheItemRepository.GetVideoCache(videoId);
            if (entity is null) { return null; }
            return await VideoCacheFolder.TryGetItemAsync(entity.FileName) as StorageFile;
        }

        private Task<StorageFile> GetCacheVideoFileAsync(VideoCacheItem item)
        {
            return VideoCacheFolder.GetFileAsync(item.FileName).AsTask();
        }


        #region Hash Computing


        private async Task HashComputeAndWritingToHashFileAsync(StorageFile file)
        {
            var bytes = await ComputeHash(file);
            await SaveHashValue(file, bytes);
        }

        private Task SaveHashValue(StorageFile file, byte[] computedHash)
        {
            return VideoCacheFolder.WriteBytesToFileAsync(computedHash, GetHashFileName(file), CreationCollisionOption.ReplaceExisting);
        }


        const int HashComputeSize = 1024 ^ 3;

        private Task<byte[]> ComputeHash(StorageFile file)
        {             
            return Task.Run(async () =>
            {
                using (var hashAlgorithm = MakeHashAlgrorithm())
                using (var stream = new XtsStream(await file.OpenStreamForReadAsync(), Xts))
                {
                    byte[] buf = new byte[HashComputeSize];
                    var readLength = await stream.ReadAsync(buf, 0, buf.Length);
                    return hashAlgorithm.ComputeHash(buf, 0, readLength);
                }
            });
        }


        private Task<byte[]> ComputeHashLegacy(StorageFile file)
        {
            return Task.Run(async () =>
            {
                using (var hashAlgorithm = SHA256.Create())
                using (var stream = new XtsStream(await file.OpenStreamForReadAsync(), Xts))
                {
                    return hashAlgorithm.ComputeHash(stream);
                }
            });
        }

        private async Task<bool> IsFileContainsValidHashAsync(StorageFile file)
        {
            try
            {
                var (result, readBytes) = await GetComputedHashFromFileAsync(file);
                if (!result) { return false; }

                var computedBytes = await ComputeHash(file);
                
#if false
                // 正規のコード
                return StructuralComparisons.StructuralEqualityComparer.Equals(readBytes, computedBytes);
#else
                // ハッシュ計算変更前との互換用コード
                // 旧ハッシュ計算と同値であれば再生可能として、新しいハッシュ計算結果を書き込んで次回以降チェックをスキップ出来るようにする
                if (StructuralComparisons.StructuralEqualityComparer.Equals(readBytes, computedBytes))
                {
                    return true;
                }
                else
                {
                    var computedBytesLegacy = await ComputeHashLegacy(file);
                    if (StructuralComparisons.StructuralEqualityComparer.Equals(readBytes, computedBytesLegacy))
                    {
                        await SaveHashValue(file, computedBytes);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
#endif
            }
            catch 
            {
                return false;
            }
        }

        private async Task<(bool Result, byte[] ComputedHash)> GetComputedHashFromFileAsync(StorageFile file)
        {
            try
            {
                var bytes = await VideoCacheFolder.ReadBytesFromFileAsync(GetHashFileName(file));
                return (bytes != null, bytes);
            }
            catch
            {
                return (false, null);
            }
        }

        private async Task DeleteHashFile(StorageFile file)
        {
            var hashFile = await VideoCacheFolder.TryGetItemAsync(GetHashFileName(file));
            if (hashFile is StorageFile removeFile)
            {
                await removeFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }

        private string GetHashFileName(StorageFile file)
        {
            return Path.ChangeExtension(file.Name, HohoemaVideoCacheHashExt);
        }

#endregion Hash Computing



        /// <summary>
        /// 指定ファイルをDBにインポートする<br/> 
        /// 同一IDが存在する場合は上書き保存される。
        /// </summary>
        /// <remarks>videoIdに数字のみのIDが渡された場合sm/so等で始まるIDを取得して置き換える。このためオンラインでないと機能しない。</remarks>
        /// <param name="videoId"></param>
        /// <param name="quality"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        internal async Task ImportCacheRequestAsync(string videoId, NicoVideoQuality quality, StorageFile file)
        {
            var (isNumberId, realVideoId) = await GetVideoIdIfNumberVideoIdAsync(videoId);
            if (isNumberId)
            {
                videoId = realVideoId;
            }

            var title = await ResolveVideoTitle(videoId);

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
                item = new VideoCacheItem(this, videoId, file.Name, title, NicoVideoQuality.Unknown, quality, VideoCacheStatus.Completed, VideoCacheDownloadOperationFailedReason.None, file.DateCreated.DateTime, (long)prop.Size, (long)prop.Size, 0);
                _videoItemMap.Add(videoId, item);
            }

            var entity = _videoCacheItemRepository.GetVideoCache(item.VideoId);
            if (entity == null) 
            {
                entity = new VideoCacheEntity() { VideoId = videoId };
            }

            entity.FileName = item.FileName;
            entity.Title = item.Title;
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

#region Migarate Legacy

        /// <summary>
        /// 旧キャッシュ機能からの統合用。キャッシュリクエストを追加するのみでファイルは取り込まない。
        /// </summary>
        /// <remarks>videoIdに数字のみのIDが渡された場合sm/so等で始まるIDを取得して置き換える。このためオンラインでないと機能しない。</remarks>
        /// <param name="videoId"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        internal async Task PushCacheRequest_Legacy(string videoId, NicoVideoQuality quality)
        {
            var (isNumberId, realVideoId) = await GetVideoIdIfNumberVideoIdAsync(videoId);
            if (isNumberId)
            {
                videoId = realVideoId;
            }

            var entity = new VideoCacheEntity() { VideoId = videoId, RequestedVideoQuality = quality, Status = VideoCacheStatus.Pending };
            _videoCacheItemRepository.UpdateVideoCache(entity);
        }

#endregion
    }
}

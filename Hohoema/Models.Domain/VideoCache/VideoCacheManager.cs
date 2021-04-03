using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Infrastructure;
using LiteDB;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using XTSSharp;

namespace Hohoema.Models.Domain.VideoCache
{
    // Listring管理とDLセッション管理を分ける

    public enum VideoCacheStatus
    {
        Pending,
        Downloading,
        DownloadPaused,
        Completed,
        MigratedFromOldCache,
        Failed,
    }

    
    public enum NicoVideoCacheQuality
    {
        Unknown,

        SuperLow,
        Low,
        Midium,
        High,
        SuperHigh,
    }


    public struct VideoCacheDownloadOperationProgress
    {
        public long TotalBytes { get; set; }

        public long ProgressBytes { get; set; }

        public float GetNormalizedProgress()
        {
            return ProgressBytes / (float)TotalBytes;
        }
    }

    public interface IVideoCacheDownloadOperationOutput
    {
        Task CopyStreamAsync(Stream inputStream, IProgress<VideoCacheDownloadOperationProgress> progress, CancellationToken cancellationToken);
        Task DeleteAsync();
    }

    public class VideoCacheDownloadOperationOutputWithEncryption : IVideoCacheDownloadOperationOutput
    {
        private readonly StorageFile _outputFile;
        private readonly Xts _xts;

        public VideoCacheDownloadOperationOutputWithEncryption(StorageFile outputFile, XTSSharp.Xts xts)
        {
            _outputFile = outputFile;
            _xts = xts;
        }

        public async Task CopyStreamAsync(Stream inputStream, IProgress<VideoCacheDownloadOperationProgress> progress, CancellationToken cancellationToken)
        {
            var outputStream = await _outputFile.OpenStreamForReadAsync();

            // 途中までDLしていた場合はそこから再開
            if (outputStream.Length != 0)
            {
                outputStream.Seek(0, SeekOrigin.End);
                inputStream.Seek(outputStream.Length, SeekOrigin.Begin);
            }

            byte[] inputBuffer = new byte[XtsSectorStream.DEFAULT_SECTOR_SIZE];
            byte[] outputBuffer = new byte[XtsSectorStream.DEFAULT_SECTOR_SIZE];
            using (var rawVideoStream = inputStream)
            using (var outputFileStream = outputStream)
            using (var encryptor = _xts.CreateEncryptor())
            {
                cancellationToken.ThrowIfCancellationRequested();

                ulong currentSector = 0;
                int readLength = -1;
                while (readLength != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    readLength = await rawVideoStream.ReadAsync(inputBuffer, 0, inputBuffer.Length);
                    encryptor.TransformBlock(inputBuffer, 0, inputBuffer.Length, outputBuffer, 0, currentSector);
                    currentSector++;
                    await outputFileStream.WriteAsync(outputBuffer, 0, outputBuffer.Length);
                    await outputFileStream.FlushAsync();

                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(new VideoCacheDownloadOperationProgress() { ProgressBytes = rawVideoStream.Position, TotalBytes = rawVideoStream.Length });
                }
            }
        }

        public Task DeleteAsync()
        {
            return _outputFile.DeleteAsync().AsTask();
        }
    }

    public struct VideoCacheDownloadOperationStartResult
    {
        public bool IsSuccess { get; set; }
        public long TotalBytes { get; set; }
    }


    public class VideoCacheDownloadOperation : IDisposable
    {
        public VideoCacheItem VideoCacheItem { get; }
        private readonly DmcVideoStreamingSession _dmcVideoStreamingSession;
        private IVideoCacheDownloadOperationOutput _videoCacheDownloadOperationOutput;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _downloadTask;

        internal VideoCacheDownloadOperation(VideoCacheItem videoCacheItem, DmcVideoStreamingSession dmcVideoStreamingSession, IVideoCacheDownloadOperationOutput videoCacheDownloadOperationOutput)
        {
            VideoCacheItem = videoCacheItem;
            _dmcVideoStreamingSession = dmcVideoStreamingSession; 
            _videoCacheDownloadOperationOutput = videoCacheDownloadOperationOutput;
        }

        public async Task<VideoCacheDownloadOperationStartResult> StartOrResumeDownloadAsync(IProgress<VideoCacheDownloadOperationProgress> progress, Action<VideoCacheDownloadOperation> onCompleted)
        {
            IRandomAccessStream downloadStream = null;
            try
            {
                var uri = await _dmcVideoStreamingSession.GetDownloadUrlAndSetupDownloadSession();
                downloadStream = await HttpSequencialAccessStream.CreateAsync(_dmcVideoStreamingSession.NiconicoSession.Context.HttpClient, uri);
            }
            catch
            {
                downloadStream?.Dispose();
                return new VideoCacheDownloadOperationStartResult() { IsSuccess = false };
            }

            _cancellationTokenSource = new CancellationTokenSource();

            _downloadTask = _videoCacheDownloadOperationOutput.CopyStreamAsync(downloadStream.AsStreamForRead(), progress, _cancellationTokenSource.Token)
                .ContinueWith(prevTask => { onCompleted?.Invoke(this); });

            return new VideoCacheDownloadOperationStartResult() { IsSuccess = true, TotalBytes = (long)downloadStream.Size };
        }

        public async Task StopAndDeleteDownloadedAsync()
        {
            if (_cancellationTokenSource is null) { return; }

            _cancellationTokenSource.Cancel();

            await _videoCacheDownloadOperationOutput.DeleteAsync();
        }

        void IDisposable.Dispose()
        {
            _dmcVideoStreamingSession?.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }


    public class VideoCacheDownloadOperationCreationResult
    {
        internal VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperation downloadOperation)
        {
            IsSuccess = true;
            DownloadOperation= downloadOperation;
            FailedReason = VideoCacheDownloadOperationCreationFailedReason.None;
        }

        internal VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason reason)
        {
            IsSuccess = false;
            DownloadOperation = null;
            FailedReason = reason;
        }

        public bool IsSuccess { get; }

        public VideoCacheDownloadOperation DownloadOperation { get; }

        public VideoCacheDownloadOperationCreationFailedReason FailedReason { get; }
    }

    public enum VideoCacheDownloadOperationCreationFailedReason
    {
        Unknown,
        None,
        InternetUnavairable,
        NoUsageAuthority,
        CanNotCacheEncryptedContent,
        StorageCapacityLimitReached,
        RistrictedDownloadLineCount,
        NoMorePendingOrPausingItem,
    }

    public static class NicoVideoCacheQualityHelper
    {
        public static NicoVideoCacheQuality QualityIdToCacheQuality(string qualityId)
        {
            return qualityId switch
            {
                "archive_h264_1080p" => NicoVideoCacheQuality.SuperHigh,
                "archive_h264_720p" => NicoVideoCacheQuality.High,
                "archive_h264_480p" => NicoVideoCacheQuality.Midium,
                "archive_h264_360p" => NicoVideoCacheQuality.Low,
                "archive_h264_360p_low" => NicoVideoCacheQuality.SuperLow,
                _ => NicoVideoCacheQuality.Unknown,
            };
        }

        public static string CacheQualityToQualityId(NicoVideoCacheQuality quality)
        {
            return quality switch
            {
                NicoVideoCacheQuality.SuperLow => "archive_h264_1080p",
                NicoVideoCacheQuality.Low => "archive_h264_720p",
                NicoVideoCacheQuality.Midium => "archive_h264_480p",
                NicoVideoCacheQuality.High => "archive_h264_360p",
                NicoVideoCacheQuality.SuperHigh => "archive_h264_360p_low",
                _ => throw new NotSupportedException()
            };
        }

        public static bool TryGetOneLowerQuality(NicoVideoCacheQuality quality, out NicoVideoCacheQuality outQuality)
        {
            outQuality = GetOneLowerQuality(quality);

            return outQuality != NicoVideoCacheQuality.Unknown;
        }

        public static NicoVideoCacheQuality GetOneLowerQuality(NicoVideoCacheQuality quality)
        {
            return quality switch
            {
                NicoVideoCacheQuality.SuperHigh => NicoVideoCacheQuality.High,
                NicoVideoCacheQuality.High => NicoVideoCacheQuality.Midium,
                NicoVideoCacheQuality.Midium => NicoVideoCacheQuality.Low,
                NicoVideoCacheQuality.Low => NicoVideoCacheQuality.SuperLow,
                NicoVideoCacheQuality.SuperLow => NicoVideoCacheQuality.Unknown,
                _ => NicoVideoCacheQuality.Unknown,
            };
        }
    }

    public class VideoCacheItem
    {
        public string VideoId { get; }

        public NicoVideoCacheQuality RequestedVideoQuality { get; internal set; }

        public NicoVideoCacheQuality DownloadedVideoQuality { get; internal set; }

        public VideoCacheStatus Status { get; internal set; }

        public VideoCacheDownloadOperationCreationFailedReason FailedReason { get; internal set; }

        public long? TotalBytes { get; internal set; }

        public long? ProgressBytes { get; internal set; }

        public DateTime RequestedAt { get; internal set; }

        public int SortIndex { get; internal set; }

        public bool IsCompleted => Status == VideoCacheStatus.Completed;

        internal VideoCacheItem(
            string videoId, 
            NicoVideoCacheQuality requestedQuality, 
            NicoVideoCacheQuality downloadedQuality, 
            VideoCacheStatus status,
            VideoCacheDownloadOperationCreationFailedReason failedReason,
            DateTime requestAt,  
            long? totalBytes, 
            long? progressBytes,
            int sortIndex
            )
        {
            VideoId = videoId;
            RequestedVideoQuality = requestedQuality;
            DownloadedVideoQuality = downloadedQuality;
            Status = status;
            TotalBytes = totalBytes;
            ProgressBytes = progressBytes;
            RequestedAt = requestAt;
            SortIndex = sortIndex;
        }

        internal void SetProgress(VideoCacheDownloadOperationProgress progress)
        {
            TotalBytes = progress.TotalBytes;
            ProgressBytes = progress.ProgressBytes;
            _progressNormalized = progress.GetNormalizedProgress();
        }

        private float? _progressNormalized;
        public float GetProgressNormalized()
        {
            return _progressNormalized ?? (IsCompleted ? 1.0f : 0.0f);
        }
    }

    public class VideoCacheTryNextDownloadResult
    {
        public string VideoId { get; }
        public bool IsSuccess => FailedReason is VideoCacheDownloadOperationCreationFailedReason.None;
        public VideoCacheDownloadOperationCreationFailedReason FailedReason { get; }
        public VideoCacheDownloadOperation DownloadOperation { get; }


        internal VideoCacheTryNextDownloadResult(string videoId, VideoCacheDownloadOperation downloadOperation)
        {
            VideoId = videoId;
            DownloadOperation = downloadOperation;
        }

        internal VideoCacheTryNextDownloadResult(string videoId, VideoCacheDownloadOperationCreationFailedReason creationFailedReason)
        {
            VideoId = videoId;
            FailedReason = creationFailedReason;
        }
    }


    public sealed class VideoCacheSettings : FlagsRepositoryBase
    {
        public VideoCacheSettings()
        {
            _MaxVideoCacheStorageSize = Read<long?>(default, nameof(MaxVideoCacheStorageSize));
            _IsAllowDownloadOnRestrictedNetwork = Read<bool>(false, nameof(IsAllowDownloadOnRestrictedNetwork));
            _CachedStorageSize = Read<long>(0, nameof(CachedStorageSize));
        }

        private long? _MaxVideoCacheStorageSize;
        public long? MaxVideoCacheStorageSize
        {
            get => _MaxVideoCacheStorageSize;
            set => Save(_MaxVideoCacheStorageSize = value);
        }


        private bool _IsAllowDownloadOnRestrictedNetwork;
        public bool IsAllowDownloadOnRestrictedNetwork
        {
            get => _IsAllowDownloadOnRestrictedNetwork;
            set => Save(_IsAllowDownloadOnRestrictedNetwork = value);
        }

        private long _CachedStorageSize;
        public long CachedStorageSize
        {
            get => _CachedStorageSize;
            set => Save(_CachedStorageSize = value);
        }
    }


    
    public sealed class VideoCacheEntity
    {
        [BsonId]
        public string VideoId { get; set; }

        public NicoVideoCacheQuality RequestedVideoQuality { get; set; }

        public NicoVideoCacheQuality DownloadedVideoQuality { get; set; }

        public VideoCacheStatus Status { get; set; }
        
        public VideoCacheDownloadOperationCreationFailedReason FailedReason { get; set; }

        public long? TotalBytes { get; set; }

        public long? ProgressBytes { get; set; }

        public DateTime RequestedAt { get; set; }

        public int SortIndex { get; set; }
    }

    public sealed class VideoCacheItemRepository
    {
        public class VideoCacheDbService : LiteDBServiceBase<VideoCacheEntity>
        {
            public VideoCacheDbService(LiteDatabase liteDatabase) : base(liteDatabase)
            {
            }

            public IEnumerable<VideoCacheEntity> FindByStatus(VideoCacheStatus status)
            {
                return _collection.Find(x => x.Status == status);
            }

            public long SumVideoCacheSize()
            {
                return _collection.FindAll().Sum(x => x.TotalBytes ?? 0);
            }
        }

        private readonly VideoCacheDbService _videoCacheDbService;

        public VideoCacheItemRepository(VideoCacheDbService videoCacheDbService)
        {
            _videoCacheDbService = videoCacheDbService;
        }

        public IEnumerable<VideoCacheEntity> FindByStatus(VideoCacheStatus status)
        {
            return _videoCacheDbService.FindByStatus(status);
        }


        public VideoCacheEntity GetVideoCache(string id)
        {
            return _videoCacheDbService.FindById(id);
        }

        public void UpdateVideoCache(VideoCacheEntity entity)
        {
            _videoCacheDbService.UpdateItem(entity);
        }

        public void DeleteVideoCache(string videoId)
        {
            _videoCacheDbService.DeleteItem(videoId);
        }

        public long SumVideoCacheSize()
        {
            return _videoCacheDbService.SumVideoCacheSize();
        }
    }

    public sealed class VideoCacheException : Exception
    {
        public VideoCacheException()
        {
        }

        public VideoCacheException(string message) : base(message)
        {
        }

        public VideoCacheException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public VideoCacheException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }



    public sealed class VideoCacheManager : IDisposable
    {
        public static Func<string, Task<string>> ResolveVideoFileNameWithoutExtFromVideoId { get; set; } = (id) => Task.FromResult(id);
        public static Func<Task<bool>> CheckUsageAuthorityAsync { get; set; } = () => Task.FromResult(true);

        private readonly NiconicoSession _niconicoSession;
        private readonly NicoVideoSessionOwnershipManager _videoSessionOwnershipManager;
        private readonly VideoCacheItemRepository _videoCacheItemRepository;

        public VideoCacheSettings VideoCacheSettings { get; }
        public Xts Xts { get; }
        public StorageFolder VideoCacheFolder { get; set; }

        CompositeDisposable _disposables = new CompositeDisposable();
        Dictionary<string, VideoCacheDownloadOperation> _currentDownloadOperations = new Dictionary<string, VideoCacheDownloadOperation>();
        AsyncLock _updateLock = new AsyncLock();

        public event EventHandler<VideoCacheTryNextDownloadResult> TryNextDownloadResult;

        public VideoCacheManager(
            NiconicoSession niconicoSession, 
            NicoVideoSessionOwnershipManager videoSessionOwnershipManager,
            VideoCacheSettings videoCacheSettings,
            VideoCacheItemRepository videoCacheItemRepository
            )
        {
            _niconicoSession = niconicoSession;
            _videoSessionOwnershipManager = videoSessionOwnershipManager;
            VideoCacheSettings = videoCacheSettings;
            _videoCacheItemRepository = videoCacheItemRepository;
            VideoCacheSettings.ObserveProperty(x => x.MaxVideoCacheStorageSize)
                .Subscribe(size => 
                {
                    // TODO: 容量制限を越えてDLしようとしている状態の場合に、現在のDLラインをキャンセルして、リクエストを失敗状態に変更する
                })
                .AddTo(_disposables);

            VideoCacheSettings.ObserveProperty(x => x.IsAllowDownloadOnRestrictedNetwork)
                .Subscribe(allow => 
                {
                    // TODO: 従量課金通信かどうかを調べてDL不可条件に当てはまる場合は、現在のDLラインをキャンセルして、リクエストを失敗状態に変更する
                })
                .AddTo(_disposables);
        }

        #region interface IDisposable

        public void Dispose()
        {
            ((IDisposable)_disposables).Dispose();
        }

        #endregion

        internal async Task<bool> CheckStorageCapacityLimitReachedAsync()
        {
            VideoCacheSettings.CachedStorageSize = _videoCacheItemRepository.SumVideoCacheSize();

            if (VideoCacheSettings.MaxVideoCacheStorageSize is not null and var maxSize)
            {
                return VideoCacheSettings.CachedStorageSize > maxSize;
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
                
                var prevStatus = entity?.Status;
                if (entity == null)
                {
                    entity = new VideoCacheEntity() { VideoId = videoId };
                }
                
                switch (entity.Status)
                {
                    case VideoCacheStatus.Pending:

                        break;
                    case VideoCacheStatus.Downloading:

                        break;
                    case VideoCacheStatus.DownloadPaused:
                        entity.Status = VideoCacheStatus.Pending;
                        break;
                    case VideoCacheStatus.Completed:
                        if (entity.RequestedVideoQuality != entity.DownloadedVideoQuality)
                        {
                            entity.Status = VideoCacheStatus.Pending;
                        }
                        break;
                    case VideoCacheStatus.MigratedFromOldCache:
                        entity.Status = VideoCacheStatus.Pending;
                        break;
                    case VideoCacheStatus.Failed:
                        entity.Status = VideoCacheStatus.Pending;
                        break;
                }

                if (entity.Status == VideoCacheStatus.Pending)
                {
                    entity.FailedReason = VideoCacheDownloadOperationCreationFailedReason.None;
                    entity.RequestedVideoQuality = requestCacheQuality;

                    if (prevStatus != VideoCacheStatus.Pending)
                    {
                        entity.RequestedAt = DateTime.Now;
                    }
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

                // ローカルDBから削除
                _videoCacheItemRepository.DeleteVideoCache(videoId);

                // ファイルとして削除
                var file = await GetCacheVideoFileAsync(videoId);
                if (file is not null)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
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

        
        public async ValueTask<VideoCacheTryNextDownloadResult> TryStartNextCacheDownloadingTaskAsync()
        {
            using (await _updateLock.LockAsync())
            {
                var item = GetNextDownloadVideoCacheItem();
                if (item == null)
                {
                    var result = new VideoCacheTryNextDownloadResult(string.Empty, VideoCacheDownloadOperationCreationFailedReason.NoMorePendingOrPausingItem);
                    TryNextDownloadResult?.Invoke(this, result);
                    return result;
                }

                try
                {
                    var opCreationResult = await CreateDownloadOperationAsync(item);

                    if (!opCreationResult.IsSuccess)
                    {
                        var op = opCreationResult.DownloadOperation;
                        var startResult = await op.StartOrResumeDownloadAsync(new Progress<VideoCacheDownloadOperationProgress>(progress => OnProgress(op, progress)), (op) => _ = FinishDownloadOperation(op));
                        if (!startResult.IsSuccess)
                        {
                            (op as IDisposable).Dispose();
                            throw new VideoCacheException();
                        }

                        _currentDownloadOperations.Add(item.VideoId, op);

                        item.TotalBytes = startResult.TotalBytes;
                        item.Status = VideoCacheStatus.Downloading;
                        UpdateVideoCacheEntity(item);

                        var result = new VideoCacheTryNextDownloadResult(item.VideoId, op);
                        TryNextDownloadResult?.Invoke(this, result);
                        return result;
                    }
                    else
                    {
                        item.FailedReason = opCreationResult.FailedReason;
                        item.Status = VideoCacheStatus.Failed;
                        UpdateVideoCacheEntity(item);

                        var result = new VideoCacheTryNextDownloadResult(item.VideoId, opCreationResult.FailedReason);
                        TryNextDownloadResult?.Invoke(this, result);
                        return result;
                    }

                }
                catch (Exception e)
                {
                    item.FailedReason = VideoCacheDownloadOperationCreationFailedReason.Unknown;
                    item.Status = VideoCacheStatus.Failed;
                    UpdateVideoCacheEntity(item);

                    var result = new VideoCacheTryNextDownloadResult(item.VideoId, item.FailedReason);
                    TryNextDownloadResult?.Invoke(this, result);
                    throw new VideoCacheException($"Failed video cache download starting. videoId: {item?.VideoId}", e);
                }
            }
        }

        private Task<StorageFile> CreateCacheVideoFileAsync(VideoCacheItem item, CreationCollisionOption creationCollisionOption)
        {
            return CreateCacheVideoFileAsync(item.VideoId, creationCollisionOption);
        }

        

        private async Task<StorageFile> CreateCacheVideoFileAsync(string videoId, CreationCollisionOption creationCollisionOption)
        {
            // キャッシュ保存先フォルダから指定動画IDのファイルを検索し、存在すればOpenIfExistsで開き、無ければ作成
            var fileNameWithExt = Path.ChangeExtension(await ResolveVideoFileNameWithoutExtFromVideoId(videoId), ".mp4");
            return await VideoCacheFolder.CreateFileAsync(fileNameWithExt, creationCollisionOption);
        }


        private Task<StorageFile> GetCacheVideoFileAsync(VideoCacheItem item)
        {
            return GetCacheVideoFileAsync(item.VideoId);
        }

        private async Task<StorageFile> GetCacheVideoFileAsync(string videoId)
        {
            // キャッシュ保存先フォルダから指定動画IDのファイルを検索し、存在すればOpenIfExistsで開き、無ければ作成
            var fileNameWithExt = Path.ChangeExtension(await ResolveVideoFileNameWithoutExtFromVideoId(videoId), ".mp4");
            return await VideoCacheFolder.GetFileAsync(fileNameWithExt);
        }


        private void OnProgress(VideoCacheDownloadOperation op, VideoCacheDownloadOperationProgress progress)
        {
            op.VideoCacheItem.SetProgress(progress);

            UpdateVideoCacheEntity(op.VideoCacheItem);            
        }

        internal async Task FinishDownloadOperation(VideoCacheDownloadOperation op)
        {
            using (await _updateLock.LockAsync())
            {
                var item = op.VideoCacheItem;
                _currentDownloadOperations.Remove(item.VideoId);

                (op as IDisposable).Dispose();

                if (item.TotalBytes == item.ProgressBytes)
                {
                    item.Status = VideoCacheStatus.Completed;
                }
                else
                {
                    item.Status = VideoCacheStatus.Failed;
                }

                UpdateVideoCacheEntity(item);
            }
        }


        
        internal async Task<VideoCacheDownloadOperationCreationResult> CreateDownloadOperationAsync(VideoCacheItem item)
        {
            if (Helpers.InternetConnection.IsInternet() is false) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason.InternetUnavairable); }

            if (await CheckUsageAuthorityAsync() is false) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason.NoUsageAuthority); }

            if (await this.CheckStorageCapacityLimitReachedAsync() is true) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason.StorageCapacityLimitReached); }

            var rentResult = await _videoSessionOwnershipManager.TryRentVideoSessionOwnershipAsync(item.VideoId, isPriorityRent: false);
            if (rentResult is null) { return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason.RistrictedDownloadLineCount); }

            var watchData = await _niconicoSession.Context.Video.GetDmcWatchResponseAsync(item.VideoId);
            if (watchData?.DmcWatchResponse?.Media?.Delivery is not null and var deliverly)
            {
                if (deliverly.Encryption is not null)
                {
                    return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason.CanNotCacheEncryptedContent);
                }
            }
            else if (watchData?.DmcWatchResponse?.Media?.Delivery is null && watchData?.DmcWatchResponse?.Media?.DeliveryLegacy is not null)
            {
                return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason.Unknown);
            }

            NicoVideoCacheQuality candidateDownloadingQuality = item.RequestedVideoQuality;
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

            var prevDownloadedVideoQuality = item.DownloadedVideoQuality;

            item.DownloadedVideoQuality = candidateDownloadingQuality;

            try
            {
                StorageFile outputFile = null;
                if (prevDownloadedVideoQuality == item.DownloadedVideoQuality
                    && item.ProgressBytes != 0
                    )
                {
                    outputFile = await GetCacheVideoFileAsync(item);
                }

                if (outputFile == null)
                {
                    outputFile = await CreateCacheVideoFileAsync(item, CreationCollisionOption.ReplaceExisting);
                }

                var dmcVideoStreamingSession = new DmcVideoStreamingSession(NicoVideoCacheQualityHelper.CacheQualityToQualityId(candidateDownloadingQuality), watchData, _niconicoSession, rentResult);
                var op = new VideoCacheDownloadOperation(item, dmcVideoStreamingSession, new VideoCacheDownloadOperationOutputWithEncryption(outputFile, Xts));

                return new VideoCacheDownloadOperationCreationResult(op);
            }
            catch
            {
                item.DownloadedVideoQuality =  NicoVideoCacheQuality.Unknown;
                return new VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationCreationFailedReason.Unknown);
            }
        }

    }
}

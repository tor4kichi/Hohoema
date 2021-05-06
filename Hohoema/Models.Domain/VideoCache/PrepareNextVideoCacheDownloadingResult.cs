using System;
using System.Threading.Tasks;
using Uno.Disposables;

namespace Hohoema.Models.Domain.VideoCache
{
    public class PrepareNextVideoCacheDownloadingResult
    {
        public string VideoId { get; }
        public VideoCacheItem VideoCacheItem { get; }
        public VideoCacheDownloadOperationFailedReason FailedReason { get; }

        public bool IsSuccess => FailedReason is VideoCacheDownloadOperationFailedReason.None;

        /// <summary>
        /// DLの実行主体<br />
        /// ライフタイム管理をVideoCacheManagerに任せたいのでpublicにしない
        /// </summary>
        private readonly IVideoCacheDownloadOperation _downloadOperation;


        internal static PrepareNextVideoCacheDownloadingResult Success(string videoId, VideoCacheItem videoCacheItem, IVideoCacheDownloadOperation downloadOperation)
        {
            return new PrepareNextVideoCacheDownloadingResult(videoId, videoCacheItem, downloadOperation);
        }

        internal static PrepareNextVideoCacheDownloadingResult Failed(string videoId, VideoCacheItem videoCacheItem, VideoCacheDownloadOperationFailedReason creationFailedReason)
        {
            return new PrepareNextVideoCacheDownloadingResult(videoId, videoCacheItem, creationFailedReason);
        }

        private PrepareNextVideoCacheDownloadingResult(string videoId, VideoCacheItem videoCacheItem, IVideoCacheDownloadOperation downloadOperation)
        {
            VideoId = videoId;
            VideoCacheItem = videoCacheItem;
            _downloadOperation = downloadOperation;
            FailedReason = VideoCacheDownloadOperationFailedReason.None;
        }

        private PrepareNextVideoCacheDownloadingResult(string videoId, VideoCacheItem videoCacheItem, VideoCacheDownloadOperationFailedReason creationFailedReason)
        {
            VideoId = videoId;
            VideoCacheItem = videoCacheItem;
            FailedReason = creationFailedReason;
        }

        public Task DownloadAsync()
        {
            return _downloadOperation.DownloadAsync();
        }
    }
}

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
        private readonly Func<IVideoCacheDownloadOperation, Task> _downloadTaskFactory;

        internal static PrepareNextVideoCacheDownloadingResult Success(string videoId, VideoCacheItem videoCacheItem, IVideoCacheDownloadOperation downloadOperation, Func<IVideoCacheDownloadOperation, Task> downloadTaskFactory)
        {
            return new PrepareNextVideoCacheDownloadingResult(videoId, videoCacheItem, downloadOperation, downloadTaskFactory);
        }

        internal static PrepareNextVideoCacheDownloadingResult Failed(string videoId, VideoCacheItem videoCacheItem, VideoCacheDownloadOperationFailedReason creationFailedReason)
        {
            return new PrepareNextVideoCacheDownloadingResult(videoId, videoCacheItem, creationFailedReason);
        }

        private PrepareNextVideoCacheDownloadingResult(string videoId, VideoCacheItem videoCacheItem, IVideoCacheDownloadOperation downloadOperation, Func<IVideoCacheDownloadOperation, Task> downloadTaskFactory)
        {
            VideoId = videoId;
            VideoCacheItem = videoCacheItem;
            _downloadOperation = downloadOperation;
            _downloadTaskFactory = downloadTaskFactory;
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
            return _downloadTaskFactory(_downloadOperation);
        }
    }
}

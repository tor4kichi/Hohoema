using System;

namespace Hohoema.Models.Domain.VideoCache
{
    public class VideoCacheItem
    {
        public string VideoId { get; }

        public NicoVideoCacheQuality RequestedVideoQuality { get; internal set; }

        public NicoVideoCacheQuality DownloadedVideoQuality { get; internal set; }

        public VideoCacheStatus Status { get; internal set; }

        public VideoCacheDownloadOperationFailedReason FailedReason { get; internal set; }

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
            VideoCacheDownloadOperationFailedReason failedReason,
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
}

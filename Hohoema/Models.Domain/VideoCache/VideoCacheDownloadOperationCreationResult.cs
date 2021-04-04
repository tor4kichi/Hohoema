namespace Hohoema.Models.Domain.VideoCache
{
    public class VideoCacheDownloadOperationCreationResult
    {
        internal VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperation downloadOperation)
        {
            IsSuccess = true;
            DownloadOperation= downloadOperation;
            FailedReason = VideoCacheDownloadOperationFailedReason.None;
        }

        internal VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason reason)
        {
            IsSuccess = false;
            DownloadOperation = null;
            FailedReason = reason;
        }

        public bool IsSuccess { get; }

        public VideoCacheDownloadOperation DownloadOperation { get; }

        public VideoCacheDownloadOperationFailedReason FailedReason { get; }
    }
}

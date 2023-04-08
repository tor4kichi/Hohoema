namespace Hohoema.Models.VideoCache;

public class VideoCacheDownloadOperationCreationResult
{
    internal VideoCacheDownloadOperationCreationResult(IVideoCacheDownloadOperation op)
    {
        IsSuccess = true;
        DownloadOperation = op;
        FailedReason = VideoCacheDownloadOperationFailedReason.None;
    }

    internal VideoCacheDownloadOperationCreationResult(VideoCacheDownloadOperationFailedReason reason)
    {
        IsSuccess = false;
        FailedReason = reason;
    }

    public bool IsSuccess { get; }

    public IVideoCacheDownloadOperation DownloadOperation;

    public VideoCacheDownloadOperationFailedReason FailedReason { get; }
}

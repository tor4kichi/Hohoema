#nullable enable
namespace Hohoema.Models.VideoCache;

public struct VideoCacheDownloadOperationResult
{
    public bool IsSuccess { get; set; }
    public long TotalBytes { get; set; }
}

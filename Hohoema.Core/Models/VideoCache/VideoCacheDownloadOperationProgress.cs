#nullable enable
namespace Hohoema.Models.VideoCache;

public struct VideoCacheDownloadOperationProgress
{
    public long TotalBytes { get; set; }

    public long ProgressBytes { get; set; }

    public float GetNormalizedProgress()
    {
        return ProgressBytes / (float)TotalBytes;
    }
}

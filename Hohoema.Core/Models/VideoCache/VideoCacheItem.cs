#nullable enable
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Video;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace Hohoema.Models.VideoCache;

public class VideoCacheItem
{
    public VideoId VideoId { get; }

    public string FileName { get; internal set; }

    public string Title { get; internal set; }

    public NicoVideoQuality RequestedVideoQuality { get; internal set; }

    public NicoVideoQuality DownloadedVideoQuality { get; internal set; }

    public VideoCacheStatus Status { get; internal set; }

    public VideoCacheDownloadOperationFailedReason FailedReason { get; internal set; }

    public long? TotalBytes { get; internal set; }

    public long? ProgressBytes { get; internal set; }

    public DateTime RequestedAt { get; internal set; }

    public int SortIndex { get; internal set; }

    public bool IsCompleted => Status == VideoCacheStatus.Completed;

    internal VideoCacheItem(
        VideoCacheManager videoCacheManager,
        string videoId,
        string fileName,
        string title,
        NicoVideoQuality requestedQuality,
        NicoVideoQuality downloadedQuality,
        VideoCacheStatus status,
        VideoCacheDownloadOperationFailedReason failedReason,
        DateTime requestAt,
        long? totalBytes,
        long? progressBytes,
        int sortIndex
        )
    {
        _videoCacheManager = videoCacheManager;
        VideoId = videoId;
        FileName = fileName;
        Title = title;
        RequestedVideoQuality = requestedQuality;
        DownloadedVideoQuality = downloadedQuality;
        Status = status;
        FailedReason = failedReason;
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
    private readonly VideoCacheManager _videoCacheManager;

    public float GetProgressNormalized()
    {
        if (_progressNormalized is not null) { return _progressNormalized.Value; }

        if (Status is VideoCacheStatus.Downloading or VideoCacheStatus.DownloadPaused)
        {
            _progressNormalized = TotalBytes is null or 0 ? 0.0f : (ProgressBytes ?? 0) / (float)TotalBytes;
        }

        return _progressNormalized ?? (IsCompleted ? 1.0f : 0.0f);
    }

    public Task<MediaSource> GetMediaSourceAsync()
    {
        return _videoCacheManager.GetCacheVideoMediaSource(this);
    }
}

using System;
using System.Threading.Tasks;

namespace Hohoema.Models.VideoCache;

public interface IVideoCacheDownloadOperation
{
    VideoCacheItem VideoCacheItem { get; }
    string VideoId { get; }

    event EventHandler Completed;
    event EventHandler<VideoCacheDownloadOperationProgress> Progress;
    event EventHandler Started;

    Task<VideoCacheDownloadOperationCompleteState> DownloadAsync();
    Task CancelAsync();
    Task PauseAsync();
}
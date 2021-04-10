using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.VideoCache
{
    public interface IVideoCacheDownloadOperation
    {
        VideoCacheItem VideoCacheItem { get; }
        string VideoId { get; }

        event EventHandler Completed;
        event EventHandler Paused;
        event EventHandler<VideoCacheDownloadOperationProgress> Progress;
        event EventHandler Started;

        Task DownloadAsync();
        Task StopAndDeleteDownloadedAsync();
        Task PauseAsync();
    }
}
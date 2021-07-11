using Hohoema.Models.Domain.Niconico.Video;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Player.Video
{

    public interface IVideoStreamingSession : IStreamingSession
    {
        string QualityId { get; }
    }

    public interface IVideoStreamingDownloadSession : IVideoStreamingSession
    {
        Task<Uri> GetDownloadUrlAndSetupDownloadSession();
    }

}
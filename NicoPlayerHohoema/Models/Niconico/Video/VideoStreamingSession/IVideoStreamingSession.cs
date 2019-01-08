using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace NicoPlayerHohoema.Models
{
    public interface IStreamingSession : IDisposable
    {
        Task StartPlayback(MediaPlayer player);
    }
    
    public interface IVideoStreamingSession : IStreamingSession
    {
        NicoVideoQuality Quality { get; }
    }

    public interface IVideoStreamingDownloadSession : IVideoStreamingSession
    {
        Task<Uri> GetDownloadUrlAndSetupDonwloadSession();
    }

}
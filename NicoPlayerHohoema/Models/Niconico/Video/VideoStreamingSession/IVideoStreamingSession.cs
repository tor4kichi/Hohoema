using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace NicoPlayerHohoema.Models
{
    public interface IVideoStreamingSession : IDisposable
    {
        NicoVideoQuality Quality { get; }

        Task<Uri> GetDownloadUrlAndSetupDonwloadSession();
        Task StartPlayback(MediaPlayer player);
    }
}
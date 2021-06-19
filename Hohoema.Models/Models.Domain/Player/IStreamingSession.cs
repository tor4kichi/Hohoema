using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.Models.Domain.Player
{
    public interface IStreamingSession : IDisposable
    {
        Task StartPlayback(MediaPlayer player, TimeSpan initialPosition = default);
    }

}
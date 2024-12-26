#nullable enable
using Hohoema.Models.Niconico.Video;
using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.Models.Player;

public interface IStreamingSession : IDisposable
{
    NicoVideoQuality Quality { get; }
    Task SetMediaSourceToPlayer(MediaPlayer player, TimeSpan initialPosition = default, bool play = true);
}
#nullable enable
using Hohoema.Models.Niconico.Video;
using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace Hohoema.Models.Player;

public interface IStreamingSession : IDisposable
{
    NicoVideoQuality Quality { get; }
    Task StartPlayback(MediaPlayer player, TimeSpan initialPosition = default);
}
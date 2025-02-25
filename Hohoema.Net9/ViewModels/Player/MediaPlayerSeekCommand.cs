#nullable enable
using System;
using Windows.Media.Playback;

namespace Hohoema.ViewModels.Player.Commands;

public sealed partial class MediaPlayerSeekCommand : CommandBase
{
    private readonly MediaPlayer _mediaPlayer;

    public MediaPlayerSeekCommand(MediaPlayer mediaPlayer)
    {
        _mediaPlayer = mediaPlayer;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is TimeSpan?;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is TimeSpan timeDelta)
        {
            _mediaPlayer.PlaybackSession.Position += timeDelta;
        }
    }
}

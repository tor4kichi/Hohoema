#nullable enable
using Hohoema.Models.Player;
using System;
using Windows.Media.Playback;

namespace Hohoema.ViewModels.Player.Commands;

public sealed partial class MediaPlayerSetPlaybackRateCommand : CommandBase
{
    private readonly MediaPlayer _mediaPlayer;
    private readonly PlayerSettings _playerSettings;

    public MediaPlayerSetPlaybackRateCommand(
        MediaPlayer mediaPlayer,
        PlayerSettings playerSettings
        )
    {
        _mediaPlayer = mediaPlayer;
        _playerSettings = playerSettings;
    }
    protected override bool CanExecute(object parameter)
    {
        return parameter is double;
    }
   
    protected override void Execute(object parameter)
    {
        if (parameter is double val)
        {
            _playerSettings.PlaybackRate = val;
            _mediaPlayer.PlaybackSession.PlaybackRate = _playerSettings.PlaybackRate;
        }
    }
}

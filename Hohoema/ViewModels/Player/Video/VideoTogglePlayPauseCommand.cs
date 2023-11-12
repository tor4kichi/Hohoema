#nullable enable
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using System;
using Windows.Media.Playback;

namespace Hohoema.ViewModels.Player.Video;

public sealed class VideoTogglePlayPauseCommand : CommandBase
{
    private readonly HohoemaPlaylistPlayer _playlistPlayer;
    private readonly MediaPlayer _mediaPlayer;
    private readonly RestoreNavigationManager _restoreNavigationManager;

    public VideoTogglePlayPauseCommand(
        HohoemaPlaylistPlayer playlistPlayer, 
        MediaPlayer mediaPlayer, 
        RestoreNavigationManager restoreNavigationManager)
    {
        _playlistPlayer = playlistPlayer;
        _mediaPlayer = mediaPlayer;
        _restoreNavigationManager = restoreNavigationManager;
    }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override async void Execute(object parameter)
    {
        if (_playlistPlayer.CurrentPlaylistItem == null) { return; }
        
        if (_mediaPlayer.PlaybackSession is not { } session)
        {
            await _playlistPlayer.ReopenAsync();
        }
        else if (session.PlaybackState == MediaPlaybackState.Playing)
        {
            _mediaPlayer.Pause();

            var currentItem = _playlistPlayer.CurrentPlaylistItem;
            _restoreNavigationManager.SetCurrentPlayerEntry(
                currentItem.VideoId,
                session.Position,
                _playlistPlayer.CurrentPlaylistId?.Origin,
                _playlistPlayer.CurrentPlaylistId?.Id
                );
        }
        else if (session.PlaybackState == MediaPlaybackState.Paused)
        {
            //TimeSpan prevPosition = TimeSpan.Zero;
            //if (_restoreNavigationManager.GetCurrentPlayerEntry() is { } state
            //    && state.ContentId == _playlistPlayer.CurrentPlaylistItem?.VideoId)
            //{
            //    prevPosition = state.Position;
            //}

            if (session.NaturalDuration - session.Position < TimeSpan.FromSeconds(1))
            {
                session.Position = TimeSpan.Zero;
            }

            _mediaPlayer.Play();
        }
        else if (_mediaPlayer.Source == null
            || session.PlaybackState == MediaPlaybackState.None
            )
        {
            TimeSpan prevPosition;
            if (session.NaturalDuration != TimeSpan.Zero
                && session.NaturalDuration - prevPosition < TimeSpan.FromSeconds(1))
            {
                prevPosition = TimeSpan.Zero;
            }
            else if (_restoreNavigationManager.GetCurrentPlayerEntry() is { } state
                && state?.ContentId == _playlistPlayer.CurrentPlaylistItem.VideoId)
            {
                prevPosition = state.Position;
            }

            await _playlistPlayer.ReopenAsync(prevPosition);
        }        
    }
}

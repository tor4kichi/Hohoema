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
        var session = _mediaPlayer.PlaybackSession;
        if (session == null && _playlistPlayer.CurrentPlaylistItem != null)
        {
            await _playlistPlayer.ReopenAsync();
        }
        else if (_mediaPlayer.Source == null
            || session.PlaybackState == MediaPlaybackState.None)
        {

            if (_playlistPlayer.CurrentPlaylistItem == null) { return; }

            var state = _restoreNavigationManager.GetCurrentPlayerEntry();
            TimeSpan prevPosition = TimeSpan.Zero;
            if (state.ContentId == _playlistPlayer.CurrentPlaylistItem.VideoId)
            {
                prevPosition = state.Position;
            }

            var isEndReached = session.NaturalDuration - prevPosition < TimeSpan.FromSeconds(1);
            if (session?.NaturalDuration != TimeSpan.Zero
                && isEndReached)
            {
                prevPosition = TimeSpan.Zero;
            }

            await _playlistPlayer.ReopenAsync(prevPosition);
        }
        else if (session.PlaybackState == MediaPlaybackState.Playing)
        {
            _mediaPlayer.Pause();
        }
        else if (session.PlaybackState == MediaPlaybackState.Paused)
        {
            var state = _restoreNavigationManager.GetCurrentPlayerEntry();
            TimeSpan prevPosition = TimeSpan.Zero;
            if (state != null && state.ContentId == _playlistPlayer.CurrentPlaylistItem?.VideoId)
            {
                prevPosition = state.Position;
            }

            var isEndReached = session.NaturalDuration - prevPosition < TimeSpan.FromSeconds(1);
            if (isEndReached)
            {
                session.Position = TimeSpan.Zero;
            }

            _mediaPlayer.Play();
        }
    }
}

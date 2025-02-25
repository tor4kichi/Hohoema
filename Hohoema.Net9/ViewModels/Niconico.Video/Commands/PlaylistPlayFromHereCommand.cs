#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Playlist;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed partial class PlaylistPlayFromHereCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public PlaylistPlayFromHereCommand(IMessenger messenger)
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is IPlaylistItemPlayable playable && playable.PlaylistItemToken is not null and var token && token.Playlist is ISortablePlaylist;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is IPlaylistItemPlayable playable && playable.PlaylistItemToken is not null and var token)
        {
            if (token.Playlist is ISortablePlaylist)
            {
                _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(token));
            }
        }
    }
}

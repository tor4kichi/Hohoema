using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.Playlist;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class PlaylistPlayAllCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public PlaylistPlayAllCommand(IMessenger messenger)
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        if (parameter is IUserManagedPlaylist userManagedPlaylist)
        {
            return userManagedPlaylist.TotalCount > 0;
        }

        if (parameter is PlaylistToken playlistToken)
        {
            return true;
        }

        if (parameter is ISeries)
        {
            return true;
        }

        return parameter is IPlaylist;
    }

    protected override void Execute(object parameter)
    {
        if (parameter is IPlaylist playlist)
        {
            _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(playlist));
        }
        else if (parameter is PlaylistToken playlistToken)
        {
            _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(playlistToken));
        }
        else if (parameter is ISeries series)
        {
            _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(series.Id, PlaylistItemsSourceOrigin.Series, null));
        }
    }
}

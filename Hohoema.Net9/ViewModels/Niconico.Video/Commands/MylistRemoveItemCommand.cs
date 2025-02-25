#nullable enable
using Hohoema.Models.Niconico.Mylist.LoginUser;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed class MylistRemoveItemCommand : VideoContentSelectionCommandBase
{
    private readonly LoginUserMylistPlaylist _playlist;

    public MylistRemoveItemCommand(LoginUserMylistPlaylist playlist)
    {
        _playlist = playlist;
    }

    protected override void Execute(IVideoContent content)
    {
        if (content is IPlaylistItemPlayable playlistItemPlayable && playlistItemPlayable.PlaylistItemToken != null)
        {
            _playlist.RemoveItem(playlistItemPlayable.PlaylistItemToken);
        }
    }
}

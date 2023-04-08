using Hohoema.Models.Niconico.User;
using Hohoema.Models.Playlist;
using Hohoema.Models.User;
using NiconicoToolkit.User;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

public sealed class UserVideoPlaylistFactory : IPlaylistFactory
{
    private readonly UserProvider _userProvider;

    public UserVideoPlaylistFactory(UserProvider userProvider)
    {
        _userProvider = userProvider;
    }

    public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
    {
        UserId userId = playlistId.Id;
        var info = await _userProvider.GetUserInfoAsync(userId);
        return new UserVideoPlaylist(userId, playlistId, info.ScreenName, _userProvider);
    }

    public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
    {
        return UserVideoPlaylistSortOption.Deserialize(serializedSortOptions);
    }
}

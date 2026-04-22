#nullable enable
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Playlist;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

public sealed class ChannelVideoPlaylistFactory : IPlaylistFactory
{
    private readonly ChannelProvider _channelProvider;

    public ChannelVideoPlaylistFactory(ChannelProvider channelProvider)
    {
        _channelProvider = channelProvider;
    }

    public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
    {
        var name = await _channelProvider.GetChannelNameWithCacheAsync(playlistId.Id);
        return new ChannelVideoPlaylist(playlistId.Id, playlistId, name, _channelProvider);
    }

    public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
    {
        return ChannelVideoPlaylistSortOption.Deserialize(serializedSortOptions);
    }
}

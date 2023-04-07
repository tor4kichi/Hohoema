using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Playlist;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory
{
    public sealed class ChannelVideoPlaylistFactory : IPlaylistFactory
    {
        private readonly ChannelProvider _channelProvider;

        public ChannelVideoPlaylistFactory(ChannelProvider channelProvider)
        {
            _channelProvider = channelProvider;
        }

        public async ValueTask<IPlaylist> Create(PlaylistId playlistId)
        {
            var info = await _channelProvider.GetChannelInfo(playlistId.Id);
            return new ChannelVideoPlaylist(info.ChannelId, playlistId, info.Name, _channelProvider);
        }

        public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
        {
            return ChannelVideoPlaylistSortOption.Deserialize(serializedSortOptions);
        }
    }
}

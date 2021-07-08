using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist.PlaylistFactory;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist
{

    public sealed class PlaylistItemsSourceResolver : IPlaylistFactoryResolver
    {
        private readonly Lazy<LocalMylistPlaylistFactory> _localMylistPlaylistFactory;
        private readonly Lazy<MylistPlaylistFactory> _mylistPlaylistFactory;
        private readonly Lazy<SeriesVideoPlaylistFactory> _seriesVideoPlaylistFactory;
        private readonly Lazy<ChannelVideoPlaylistFactory> _channelVideoPlaylistFactory;
        private readonly Lazy<UserVideoPlaylistFactory> _userVideoPlaylistFactory;
        private readonly Lazy<CommunityVideoPlaylistFactory> _communityVideoPlaylistFactory;

        public PlaylistItemsSourceResolver(
            Lazy<LocalMylistPlaylistFactory> localMylistPlaylistFactory,
            Lazy<MylistPlaylistFactory> mylistPlaylistFactory,
            Lazy<SeriesVideoPlaylistFactory> seriesVideoPlaylistFactory,
            Lazy<ChannelVideoPlaylistFactory> channelVideoPlaylistFactory,
            Lazy<UserVideoPlaylistFactory> userVideoPlaylistFactory,
            Lazy<CommunityVideoPlaylistFactory> communityVideoPlaylistFactory
            )
        {
            _localMylistPlaylistFactory = localMylistPlaylistFactory;
            _mylistPlaylistFactory = mylistPlaylistFactory;
            _seriesVideoPlaylistFactory = seriesVideoPlaylistFactory;
            _channelVideoPlaylistFactory = channelVideoPlaylistFactory;
            _userVideoPlaylistFactory = userVideoPlaylistFactory;
            _communityVideoPlaylistFactory = communityVideoPlaylistFactory;
        }

        public IPlaylistFactory Resolve(PlaylistItemsSourceOrigin origin)
        {
            return origin switch
            {
                PlaylistItemsSourceOrigin.Local => _localMylistPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.Mylist => _mylistPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.Series => _seriesVideoPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.ChannelVideos => _channelVideoPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.UserVideos => _userVideoPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.CommunityVideos => _communityVideoPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.SearchWithKeyword => null,
                PlaylistItemsSourceOrigin.SearchWithTag => null,
                _ => throw new NotSupportedException(origin.ToString()),
            };
        }
    }

}

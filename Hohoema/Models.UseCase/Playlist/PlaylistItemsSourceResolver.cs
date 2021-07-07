using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist.PlaylistItemsSource;
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

        public PlaylistItemsSourceResolver(
            Lazy<LocalMylistPlaylistFactory> localMylistPlaylistFactory,
            Lazy<MylistPlaylistFactory> mylistPlaylistFactory,
            Lazy<SeriesVideoPlaylistFactory> seriesVideoPlaylistFactory
            )
        {
            _localMylistPlaylistFactory = localMylistPlaylistFactory;
            _mylistPlaylistFactory = mylistPlaylistFactory;
            _seriesVideoPlaylistFactory = seriesVideoPlaylistFactory;
        }

        public IPlaylistFactory Resolve(PlaylistItemsSourceOrigin origin)
        {
            return origin switch
            {
                PlaylistItemsSourceOrigin.Local => _localMylistPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.Mylist => _mylistPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.ChannelVideos => null,
                PlaylistItemsSourceOrigin.UserVideos => null,
                PlaylistItemsSourceOrigin.Series => _seriesVideoPlaylistFactory.Value,
                PlaylistItemsSourceOrigin.CommunityVideos => null,
                PlaylistItemsSourceOrigin.SearchWithKeyword => null,
                PlaylistItemsSourceOrigin.SearchWithTag => null,
                _ => throw new NotSupportedException(origin.ToString()),
            };
        }
    }

}

using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist.PlaylistItemsSource;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist
{

    public sealed class PlaylistItemsSourceResolver : IPlaylistItemsSourceFactoryResolver
    {
        private readonly Lazy<LocalMylistPlaylistItemsSourceFactory> _localMylistPlaylistItemsSourceFactory;
        private readonly Lazy<MylistPlaylistItemsSourceFactory> _mylistPlaylistItemsSourceFactory;

        public PlaylistItemsSourceResolver(
            Lazy<LocalMylistPlaylistItemsSourceFactory> localMylistPlaylistItemsSourceFactory,
            Lazy<MylistPlaylistItemsSourceFactory> mylistPlaylistItemsSourceFactory
            )
        {
            _localMylistPlaylistItemsSourceFactory = localMylistPlaylistItemsSourceFactory;
            _mylistPlaylistItemsSourceFactory = mylistPlaylistItemsSourceFactory;
        }

        public IPlaylistItemsSourceFactory Resolve(PlaylistItemsSourceOrigin origin)
        {
            return origin switch
            {
                PlaylistItemsSourceOrigin.Local => _localMylistPlaylistItemsSourceFactory.Value,
                PlaylistItemsSourceOrigin.Mylist => _mylistPlaylistItemsSourceFactory.Value,
                PlaylistItemsSourceOrigin.ChannelVideos => null,
                PlaylistItemsSourceOrigin.UserVideos => null,
                PlaylistItemsSourceOrigin.Series => null,
                PlaylistItemsSourceOrigin.CommunityVideos => null,
                PlaylistItemsSourceOrigin.SearchWithKeyword => null,
                PlaylistItemsSourceOrigin.SearchWithTag => null,
                _ => throw new NotSupportedException(origin.ToString()),
            };
        }
    }

}

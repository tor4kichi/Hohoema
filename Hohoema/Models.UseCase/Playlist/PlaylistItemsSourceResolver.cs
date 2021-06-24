using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist.PlaylistItemsSource;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist
{
    public interface IPlaylistItemsSourceFactory
    {
        ValueTask<IPlaylistItemsSource> Create(PlaylistId playlistId);
    }


    public sealed class PlaylistItemsSourceResolver : IPlaylistItemsSourceResolver
    {
        private readonly Lazy<LocalMylistPlaylistItemsSourceFactory> _localMylistPlaylistItemsSourceFactory;

        public PlaylistItemsSourceResolver(Lazy<LocalMylistPlaylistItemsSourceFactory> localMylistPlaylistItemsSourceFactory)
        {
            _localMylistPlaylistItemsSourceFactory = localMylistPlaylistItemsSourceFactory;
        }

        public ValueTask<IPlaylistItemsSource> ResolveItemsSource(PlaylistId playlistId)
        {
            IPlaylistItemsSourceFactory factory = playlistId.Origin switch
            {
                PlaylistItemsSourceOrigin.Local => _localMylistPlaylistItemsSourceFactory.Value,
                PlaylistItemsSourceOrigin.Mylist => null,
                PlaylistItemsSourceOrigin.ChannelVideos => null,
                PlaylistItemsSourceOrigin.UserVideos => null,
                PlaylistItemsSourceOrigin.Series => null,
                PlaylistItemsSourceOrigin.CommunityVideos => null,
                PlaylistItemsSourceOrigin.SearchWithKeyword => null,
                PlaylistItemsSourceOrigin.SearchWithTag => null,
                _ => throw new NotSupportedException(playlistId.Origin.ToString()),
            };

            if (factory == null)
            {
                throw new InvalidOperationException();
            }

            return factory.Create(playlistId);
        }
    }

}

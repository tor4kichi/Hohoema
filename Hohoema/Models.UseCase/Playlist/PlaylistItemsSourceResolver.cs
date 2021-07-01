using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase.Playlist.PlaylistItemsSource;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist
{
    public interface IPlaylistItemsSourceFactory
    {
        ValueTask<IPlaylist> Create(PlaylistId playlistId);
        IPlaylistSortOptions DeserializeSortOptions(string serializedSortOptions);
    }


    public sealed class PlaylistItemsSourceResolver : IPlaylistItemsSourceResolver
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

        public async ValueTask<IPlaylist> ResolveItemsSource(PlaylistId playlistId, string serializedSortOptions)
        {
            IPlaylistItemsSourceFactory factory = playlistId.Origin switch
            {
                PlaylistItemsSourceOrigin.Local => _localMylistPlaylistItemsSourceFactory.Value,
                PlaylistItemsSourceOrigin.Mylist => _mylistPlaylistItemsSourceFactory.Value,
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

            var playlist = await factory.Create(playlistId);
            playlist.SortOptions = factory.DeserializeSortOptions(serializedSortOptions);
            return playlist;
        }
    }

}

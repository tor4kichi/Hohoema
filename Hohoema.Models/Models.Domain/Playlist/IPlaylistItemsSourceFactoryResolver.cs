using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{
    public interface IPlaylistItemsSourceFactoryResolver
    {
        IPlaylistItemsSourceFactory Resolve(PlaylistItemsSourceOrigin origin);
    }

    public interface IPlaylistItemsSourceFactory
    {
        ValueTask<IPlaylist> Create(PlaylistId playlistId);
        IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions);
    }
}

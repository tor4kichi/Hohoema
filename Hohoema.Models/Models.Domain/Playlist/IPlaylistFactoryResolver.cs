using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{
    public interface IPlaylistFactoryResolver
    {
        IPlaylistFactory Resolve(PlaylistItemsSourceOrigin origin);
    }

    public interface IPlaylistFactory
    {
        ValueTask<IPlaylist> Create(PlaylistId playlistId);
        IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions);
    }
}

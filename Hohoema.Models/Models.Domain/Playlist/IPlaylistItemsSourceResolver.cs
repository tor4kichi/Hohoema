using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Models.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
{
    public interface IPlaylistItemsSourceResolver
    {
        ValueTask<IPlaylistItemsSource> ResolveItemsSource(PlaylistId id);
    }
}

#nullable enable
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.Playlist;
using System.Threading.Tasks;

namespace Hohoema.Services.Playlist.PlaylistFactory;

public sealed class SearchPlaylistFactory : IPlaylistFactory
{
    private readonly SearchProvider _searchProvider;

    public SearchPlaylistFactory(SearchProvider searchProvider)
    {
        _searchProvider = searchProvider;
    }

    public ValueTask<IPlaylist> Create(PlaylistId playlistId)
    {
        return new (new CeApiSearchVideoPlaylist(playlistId, _searchProvider));
    }

    public IPlaylistSortOption DeserializeSortOptions(string serializedSortOptions)
    {
        return CeApiSearchVideoPlaylistSortOption.Deserialize(serializedSortOptions);
    }
}

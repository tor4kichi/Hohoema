using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using I18NPortable;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Playlist.PlaylistFactory
{
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


}

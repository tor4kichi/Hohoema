using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.SearchWithCeApi.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Search
{

    public sealed class CeApiSearchVideoPlaylist : IUnlimitedPlaylist
    {
        private readonly SearchProvider _searchProvider;

        public static CeApiSearchVideoPlaylistSortOption[] SortOptions { get; } = new[]
        {
            VideoSortKey.FirstRetrieve,
            VideoSortKey.ViewCount,
            VideoSortKey.NewComment,
            VideoSortKey.MylistCount,
            VideoSortKey.CommentCount,
            VideoSortKey.Length,
        }
        .SelectMany(x => new CeApiSearchVideoPlaylistSortOption[] { new (x, VideoSortOrder.Desc), new (x, VideoSortOrder.Asc)})
            .ToArray();

        public static CeApiSearchVideoPlaylistSortOption DefaultSortOption => SortOptions[0];

        public CeApiSearchVideoPlaylist(PlaylistId playlistId, SearchProvider searchProvider)
        {
            PlaylistId = playlistId;
            _searchProvider = searchProvider;
        }
        public string Name => PlaylistId.Id;

        public PlaylistId PlaylistId { get; }

        IPlaylistSortOption[] IPlaylist.SortOptions => SortOptions;

        IPlaylistSortOption IPlaylist.DefaultSortOption => DefaultSortOption;


        public int OneTimeLoadItemsCount => 50;

        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, IPlaylistSortOption sortOption, CancellationToken cancellationToken = default)
        {
            var sort = sortOption as CeApiSearchVideoPlaylistSortOption;
            var head = pageIndex * pageSize;

            var result = PlaylistId.Origin switch
            {
                PlaylistItemsSourceOrigin.SearchWithKeyword => await _searchProvider.GetKeywordSearch(PlaylistId.Id, head, pageSize, sort.SortKey, sort.SortOrder),
                PlaylistItemsSourceOrigin.SearchWithTag => await _searchProvider.GetTagSearch(PlaylistId.Id, head, pageSize, sort.SortKey, sort.SortOrder),
                _ => throw new NotSupportedException(),
            };

            Guard.IsTrue(result.IsOK, nameof(result.IsOK));

            return result.Videos.Select(x => new CeApiSearchVideoContent(x.Video));
        }
    }
}

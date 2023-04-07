using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Hohoema.Models.Niconico.Search;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.SearchWithPage.Video;

namespace Hohoema.ViewModels.Niconico.Search
{
    public class VideoSearchIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
	{
        public VideoSearchIncrementalSource(CeApiSearchVideoPlaylist searchVideoPlaylist, CeApiSearchVideoPlaylistSortOption sortOption, SearchProvider searchProvider)
        {
            _searchVideoPlaylist = searchVideoPlaylist;
            _sortOption = sortOption;
            SearchProvider = searchProvider;
        }

        // Note: 50以上じゃないと２回目の取得が失敗する
        public const int OneTimeLoadingCount = 50;
        private readonly CeApiSearchVideoPlaylist _searchVideoPlaylist;
        private readonly CeApiSearchVideoPlaylistSortOption _sortOption;

        public string Keyword => _searchVideoPlaylist.PlaylistId.Id;

        public bool IsTagSearch => _searchVideoPlaylist.PlaylistId.Origin == Models.Playlist.PlaylistItemsSourceOrigin.SearchWithTag;

		public SearchProvider SearchProvider { get; }

        async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            VideoListingResponse res = null;
            var head = pageIndex * pageSize;
            if (!IsTagSearch)
            {
                res = await SearchProvider.GetKeywordSearch(Keyword, head, pageSize, _sortOption.SortKey, _sortOption.SortOrder);
            }
            else
            {
                res = await SearchProvider.GetTagSearch(Keyword, head, pageSize, _sortOption.SortKey, _sortOption.SortOrder);
            }

            ct.ThrowIfCancellationRequested();

            if (res == null || res.Videos == null)
            {
                return Enumerable.Empty<VideoListItemControlViewModel>();
            }

            return res.Videos
                .Select((x, i) => new VideoListItemControlViewModel(x.Video, x.Thread) { PlaylistItemToken = new Models.Playlist.PlaylistItemToken(_searchVideoPlaylist, _sortOption, new CeApiSearchVideoContent(x.Video))})
                .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                ;
        }
    }
}

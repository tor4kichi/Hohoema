using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.SearchWithPage.Video;

namespace Hohoema.Presentation.ViewModels.Niconico.Search
{
    public class VideoSearchIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
	{
        public VideoSearchIncrementalSource(string keyword, bool isTagSearch, VideoSortKey sortKey, VideoSortOrder sortOrder, SearchProvider searchProvider)
        {
            Keyword = keyword;
            IsTagSearch = isTagSearch;
			SortKey = sortKey;
			SortOrder = sortOrder;
            SearchProvider = searchProvider;
        }

        // Note: 50以上じゃないと２回目の取得が失敗する
        public const int OneTimeLoadingCount = 50;  

        public string Keyword { get; }

		public bool IsTagSearch { get;  }

		public VideoSortKey SortKey { get;  }
		public VideoSortOrder SortOrder { get; }

		public SearchProvider SearchProvider { get; }

        async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            VideoListingResponse res = null;
            var head = pageIndex * pageSize;
            if (!IsTagSearch)
            {
                res = await SearchProvider.GetKeywordSearch(Keyword, (uint)head, (uint)pageSize, SortKey, SortOrder);
            }
            else
            {
                res = await SearchProvider.GetTagSearch(Keyword, (uint)head, (uint)pageSize, SortKey, SortOrder);
            }

            ct.ThrowIfCancellationRequested();

            if (res == null || res.Videos == null)
            {
                return Enumerable.Empty<VideoListItemControlViewModel>();
            }

            return res.Videos.Select(x => new VideoListItemControlViewModel(x.Video, x.Thread));
        }
    }
}

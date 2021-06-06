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
    public class VideoSearchSource : HohoemaIncrementalSourceBase<VideoListItemControlViewModel>
	{
        public VideoSearchSource(string keyword, bool isTagSearch, VideoSortKey sortKey, VideoSortOrder sortOrder, SearchProvider searchProvider)
        {
            Keyword = keyword;
            IsTagSearch = isTagSearch;
			SortKey = sortKey;
			SortOrder = sortOrder;
            SearchProvider = searchProvider;
        }

        public override uint OneTimeLoadCount => 30;

        public string Keyword { get; }

		public bool IsTagSearch { get;  }

		public VideoSortKey SortKey { get;  }
		public VideoSortOrder SortOrder { get; }

		public SearchProvider SearchProvider { get; }

        protected override async IAsyncEnumerable<VideoListItemControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation]CancellationToken ct)
        {
            VideoListingResponse res = null;
            if (!IsTagSearch)
            {
                res = await SearchProvider.GetKeywordSearch(Keyword, (uint)head, (uint)count, SortKey, SortOrder);
            }
            else
            {
                res = await SearchProvider.GetTagSearch(Keyword, (uint)head, (uint)count, SortKey, SortOrder);
            }

            ct.ThrowIfCancellationRequested();

            if (res == null || res.Videos == null)
            {

            }
            else
            {
                foreach (var item in res.Videos.Where(x => x != null))
                {
                    var vm = new VideoListItemControlViewModel(item.Video, item.Thread);

                    yield return vm;

                    ct.ThrowIfCancellationRequested();
                }
            }
        }

        protected override ValueTask<int> ResetSourceImpl()
        {
            return new ValueTask<int>(1);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Mntone.Nico2;
using Mntone.Nico2.Searches.Video;
using System.Runtime.CompilerServices;
using System.Threading;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Presentation.ViewModels.VideoListPage;

namespace Hohoema.Presentation.ViewModels.Niconico.Search
{
    public class VideoSearchSource : HohoemaIncrementalSourceBase<VideoInfoControlViewModel>
	{
        public VideoSearchSource(string keyword, bool isTagSearch, Sort sort, Order order, SearchProvider searchProvider)
        {
            Keyword = keyword;
            IsTagSearch = isTagSearch;
			SearchSort = sort;
			SearchOrder = order;
            SearchProvider = searchProvider;
        }

		public string Keyword { get; }

		public bool IsTagSearch { get;  }

		public Sort SearchSort { get;  }
		public Order SearchOrder { get; }

		public SearchProvider SearchProvider { get; }
		

		

        protected override async IAsyncEnumerable<VideoInfoControlViewModel> GetPagedItemsImpl(int head, int count, [EnumeratorCancellation] CancellationToken ct = default)
        {
            VideoListingResponse res = null;
            if (!IsTagSearch)
            {
                res = await SearchProvider.GetKeywordSearch(Keyword, (uint)head, (uint)count, SearchSort, SearchOrder);
            }
            else 
            {
                res = await SearchProvider.GetTagSearch(Keyword, (uint)head, (uint)count, SearchSort, SearchOrder);
            }

			ct.ThrowIfCancellationRequested();

            if (res == null || res.VideoInfoItems == null)
            {
                
            }
            else
            {
                foreach (var item in res.VideoInfoItems.Where(x => x != null))
                {
                    var vm = new VideoInfoControlViewModel(item.Video.Id, item.Video.Title, item.Video.ThumbnailUrl.OriginalString, item.Video.Length);

                    vm.Setup(item);

					await vm.InitializeAsync(ct).ConfigureAwait(false);

					yield return vm;

					ct.ThrowIfCancellationRequested();
                }
            }
        }

        protected override async ValueTask<int> ResetSourceImpl()
        {
            int totalCount = 0;
            if (!IsTagSearch)
            {
                var res = await SearchProvider.GetKeywordSearch(Keyword, 0, 2, SearchSort, SearchOrder);
                totalCount = (int)res.GetTotalCount();

            }
            else 
            {
                var res = await SearchProvider.GetTagSearch(Keyword, 0, 2, SearchSort, SearchOrder);
                totalCount = (int)res.GetTotalCount();
            }

            return totalCount;
        }
    }
}

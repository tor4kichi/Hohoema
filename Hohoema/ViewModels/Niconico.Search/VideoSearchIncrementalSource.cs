#nullable enable
using CommunityToolkit.Diagnostics;
using Hohoema.Models.Niconico.Search;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Search;
using NiconicoToolkit.Search.Video;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Search;

public class VideoSearchIncrementalSource : IIncrementalSource<VideoListItemControlViewModel>
	{
    public VideoSearchIncrementalSource(
        SearchClient searchClient,
        string keyword, 
        bool isTagSearch,
        SortKey sortKey,
        SortOrder sortOrder
        )
    {
        _searchClient = searchClient;
        Keyword = keyword;
        IsTagSearch = isTagSearch;
        SortKey = sortKey;
        SortOrder = sortOrder;
    }

    // Note: 50以上じゃないと２回目の取得が失敗する
    public const int OneTimeLoadingCount = 50;
    private readonly SearchClient _searchClient;

    public string Keyword { get; }
    public bool IsTagSearch { get; }
    public SortKey SortKey { get; }
    public SortOrder SortOrder { get; }

    async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
    {
        var res = await _searchClient.Video.VideoSearchAsync(
            keyword: Keyword,
            isTagSearch: IsTagSearch,
            sortKey: SortKey,
            sortOrder: SortOrder,
            pageCountStartWith1: pageIndex + 1
            );
        Guard.IsTrue(res.IsSuccess, nameof(res.IsSuccess));        
        return res.Data.Items.Select(x => new VideoListItemControlViewModel(x)).ToArray();        
    }
}

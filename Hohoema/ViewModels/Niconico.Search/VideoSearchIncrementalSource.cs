#nullable enable
using CommunityToolkit.Diagnostics;
using Hohoema.Models.Niconico.Search;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using NiconicoToolkit.Search;
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
        bool isTagSearch
        )
    {
        _searchClient = searchClient;
        Keyword = keyword;
        IsTagSearch = isTagSearch;
    }

    // Note: 50以上じゃないと２回目の取得が失敗する
    public const int OneTimeLoadingCount = 50;
    private readonly SearchClient _searchClient;

    public string Keyword { get; }
    public bool IsTagSearch { get; }

    async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
    {
        var res = await _searchClient.Video.VideoSearchAsync(
            keyword: Keyword,
            isTagSearch: IsTagSearch,
            sortKey: NiconicoToolkit.Search.Video.SortKey.RegisteredAt,
            pageCount: pageIndex
            );
        Guard.IsTrue(res.IsSuccess, nameof(res.IsSuccess));
        return res.Data.Items.Select(x => new VideoListItemControlViewModel(x)).ToArray();        

        //VideoListingResponse res = null;
        //var head = pageIndex * pageSize;
        //if (!IsTagSearch)
        //{
        //    res = await SearchProvider.GetKeywordSearch(Keyword, head, pageSize, _sortOption.SortKey, _sortOption.SortOrder);
        //}
        //else
        //{
        //    res = await SearchProvider.GetTagSearch(Keyword, head, pageSize, _sortOption.SortKey, _sortOption.SortOrder);
        //}

        //ct.ThrowIfCancellationRequested();

        //if (res == null || res.Videos == null)
        //{
        //    return Enumerable.Empty<VideoListItemControlViewModel>();
        //}

        //return res.Videos
        //    .Select((x, i) => new VideoListItemControlViewModel(x.Video, x.Thread) { PlaylistItemToken = new Models.Playlist.PlaylistItemToken(_searchVideoPlaylist, _sortOption, new CeApiSearchVideoContent(x.Video))})
        //    .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
        //    ;
    }
}

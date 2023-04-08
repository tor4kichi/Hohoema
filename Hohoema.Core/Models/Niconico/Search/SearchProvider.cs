#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Niconico.Mylist;
using NiconicoToolkit.SearchWithCeApi.Video;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Search;

public sealed class SearchProvider : ProviderBase
{
    private readonly MylistProvider _mylistProvider;

    // TODO: タグによる生放送検索を別メソッドに分ける

    public SearchProvider(
        NiconicoSession niconicoSession,
        MylistProvider mylistProvider
        )
        : base(niconicoSession)
    {
        _mylistProvider = mylistProvider;
    }



    public Task<NiconicoToolkit.SearchWithCeApi.Video.VideoListingResponse> GetKeywordSearch(string keyword, int from, int limit, VideoSortKey sort = VideoSortKey.FirstRetrieve, VideoSortOrder order = VideoSortOrder.Desc)
    {
        return _niconicoSession.ToolkitContext.SearchWithCeApi.Video.KeywordSearchAsync(keyword, from, limit, sort, order);
    }

    public Task<NiconicoToolkit.SearchWithCeApi.Video.VideoListingResponse> GetTagSearch(string tag, int from, int limit, VideoSortKey sort = VideoSortKey.FirstRetrieve, VideoSortOrder order = VideoSortOrder.Desc)
    {
        return _niconicoSession.ToolkitContext.SearchWithCeApi.Video.TagSearchAsync(tag, from, limit, sort, order);
    }

    public Task<NiconicoToolkit.SearchWithPage.Live.LiveSearchPageScrapingResult> LiveSearchAsync(
        NiconicoToolkit.SearchWithPage.Live.LiveSearchOptionsQuery query
        )
    {
        return _niconicoSession.ToolkitContext.SearchWithPage.Live.GetLiveSearchPageScrapingResultAsync(query, CancellationToken.None);
    }





}

#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Niconico.Mylist;
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

    public Task<NiconicoToolkit.SearchWithPage.Live.LiveSearchPageScrapingResult> LiveSearchAsync(
        NiconicoToolkit.SearchWithPage.Live.LiveSearchOptionsQuery query
        )
    {
        return _niconicoSession.ToolkitContext.SearchWithPage.Live.GetLiveSearchPageScrapingResultAsync(query, CancellationToken.None);
    }
}

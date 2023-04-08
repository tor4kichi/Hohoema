using Hohoema.Infra;
using NiconicoToolkit.Series;
using NiconicoToolkit.User;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video.Series;

public sealed class SeriesProvider : ProviderBase
{
    public SeriesProvider(NiconicoSession niconicoSession)
        : base(niconicoSession)
    {
    }

    public async Task<IList<SeriesItem>> GetUserSeriesAsync(UserId userId)
    {
        List<SeriesItem> items = new();
        SeriesListResponse res = await _niconicoSession.ToolkitContext.Series.GetUserSeriesAsync(userId, 0);
        items.AddRange(res.Data.Items);
        int page = 1;
        while (items.Count < res.Data.TotalCount)
        {
            res = await _niconicoSession.ToolkitContext.Series.GetUserSeriesAsync(userId, page);
            items.AddRange(res.Data.Items);
        }

        return items;
    }

    public async Task<NvapiSeriesVidoesResponseContainer> GetSeriesVideosAsync(string seriesId, int page = 0, int pageSize = 100)
    {
        return await _niconicoSession.ToolkitContext.Series.GetUserSeriesVideosAsync(seriesId, page, pageSize);
    }
}

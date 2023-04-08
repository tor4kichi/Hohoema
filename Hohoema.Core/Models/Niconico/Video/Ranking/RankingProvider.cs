#nullable enable
using Hohoema.Infra;
using NiconicoToolkit.Ranking.Video;
using NiconicoToolkit.Rss.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video.Ranking;

public sealed class RankingProvider : ProviderBase
{
    private readonly RankingGenreCache _rankingGenreCache;

    public RankingProvider(
        NiconicoSession niconicoSession,
        RankingGenreCache rankingGenreCache
        )
        : base(niconicoSession)
    {
        _rankingGenreCache = rankingGenreCache;
    }


    public List<RankingGenreTag> GetRankingGenreTagsFromCache(RankingGenre genre)
    {
        RankingTagsGenreGroupedEntry cachedTags = _rankingGenreCache.Get(genre);
        return cachedTags != null
            ? cachedTags.Tags.Select(x => new RankingGenreTag() { Label = x.DisplayName, Genre = genre, Tag = x.Tag }).ToList()
            : new List<RankingGenreTag>();
    }

    public async ValueTask<List<RankingGenreTag>> GetRankingGenreTagsAsync(RankingGenre genre, bool isForceUpdate = false, CancellationToken ct = default)
    {
        if (isForceUpdate)
        {
            _ = _rankingGenreCache.Delete(genre);
        }
        else
        {
            RankingTagsGenreGroupedEntry cachedTags = _rankingGenreCache.Get(genre);
            if (cachedTags != null && (DateTime.Now - cachedTags.UpdateAt) < TimeSpan.FromHours(12))
            {
                return cachedTags.Tags.Select(x => new RankingGenreTag() { Label = x.DisplayName, Genre = genre, Tag = x.Tag }).ToList();
            }
        }

        List<RankingGenrePickedTag> tagsRaw = await _niconicoSession.ToolkitContext.Video.Ranking.GetGenrePickedTagAsync(genre);
        List<RankingGenreTag> tags = tagsRaw.Select(x => new RankingGenreTag() { Label = x.DisplayName, Tag = x.Tag, Genre = genre }).ToList();
        _ = _rankingGenreCache.Upsert(genre, tags.Select(x => new RankingGenreTagEntry() { DisplayName = x.Label, Tag = x.Tag }));

        return tags;
    }

    public Task<RssVideoResponse> GetRankingGenreWithTagAsync(RankingGenre genre, string tag, RankingTerm term, int page = 1)
    {
        return _niconicoSession.ToolkitContext.Video.Ranking.GetRankingRssAsync(genre, tag, term, page);
    }
}

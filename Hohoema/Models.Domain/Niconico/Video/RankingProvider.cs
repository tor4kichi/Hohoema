using Mntone.Nico2.Videos.Ranking;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video
{
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
            var cachedTags = _rankingGenreCache.Get(genre);
            if (cachedTags != null)
            {
                return cachedTags.Tags.Select(x => new RankingGenreTag() { Label = x.DisplayName, Genre = genre, Tag = x.Tag }).ToList();
            }
            else 
            {
                return new List<RankingGenreTag>();
            }
        }

        public async Task<List<RankingGenreTag>> GetRankingGenreTagsAsync(RankingGenre genre, bool isForceUpdate = false)
        {
            if (isForceUpdate)
            {
                _rankingGenreCache.Delete(genre);
            }
            else
            {
                var cachedTags = _rankingGenreCache.Get(genre);
                if (cachedTags != null && (DateTime.Now - cachedTags.UpdateAt) < TimeSpan.FromHours(6) )
                {
                    return cachedTags.Tags.Select(x => new RankingGenreTag() { Label = x.DisplayName, Genre = genre, Tag = x.Tag }).ToList();
                }
            }

            var tagsRaw = await NiconicoRanking.GetGenrePickedTagAsync(genre);
            var tags = tagsRaw.Select(x => new RankingGenreTag() { Label = x.DisplayName, Tag = x.Tag, Genre = genre }).ToList();
            _rankingGenreCache.Upsert(genre, tags.Select(x => new RankingGenreTagEntry() {DisplayName = x.Label, Tag = x.Tag } ));
            return tags;
        }

        public async Task<Mntone.Nico2.RssVideoResponse> GetRankingGenreWithTagAsync(RankingGenre genre, string tag, RankingTerm term, int page = 1)
        {
            return await NiconicoRanking.GetRankingRssAsync(genre, tag, term, page);
        }
    }
}

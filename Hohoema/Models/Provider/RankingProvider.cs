﻿using Mntone.Nico2.Videos.Ranking;
using Hohoema.Database.Local;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Provider
{
    public sealed class RankingProvider : ProviderBase
    {
        public RankingProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<List<Database.Local.RankingGenreTag>> GetRankingGenreTagsAsync(RankingGenre genre, bool isForceUpdate = false)
        {
            if (isForceUpdate)
            {
                Database.Local.RankingGenreTagsDb.Delete(genre);
            }
            else
            {
                var cachedTags = Database.Local.RankingGenreTagsDb.Get(genre);
                if (cachedTags != null && (DateTime.Now - cachedTags.UpdateAt) < TimeSpan.FromHours(6) )
                {
                    return cachedTags.Tags;
                }
            }

            var tagsRaw = await NiconicoRanking.GetGenrePickedTagAsync(genre);
            var tags = tagsRaw.Select(x => new Database.Local.RankingGenreTag() { DisplayName = x.DisplayName, Tag = x.Tag }).ToList();
            Database.Local.RankingGenreTagsDb.Upsert(genre, tags);
            return tags;
        }

        public async Task<Mntone.Nico2.RssVideoResponse> GetRankingGenreWithTagAsync(RankingGenre genre, string tag, RankingTerm term, int page = 1)
        {
            return await NiconicoRanking.GetRankingRssAsync(genre, tag, term, page);
        }
    }
}

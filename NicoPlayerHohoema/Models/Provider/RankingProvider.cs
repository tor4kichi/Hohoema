using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class RankingProvider : ProviderBase
    {
        public RankingProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<Mntone.Nico2.RssVideoResponse> GetCategoryRanking(RankingGenre genre, RankingTerm? term = null, string tag = null, int page = 1)
        {
            return await ContextActionAsync(async context => 
            {
                return await NiconicoRanking.GetRankingRssAsync(genre, term, tag, page);
            });
        }


    }
}

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

        public async Task<NiconicoVideoRss> GetCategoryRanking(RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
        {
            return await ContextActionAsync(async context => 
            {
                return await NiconicoRanking.GetRankingData(target, timeSpan, category);
            });
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideoRanking
{
    internal static class RankingTermMapper
    {
        public static RankingTerm ToModelRankingTerm(this Mntone.Nico2.Videos.Ranking.RankingTerm infraRankingTerm) => infraRankingTerm switch
        {
            Mntone.Nico2.Videos.Ranking.RankingTerm.Hour => RankingTerm.Hour,
            Mntone.Nico2.Videos.Ranking.RankingTerm.Day => RankingTerm.Day,
            Mntone.Nico2.Videos.Ranking.RankingTerm.Week => RankingTerm.Week,
            Mntone.Nico2.Videos.Ranking.RankingTerm.Month => RankingTerm.Month,
            Mntone.Nico2.Videos.Ranking.RankingTerm.Total => RankingTerm.Total,
        };

        public static Mntone.Nico2.Videos.Ranking.RankingTerm ToInfrastructureRankingTerm(this RankingTerm infraRankingTerm) => infraRankingTerm switch
        {
            RankingTerm.Hour => Mntone.Nico2.Videos.Ranking.RankingTerm.Hour,
            RankingTerm.Day => Mntone.Nico2.Videos.Ranking.RankingTerm.Day,
            RankingTerm.Week => Mntone.Nico2.Videos.Ranking.RankingTerm.Week,
            RankingTerm.Month => Mntone.Nico2.Videos.Ranking.RankingTerm.Month,
            RankingTerm.Total => Mntone.Nico2.Videos.Ranking.RankingTerm.Total,
        };
    }
}

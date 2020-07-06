using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoVideo.Ranking
{
    internal static class RankingGenreMapper
    {
        public static RankingGenre ToModelRankingGenre(this Mntone.Nico2.Videos.Ranking.RankingGenre genre) => genre switch
        {
            Mntone.Nico2.Videos.Ranking.RankingGenre.All => RankingGenre.All,
            Mntone.Nico2.Videos.Ranking.RankingGenre.HotTopic => RankingGenre.HotTopic,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Entertainment => RankingGenre.Entertainment,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Radio => RankingGenre.Radio,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Music_Sound => RankingGenre.Music_Sound,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Dance => RankingGenre.Dance,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Animal => RankingGenre.Animal,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Nature => RankingGenre.Nature,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Cooking => RankingGenre.Cooking,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Traveling_Outdoor => RankingGenre.Traveling_Outdoor,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Vehicle => RankingGenre.Vehicle,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Sports => RankingGenre.Sports,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Society_Politics_News => RankingGenre.Society_Politics_News,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Technology_Craft => RankingGenre.Technology_Craft,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Commentary_Lecture => RankingGenre.Commentary_Lecture,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Anime => RankingGenre.Anime,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Game => RankingGenre.Game,
            Mntone.Nico2.Videos.Ranking.RankingGenre.Other => RankingGenre.Other,
            Mntone.Nico2.Videos.Ranking.RankingGenre.R18 => RankingGenre.R18,
            _ => throw new NotSupportedException(genre.ToString()),
        };

        public static Mntone.Nico2.Videos.Ranking.RankingGenre ToInfrastructureRankingGenre(this RankingGenre genre) => genre switch
        {
            RankingGenre.All => Mntone.Nico2.Videos.Ranking.RankingGenre.All,
            RankingGenre.HotTopic => Mntone.Nico2.Videos.Ranking.RankingGenre.HotTopic,
            RankingGenre.Entertainment => Mntone.Nico2.Videos.Ranking.RankingGenre.Entertainment,
            RankingGenre.Radio => Mntone.Nico2.Videos.Ranking.RankingGenre.Radio,
            RankingGenre.Music_Sound => Mntone.Nico2.Videos.Ranking.RankingGenre.Music_Sound,
            RankingGenre.Dance => Mntone.Nico2.Videos.Ranking.RankingGenre.Dance,
            RankingGenre.Animal => Mntone.Nico2.Videos.Ranking.RankingGenre.Animal,
            RankingGenre.Nature => Mntone.Nico2.Videos.Ranking.RankingGenre.Nature,
            RankingGenre.Cooking => Mntone.Nico2.Videos.Ranking.RankingGenre.Cooking,
            RankingGenre.Traveling_Outdoor => Mntone.Nico2.Videos.Ranking.RankingGenre.Traveling_Outdoor,
            RankingGenre.Vehicle => Mntone.Nico2.Videos.Ranking.RankingGenre.Vehicle,
            RankingGenre.Sports => Mntone.Nico2.Videos.Ranking.RankingGenre.Sports,
            RankingGenre.Society_Politics_News => Mntone.Nico2.Videos.Ranking.RankingGenre.Society_Politics_News,
            RankingGenre.Technology_Craft => Mntone.Nico2.Videos.Ranking.RankingGenre.Technology_Craft,
            RankingGenre.Commentary_Lecture => Mntone.Nico2.Videos.Ranking.RankingGenre.Commentary_Lecture,
            RankingGenre.Anime => Mntone.Nico2.Videos.Ranking.RankingGenre.Anime,
            RankingGenre.Game => Mntone.Nico2.Videos.Ranking.RankingGenre.Game,
            RankingGenre.Other => Mntone.Nico2.Videos.Ranking.RankingGenre.Other,
            RankingGenre.R18 => Mntone.Nico2.Videos.Ranking.RankingGenre.R18,
            _ => throw new NotSupportedException(genre.ToString()),
        };
    }
}

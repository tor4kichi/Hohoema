using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Events
{
    public class RankingGenreCustomizeEventArgs
    {
        public Mntone.Nico2.Videos.Ranking.RankingGenre RankingGenre { get; set; }
        public string Tag { get; set; }
    }

    public class RankingGenreFavoriteRequestEventArgs
    {
        public Mntone.Nico2.Videos.Ranking.RankingGenre RankingGenre { get; set; }
        public string Tag { get; set; }
        public string Label { get; set; }
    }


    public sealed class RankingGenreShowRequestedEvent : PubSubEvent<RankingGenreCustomizeEventArgs> { }
    public sealed class RankingGenreHiddenRequestedEvent : PubSubEvent<RankingGenreCustomizeEventArgs> { }

    public sealed class RankingGenreFavoriteRequestedEvent : PubSubEvent<RankingGenreFavoriteRequestEventArgs> { }
    public sealed class RankingGenreUnFavoriteRequestedEvent : PubSubEvent<RankingGenreCustomizeEventArgs> { }
}

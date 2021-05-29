namespace NiconicoToolkit.Ranking.Video
{
    public static class VideoRankingConstants
    {
        public const string NiconicoRankingGenreDomain = "https://www.nicovideo.jp/ranking/genre/";
        public const string NiconicoRankingHotTopicDomain = "https://www.nicovideo.jp/ranking/hot-topic";

        public const int MaxPage = 10;
        public const int MaxPageWithTag = 3;
        public const int MaxPageHotTopic = 3;
        public const int MaxPageHotTopicWithKey = 1;

        public const int ItemsCountPerPage = 100;

        public static readonly RankingTerm[] AllRankingTerms = new[]
        {
                RankingTerm.Hour,
                RankingTerm.Day,
                RankingTerm.Week,
                RankingTerm.Month,
                RankingTerm.Total
            };


        public static readonly RankingTerm[] HotTopicAccepteRankingTerms = new[]
        {
                RankingTerm.Hour,
                RankingTerm.Day
            };

        public static readonly RankingTerm[] GenreWithTagAccepteRankingTerms = new[]
        {
                RankingTerm.Hour,
                RankingTerm.Day
            };

    }



}


namespace Hohoema.Models.Repository.Niconico.NicoVideoRanking
{
    public sealed class RankingSettingsRepository : FlagsRepositoryBase
    {
        public RankingGenre[] HiddenGenres
        {
            get => Read(new RankingGenre[0]);
            set => Save(value);
        }

        public RankingGenreTag[] HiddenTags
        {
            get => Read(new RankingGenreTag[0]);
            set => Save(value);
        }

        public RankingGenreTag[] FavoriteTags
        {
            get => Read(new RankingGenreTag[0]);
            set => Save(value);
        }
    }


    public class RankingGenreTag
    {
        public string Label { get; set; }
        public RankingGenre Genre { get; set; }
        public string Tag { get; set; }
    }
}

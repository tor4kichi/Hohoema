
using Hohoema.Models.Repository.Niconico.NicoVideo.Ranking;

namespace Hohoema.Models.Repository.App
{
    using RankingGenreTag = Niconico.NicoVideo.Ranking.RankingGenreTag;

    public sealed class RankingSettingsRepository : FlagsRepositoryBase
    {
        private RankingGenre[] _HiddenGenres;
        public RankingGenre[] HiddenGenres
        {
            get => _HiddenGenres ??= Read(new RankingGenre[0]);
            set => Save(value);
        }

        private RankingGenreTag[] _HiddenTags;
        public RankingGenreTag[] HiddenTags
        {
            get => _HiddenTags ??= Read(new RankingGenreTag[0]);
            set => Save(value);
        }

        private RankingGenreTag[] _FavoriteTags;
        public RankingGenreTag[] FavoriteTags
        {
            get => _FavoriteTags ??= Read(new RankingGenreTag[0]);
            set => Save(value);
        }
    }
}

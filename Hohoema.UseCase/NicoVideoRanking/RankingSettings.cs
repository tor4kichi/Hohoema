using Hohoema.Models.Repository.Niconico.NicoVideoRanking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.UseCase.NicoVideoRanking
{
    public sealed class RankingSettings
    {
        private readonly RankingSettingsRepository _rankingSettingsRepository;

        public RankingSettings(RankingSettingsRepository rankingSettingsRepository)
        {
            _rankingSettingsRepository = rankingSettingsRepository;

            HiddenTags = _rankingSettingsRepository.HiddenTags.ToList();
            HiddenGenres = _rankingSettingsRepository.HiddenGenres.ToHashSet();
            FavoriteTags = _rankingSettingsRepository.FavoriteTags.ToList();
        }


        public List<RankingGenreTag> HiddenTags { get; } 

        public HashSet<RankingGenre> HiddenGenres { get; }

        public List<RankingGenreTag> FavoriteTags { get; }
        

        public bool IsHiddenTag(RankingGenre genre, string tag)
        {
            return HiddenTags.Any(x => x.Genre == genre && x.Tag == tag);
        }

        public void RemoveHiddenTag(RankingGenre genre, string tag)
        {
            var target = HiddenTags.FirstOrDefault(x => x.Genre == genre && x.Tag == tag);
            if (target != null)
            {
                if (HiddenTags.Remove(target))
                {
                    _rankingSettingsRepository.HiddenTags = HiddenTags.ToArray();
                }
            }
        }

        public void AddHiddenTag(RankingGenre genre, string tag, string label)
        {
            if (false == HiddenTags.Any(x => x.Genre == genre && x.Tag == tag))
            {
                HiddenTags.Add(new RankingGenreTag()
                {
                    Tag = tag,
                    Label = label,
                    Genre = genre
                });

                _rankingSettingsRepository.HiddenTags = HiddenTags.ToArray();
            }
        }


        public bool IsHiddenGenre(RankingGenre genre)
        {
            return HiddenGenres.Contains(genre);
        }

        public void RemoveHiddenGenre(RankingGenre genre)
        {
            if (HiddenGenres.Remove(genre))
            {
                _rankingSettingsRepository.HiddenGenres = HiddenGenres.ToArray();
            }
        }

        public void AddHiddenGenre(RankingGenre genre)
        {
            if (false == HiddenGenres.Contains(genre))
            {
                HiddenGenres.Add(genre);
                _rankingSettingsRepository.HiddenGenres = HiddenGenres.ToArray();
            }
        }


        public bool IsFavoriteTag(RankingGenre genre, string tag)
        {
            return FavoriteTags.Any(x => x.Genre == genre && x.Tag == tag);
        }

        public void RemoveFavoriteTag(RankingGenre genre, string tag)
        {
            var target = FavoriteTags.FirstOrDefault(x => x.Genre == genre && x.Tag == tag);
            if (target != null)
            {
                if (FavoriteTags.Remove(target))
                {
                    _rankingSettingsRepository.FavoriteTags = FavoriteTags.ToArray();
                }
            }
        }

        public void AddFavoriteTag(RankingGenre genre, string tag, string label)
        {
            if (false == FavoriteTags.Any(x => x.Genre == genre && x.Tag == tag))
            {
                FavoriteTags.Add(new RankingGenreTag()
                {
                    Tag = tag,
                    Label = label,
                    Genre = genre
                });

                _rankingSettingsRepository.FavoriteTags = FavoriteTags.ToArray();
            }
        }

    }
}

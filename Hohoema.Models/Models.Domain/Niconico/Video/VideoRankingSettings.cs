using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Collections;
using Hohoema.Models.Infrastructure;
using Uno.Extensions;
using NiconicoToolkit.Ranking.Video;

namespace Hohoema.Models.Domain.Niconico.Video
{
	public class VideoRankingSettings : FlagsRepositoryBase
	{
		public VideoRankingSettings()
		{
            _HiddenTags = Read(new List<RankingGenreTag>(), nameof(HiddenTags));
            HiddenTags = new ReadOnlyCollection<RankingGenreTag>(_HiddenTags);
            HiddenGenres = Read(new HashSet<RankingGenre>(), nameof(HiddenGenres));
            _FavoriteTags = Read(new List<RankingGenreTag>(), nameof(FavoriteTags));
            FavoriteTags = new ReadOnlyCollection<RankingGenreTag>(_FavoriteTags);
        }


        List<RankingGenreTag> _HiddenTags;
        public IReadOnlyCollection<RankingGenreTag> HiddenTags { get; }

        public HashSet<RankingGenre> HiddenGenres { get; }

        List<RankingGenreTag> _FavoriteTags;
        public IReadOnlyCollection<RankingGenreTag> FavoriteTags { get; }


        public bool IsHiddenTag(RankingGenre genre, string tag)
        {
            return _HiddenTags.Any(x => x.Genre == genre && x.Tag == tag);
        }

        public void RemoveHiddenTag(RankingGenre genre, string tag)
        {
            var target = _HiddenTags.FirstOrDefault(x => x.Genre == genre && x.Tag == tag);
            if (target != null)
            {
                _HiddenTags.Remove(target);
                Save(_HiddenTags.ToList(), nameof(HiddenTags));
            }
        }

        public void AddHiddenTag(RankingGenre genre, string tag, string label)
        {
            if (false == _HiddenTags.Any(x => x.Genre == genre && x.Tag == tag))
            {
                _HiddenTags.Add(new RankingGenreTag()
                {
                    Tag = tag,
                    Label = label,
                    Genre = genre
                });
                Save(_HiddenTags.ToList(), nameof(HiddenTags));
            }
        }


        public bool IsHiddenGenre(RankingGenre genre)
        {
            return HiddenGenres.Contains(genre);
        }

        public void RemoveHiddenGenre(RankingGenre genre)
        {
            HiddenGenres.Remove(genre);
            Save(HiddenGenres, nameof(HiddenGenres));
        }

        public void ResetHiddenGenre(IEnumerable<RankingGenre> genreList)
        {
            HiddenGenres.Clear();
            HiddenGenres.AddRange(genreList);
            Save(HiddenGenres, nameof(HiddenGenres));
        }


        public void AddHiddenGenre(RankingGenre genre)
        {
            if (false == HiddenGenres.Contains(genre))
            {
                HiddenGenres.Add(genre);
                Save(HiddenGenres, nameof(HiddenGenres));
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
                _FavoriteTags.Remove(target);
                Save(_FavoriteTags.ToList(), nameof(FavoriteTags));
            }
        }

        public void AddFavoriteTag(RankingGenre genre, string tag, string label)
        {
            if (false == FavoriteTags.Any(x => x.Genre == genre && x.Tag == tag))
            {
                _FavoriteTags.Add(new RankingGenreTag()
                {
                    Tag = tag,
                    Label = label,
                    Genre = genre
                });
                Save(_FavoriteTags.ToList(), nameof(FavoriteTags));
            }
        }
	}

    public record RankingGenreTag
    {
        public string Label { get; set; }
        public RankingGenre Genre { get; set; }
        public string Tag { get; set; }
    }


}

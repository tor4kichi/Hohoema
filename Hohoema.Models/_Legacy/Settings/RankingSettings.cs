using Mntone.Nico2.Videos.Ranking;
using Hohoema.Services.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Collections;

namespace Hohoema.Models
{
    using RankingGenre = Models.Repository.Niconico.NicoVideo.Ranking.RankingGenre;

    [Obsolete]
    [DataContract]
	public class RankingSettings : SettingsBase
	{
		public RankingSettings()
			: base()
		{
		}

        [DataMember]
        public List<RankingGenreTag> HiddenTags { get; set; } = new List<RankingGenreTag>();

        [DataMember]
        public HashSet<RankingGenre> HiddenGenres { get; set; } = new HashSet<RankingGenre>();


        [DataMember]
        public List<RankingGenreTag> FavoriteTags { get; set; } = new List<RankingGenreTag>();


        public bool IsHiddenTag(RankingGenre genre, string tag)
        {
            return HiddenTags.Any(x => x.Genre == genre && x.Tag == tag);
        }

        public void RemoveHiddenTag(RankingGenre genre, string tag)
        {
            var target = HiddenTags.FirstOrDefault(x => x.Genre == genre && x.Tag == tag);
            if (target != null)
            {
                HiddenTags.Remove(target);
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
            }
        }


        public bool IsHiddenGenre(RankingGenre genre)
        {
            return HiddenGenres.Contains(genre);
        }

        public void RemoveHiddenGenre(RankingGenre genre)
        {
            HiddenGenres.Remove(genre);
        }

        public void AddHiddenGenre(RankingGenre genre)
        {
            if (false == HiddenGenres.Contains(genre))
            {
                HiddenGenres.Add(genre);
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
                FavoriteTags.Remove(target);
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
            }
        }


        public void ResetCategoryPriority()
		{
            Save().ConfigureAwait(false);
		}


		public override void OnInitialize()
		{
			ResetCategoryPriority();
		}

		protected override void Validate()
		{

		}
	}

    public class RankingGenreTag
    {
        public string Label { get; set; }
        public RankingGenre Genre { get; set; }
        public string Tag { get; set; }
    }


}

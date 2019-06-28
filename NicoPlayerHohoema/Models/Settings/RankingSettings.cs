using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Services.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Collections;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class RankingSettings : SettingsBase
	{
		public RankingSettings()
			: base()
		{
		}

        [DataMember]
        public HashSet<string> HiddenTags { get; set; } = new HashSet<string>();

        [DataMember]
        public HashSet<RankingGenre> HiddenGenres { get; set; } = new HashSet<RankingGenre>();
        

        public bool IsHiddenTag(string tag)
        {
            return HiddenTags.Contains(tag);
        }

        public bool IsHiddenGenre(RankingGenre genre)
        {
            return HiddenGenres.Contains(genre);
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

}

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

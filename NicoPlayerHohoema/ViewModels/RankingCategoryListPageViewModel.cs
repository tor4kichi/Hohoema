using Mntone.Nico2.Videos.Ranking;
using Prism.Windows.Mvvm;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Navigation;
using Prism.Commands;
using NicoPlayerHohoema.Models;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public class RankingCategoryListPageViewModel : HohoemaViewModelBase
	{
		public RankingCategoryListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			_RankingSettings = HohoemaApp.UserSettings.RankingSettings;

			// ランキングのカテゴリ
			RankingCategoryItems = new ObservableCollection<RankingCategoryHostListItem>()
			{
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.all), OnRankingCategorySelected),
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_ent2), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.ent),
						CreateRankingCategryListItem(RankingCategory.music),
						CreateRankingCategryListItem(RankingCategory.sing),
						CreateRankingCategryListItem(RankingCategory.dance),
						CreateRankingCategryListItem(RankingCategory.play),
						CreateRankingCategryListItem(RankingCategory.vocaloid),
						CreateRankingCategryListItem(RankingCategory.nicoindies),
					}
				},
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_life2), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.animal),
						CreateRankingCategryListItem(RankingCategory.cooking),
						CreateRankingCategryListItem(RankingCategory.nature),
						CreateRankingCategryListItem(RankingCategory.travel),
						CreateRankingCategryListItem(RankingCategory.sport),
						CreateRankingCategryListItem(RankingCategory.lecture),
						CreateRankingCategryListItem(RankingCategory.drive),
						CreateRankingCategryListItem(RankingCategory.history),
					}
				},
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_politics), OnRankingCategorySelected),
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_tech), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.science),
						CreateRankingCategryListItem(RankingCategory.tech),
						CreateRankingCategryListItem(RankingCategory.handcraft),
						CreateRankingCategryListItem(RankingCategory.make),
					}
				},
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_culture2), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.anime),
						CreateRankingCategryListItem(RankingCategory.game),
						CreateRankingCategryListItem(RankingCategory.toho),
						CreateRankingCategryListItem(RankingCategory.imas),
						CreateRankingCategryListItem(RankingCategory.radio),
						CreateRankingCategryListItem(RankingCategory.draw),
					}
				},
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_other), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.are),
						CreateRankingCategryListItem(RankingCategory.diary),
						CreateRankingCategryListItem(RankingCategory.other),
					}
				},
			};
		}

		RankingCategoryListItem CreateRankingCategryListItem(RankingCategory category)
		{
			return new RankingCategoryListItem(RankingCategoryInfo.CreateFromRankingCategory(category), OnRankingCategorySelected);
		}


		internal void OnRankingCategorySelected(RankingCategoryInfo info)
		{
			PageManager.OpenPage(HohoemaPageType.RankingCategory, info.ToParameterString());
		}


		public ObservableCollection<RankingCategoryHostListItem> RankingCategoryItems { get; private set; }

		RankingSettings _RankingSettings;
	}



	public class RankingCategoryHostListItem : RankingCategoryListItem
	{
		public RankingCategoryHostListItem(RankingCategoryInfo info, Action<RankingCategoryInfo> selected)
			: base(info, selected)
		{
			ChildItems = new List<RankingCategoryListItem>();
		}


		public List<RankingCategoryListItem> ChildItems { get; set; }
	}
}

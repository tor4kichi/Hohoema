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


			Func< RankingCategory, bool> checkFavorite = (RankingCategory cat) => 
			{
				return _RankingSettings.HighPriorityCategory.Any(x => x.RankingSource == RankingSource.CategoryRanking && x.Parameter == cat.ToString());
			};

			// ランキングのカテゴリ
			RankingCategoryItems = new ObservableCollection<RankingCategoryHostListItem>()
			{
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.all), checkFavorite(RankingCategory.all), OnRankingCategorySelected),
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_ent2), checkFavorite(RankingCategory.g_ent2), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListPageListItem>()
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
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_life2), checkFavorite(RankingCategory.g_life2), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListPageListItem>()
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
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_politics), checkFavorite(RankingCategory.g_politics), OnRankingCategorySelected),
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_tech), checkFavorite(RankingCategory.g_tech), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListPageListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.science),
						CreateRankingCategryListItem(RankingCategory.tech),
						CreateRankingCategryListItem(RankingCategory.handcraft),
						CreateRankingCategryListItem(RankingCategory.make),
					}
				},
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_culture2), checkFavorite(RankingCategory.g_culture2), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListPageListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.anime),
						CreateRankingCategryListItem(RankingCategory.game),
						CreateRankingCategryListItem(RankingCategory.toho),
						CreateRankingCategryListItem(RankingCategory.imas),
						CreateRankingCategryListItem(RankingCategory.radio),
						CreateRankingCategryListItem(RankingCategory.draw),
					}
				},
				new RankingCategoryHostListItem(RankingCategoryInfo.CreateFromRankingCategory(RankingCategory.g_other), checkFavorite(RankingCategory.g_other), OnRankingCategorySelected)
				{
					ChildItems = new List<RankingCategoryListPageListItem>()
					{
						CreateRankingCategryListItem(RankingCategory.are),
						CreateRankingCategryListItem(RankingCategory.diary),
						CreateRankingCategryListItem(RankingCategory.other),
					}
				},
			};
		}

		RankingCategoryListPageListItem CreateRankingCategryListItem(RankingCategory category)
		{
			var categoryInfo = RankingCategoryInfo.CreateFromRankingCategory(category);
			var isFavoriteCategory = HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory.Contains(categoryInfo);
			return new RankingCategoryListPageListItem(categoryInfo, isFavoriteCategory, OnRankingCategorySelected);
		}


		internal void OnRankingCategorySelected(RankingCategoryInfo info)
		{
			PageManager.OpenPage(HohoemaPageType.RankingCategory, info.ToParameterString());
		}


		public ObservableCollection<RankingCategoryHostListItem> RankingCategoryItems { get; private set; }

		RankingSettings _RankingSettings;
	}



	public class RankingCategoryHostListItem : RankingCategoryListPageListItem
	{
		public RankingCategoryHostListItem(RankingCategoryInfo info, bool isFavoriteCategory, Action<RankingCategoryInfo> selected)
			: base(info, isFavoriteCategory, selected)
		{
			ChildItems = new List<RankingCategoryListPageListItem>();
		}


		public List<RankingCategoryListPageListItem> ChildItems { get; set; }
	}


	public class RankingCategoryListPageListItem : RankingCategoryListItem
	{
		public Windows.UI.Text.FontWeight FontWeight { get; private set; }
		public bool IsFavorite { get; private set; }

		public RankingCategoryListPageListItem(RankingCategoryInfo info, bool isFavoriteCategory, Action<RankingCategoryInfo> selected)
			: base(info, selected)
		{
			IsFavorite = isFavoriteCategory;

			FontWeight = IsFavorite ? Windows.UI.Text.FontWeights.Bold : Windows.UI.Text.FontWeights.Normal;
		}
	}
}

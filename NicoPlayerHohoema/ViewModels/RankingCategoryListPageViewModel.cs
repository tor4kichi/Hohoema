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

		private static readonly List<List<RankingCategory>> RankingCategories;

		static RankingCategoryListPageViewModel()
		{
			RankingCategories = new List<List<RankingCategory>>()
			{
				new List<RankingCategory>()
				{
					RankingCategory.all
				},
				new List<RankingCategory>()
				{
					RankingCategory.g_ent2,
					RankingCategory.ent,
					RankingCategory.music,
					RankingCategory.sing,
					RankingCategory.dance,
					RankingCategory.play,
					RankingCategory.vocaloid,
					RankingCategory.nicoindies
				},
				new List<RankingCategory>()
				{
					RankingCategory.g_life2,
					RankingCategory.animal,
					RankingCategory.cooking,
					RankingCategory.nature,
					RankingCategory.travel,
					RankingCategory.sport,
					RankingCategory.lecture,
					RankingCategory.drive,
					RankingCategory.history,
				},
				new List<RankingCategory>()
				{
					RankingCategory.g_politics
				},
				new List<RankingCategory>()
				{
					RankingCategory.g_tech,
					RankingCategory.science,
					RankingCategory.tech,
					RankingCategory.handcraft,
					RankingCategory.make,
				},
				new List<RankingCategory>()
				{
					RankingCategory.g_culture2,
					RankingCategory.anime,
					RankingCategory.game,
					RankingCategory.jikkyo,
					RankingCategory.toho,
					RankingCategory.imas,
					RankingCategory.radio,
					RankingCategory.draw,
				},
				new List<RankingCategory>()
				{
					RankingCategory.g_other,
					RankingCategory.are,
					RankingCategory.diary,
					RankingCategory.other,

				}

			};

		}

		public RankingCategoryListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
			: base(hohoemaApp, pageManager)
		{
			_RankingSettings = HohoemaApp.UserSettings.RankingSettings;


			Func< RankingCategory, bool> checkFavorite = (RankingCategory cat) => 
			{
				return _RankingSettings.HighPriorityCategory.Any(x => x.RankingSource == RankingSource.CategoryRanking && x.Parameter == cat.ToString());
			};


			RankingCategoryItems = new ObservableCollection<List<RankingCategoryListPageListItem>>();
		}

		RankingCategoryListPageListItem CreateRankingCategryListItem(RankingCategory category)
		{
			var categoryInfo = RankingCategoryInfo.CreateFromRankingCategory(category);
			var isFavoriteCategory = HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory.Contains(categoryInfo);
			return new RankingCategoryListPageListItem(categoryInfo, isFavoriteCategory, OnRankingCategorySelected);
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			RankingCategoryItems.Clear();
			foreach (var categoryList in RankingCategories)
			{
				// 非表示ランキングを除外したカテゴリリストを作成
				var list = categoryList
					.Where(x => !HohoemaApp.UserSettings.RankingSettings.IsDislikeRankingCategory(x))
					.Select(x => CreateRankingCategryListItem(x))
					.ToList();

				// 表示対象があればリストに追加
				if (list.Count > 0)
				{
					RankingCategoryItems.Add(list);
				}
			}

			OnPropertyChanged(nameof(RankingCategoryItems));

			base.OnNavigatedTo(e, viewModelState);
		}
		
		internal void OnRankingCategorySelected(RankingCategoryInfo info)
		{
			PageManager.OpenPage(HohoemaPageType.RankingCategory, info.ToParameterString());
		}


		public ObservableCollection<List<RankingCategoryListPageListItem>> RankingCategoryItems { get; private set; }

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

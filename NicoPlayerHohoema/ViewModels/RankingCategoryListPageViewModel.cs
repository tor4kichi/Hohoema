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
	public class RankingCategoryListPageViewModel : ViewModelBase
	{
		public RankingCategoryListPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager)
		{
			_HohoemaApp = hohoemaApp;
			_RankingSettings = _HohoemaApp.UserSettings.RankingSettings;

			// ランキングのカテゴリ
			HighPriorityRankingCategoryItems = new ObservableCollection<RankingCategoryListItem>();
			MiddlePriorityRankingCategoryItems = new ObservableCollection<RankingCategoryListItem>();
			LowPriorityRankingCategoryItems = new ObservableCollection<RankingCategoryListItem>();

			SelectedRankingCategory = new ReactiveProperty<RankingCategoryListItem>();

			SelectedRankingCategory
				.Where(x => x != null)
				.Subscribe(category => 
			{
				// RankingCategoryPageを開く
				pageManager.OpenPage(HohoemaPageType.RankingCategory, category.CategoryInfo);
			});
		}

		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			base.OnNavigatedTo(e, viewModelState);

			HighPriorityRankingCategoryItems.Clear();
			MiddlePriorityRankingCategoryItems.Clear();
			LowPriorityRankingCategoryItems.Clear();


			foreach (var categoryType in _RankingSettings.HighPriorityCategory)
			{
				HighPriorityRankingCategoryItems.Add(new RankingCategoryListItem(categoryType));
			}

			foreach (var categoryType in _RankingSettings.MiddlePriorityCategory)
			{
				MiddlePriorityRankingCategoryItems.Add(new RankingCategoryListItem(categoryType));
			}
			foreach (var categoryType in _RankingSettings.LowPriorityCategory)
			{
				LowPriorityRankingCategoryItems.Add(new RankingCategoryListItem(categoryType));
			}
			

		}



		public ObservableCollection<RankingCategoryListItem> HighPriorityRankingCategoryItems { get; private set; }
		public ObservableCollection<RankingCategoryListItem> MiddlePriorityRankingCategoryItems { get; private set; }
		public ObservableCollection<RankingCategoryListItem> LowPriorityRankingCategoryItems { get; private set; }

		public ReactiveProperty<RankingCategoryListItem> SelectedRankingCategory { get; private set; }

		RankingSettings _RankingSettings;
		private HohoemaApp _HohoemaApp;
	}
}

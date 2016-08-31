using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class RankingPortalPageContentViewModel : PotalPageContentViewModel
	{
		public RankingPortalPageContentViewModel(PageManager pageManager, HohoemaApp hohoemaApp)
			: base(pageManager)
		{
			_HohoemaApp = hohoemaApp;


			PriorityRankingCategories = _HohoemaApp.UserSettings.RankingSettings.HighPriorityCategory.ToReadOnlyReactiveCollection(
				x => new RankingCategoryListItem(x, OnRankingListItemSelected)
				);

			HasPriorityRankingCategoryItem = PriorityRankingCategories.ObserveProperty(x => x.Count)
				.Select(x => x > 0)
				.ToReadOnlyReactiveProperty();
		}

		protected override Task NavigateTo()
		{
			return base.NavigateTo();
		}

		private void OnRankingListItemSelected(RankingCategoryInfo info)
		{
			PageManager.OpenPage(HohoemaPageType.RankingCategory, info.ToParameterString());
		}


		private DelegateCommand _OpenRankingCategoryCommand;
		public DelegateCommand OpenRankingCategoryCommand
		{
			get
			{
				return _OpenRankingCategoryCommand
					?? (_OpenRankingCategoryCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.RankingCategoryList);
					}));
			}
		}

		private DelegateCommand _OpenVideoListingSettingsCommand;
		public DelegateCommand OpenVideoListingSettingsCommand
		{
			get
			{
				return _OpenVideoListingSettingsCommand
					?? (_OpenVideoListingSettingsCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(HohoemaPageType.Settings, HohoemaSettingsKind.VideoList.ToString());
					}));
			}
		}

		public ReadOnlyReactiveProperty<bool> HasPriorityRankingCategoryItem { get; private set; }

		public ReadOnlyReactiveCollection<RankingCategoryListItem> PriorityRankingCategories { get; private set; }


		HohoemaApp _HohoemaApp;
	}
}

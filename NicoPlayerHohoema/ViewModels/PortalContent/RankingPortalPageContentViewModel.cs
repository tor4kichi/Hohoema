using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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


		public ReadOnlyReactiveCollection<RankingCategoryListItem> PriorityRankingCategories { get; private set; }


		HohoemaApp _HohoemaApp;
	}
}

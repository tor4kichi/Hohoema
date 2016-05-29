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
		{
			_PageManager = pageManager;
			_HohoemaApp = hohoemaApp;

			PriorityRankingCategories = hohoemaApp.UserSettings.RankingSettings.HighPriorityCategory.ToReadOnlyReactiveCollection(
				x => new RankingCategoryListItem(x, OnRankingListItemSelected)
				);


			
		}

		private void OnRankingListItemSelected(RankingCategoryInfo info)
		{
			_PageManager.OpenPage(HohoemaPageType.RankingCategory, info.ToParameterString());
		}


		private DelegateCommand _OpenRankingCategoryCommand;
		public DelegateCommand OpenRankingCategoryCommand
		{
			get
			{
				return _OpenRankingCategoryCommand
					?? (_OpenRankingCategoryCommand = new DelegateCommand(() =>
					{
						_PageManager.OpenPage(HohoemaPageType.RankingCategoryList);
					}));
			}
		}


		public ReadOnlyReactiveCollection<RankingCategoryListItem> PriorityRankingCategories { get; private set; }


		PageManager _PageManager;
		HohoemaApp _HohoemaApp;
	}
}

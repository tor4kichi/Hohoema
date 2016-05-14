using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
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



		PageManager _PageManager;
		HohoemaApp _HohoemaApp;
	}
}

using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class SearchPortalPageContentViewModel : PotalPageContentViewModel
	{

		public SearchPortalPageContentViewModel(PageManager pageManager, HohoemaApp hohoemaApp)
		{
			_PageManager = pageManager;
			_HohoemaApp = hohoemaApp;
		}


		private DelegateCommand<string> _SearchCommand;
		public DelegateCommand<string> SearchCommand
		{
			get
			{
				return _SearchCommand
					?? (_SearchCommand = new DelegateCommand<string>(word => 
					{
						_PageManager.OpenPage(HohoemaPageType.Search, word);
					}));
			}
		}



		PageManager _PageManager;
		HohoemaApp _HohoemaApp;
	}
}

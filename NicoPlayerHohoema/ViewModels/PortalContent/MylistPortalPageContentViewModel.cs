using NicoPlayerHohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels.PortalContent
{
	public class MylistPortalPageContentViewModel : PotalPageContentViewModel
	{
		public MylistPortalPageContentViewModel(PageManager pageManager, HohoemaApp hohoemaApp)
		{
			_PageManager = pageManager;
			_HohoemaApp = hohoemaApp;
		}


		private DelegateCommand _OpenMylistCommand;
		public DelegateCommand OpenMylistCommand
		{
			get
			{
				return _OpenMylistCommand
					?? (_OpenMylistCommand = new DelegateCommand(() =>
					{
						_PageManager.OpenPage(HohoemaPageType.Mylist);
					}));
			}
		}



		PageManager _PageManager;
		HohoemaApp _HohoemaApp;
	}
}

using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	abstract public class HohoemaViewModelBase : ViewModelBase
	{
		public HohoemaViewModelBase(PageManager pageManager)
		{
			PageManager = pageManager;
		}

		abstract public string GetPageTitle();


		private DelegateCommand _BackCommand;
		public DelegateCommand BackCommand
		{
			get
			{
				return _BackCommand
					?? (_BackCommand = new DelegateCommand(
						() => 
						{
							if (PageManager.NavigationService.CanGoBack())
							{
								PageManager.NavigationService.GoBack();
							}
							else
							{
								PageManager.OpenPage(HohoemaPageType.Portal);
							}
						}));
			}
		}


		public PageManager PageManager { get; private set; }
	}
}

using Prism.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NicoPlayerHohoema.Models
{
	public class PageManager : BindableBase
	{
		public INavigationService NavigationService { get; private set; }

		private HohoemaPageType _CurrentPageType;
		public HohoemaPageType CurrentPageType
		{
			get { return _CurrentPageType; }
			set { SetProperty(ref _CurrentPageType, value); }
		}

		public PageManager(INavigationService ns)
		{
			NavigationService = ns;
			CurrentPageType = HohoemaPageType.Ranking;
		}

		public void OpenPage<NavigateParamType>(HohoemaPageType pageType, NavigateParamType parameter)
		{
			if (NavigationService.Navigate(pageType.ToString(), parameter))
			{
				CurrentPageType = pageType;
			}
		}

		public void OpenPage(HohoemaPageType pageType)
		{
			if (NavigationService.Navigate(pageType.ToString(), null))
			{
				CurrentPageType = pageType;
			}
		}
	}

}

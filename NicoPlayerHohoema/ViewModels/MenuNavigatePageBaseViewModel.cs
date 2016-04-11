using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class MenuNavigatePageBaseViewModel : BindableBase
	{
		public INavigationService NavigationService { get; private set; }

		public List<MenuListItemViewModel> TopMenuItems { get; private set; }
		public List<MenuListItemViewModel> BottomMenuItems { get; private set; }


		
		public MenuNavigatePageBaseViewModel(INavigationService navigationService)
		{
			NavigationService = navigationService;


			TopMenuItems = new List<MenuListItemViewModel>()
			{
				new MenuListItemViewModel(NavigationService)
				{
					Title = "ランキング",
					PageName = "Ranking",
				}
				, new MenuListItemViewModel(NavigationService)
				{
					Title = "購読",
					PageName = "Subscription",
				}
				, new MenuListItemViewModel(NavigationService)
				{
					Title = "履歴",
					PageName = "History",
				}
				, new MenuListItemViewModel(NavigationService)
				{
					Title = "検索",
					PageName = "Search",
				}
			};


			BottomMenuItems = new List<MenuListItemViewModel>()
			{
				new MenuListItemViewModel(NavigationService)
				{
					Title = "設定",
					PageName = "Settings",
				}
				, new MenuListItemViewModel(NavigationService)
				{
					Title = "アカウント",
					PageName = "Settings",
					PageParameter = "Account"
				}
			};

		}



		
	}

	public class MenuListItemViewModel : BindableBase
	{
		public INavigationService NavigationService { get; private set; }

		public MenuListItemViewModel(INavigationService ns)
		{
			NavigationService = ns;

		}

		public string Title { get; set; } = "";
		public string PageName { get; set; } = "";
		public string PageParameter { get; set; } = "";

		private DelegateCommand _SelectMenuItemCommand;
		public DelegateCommand SelectMenuItemCommand
		{
			get
			{
				return _SelectMenuItemCommand
					?? (_SelectMenuItemCommand = new DelegateCommand(() =>
					{
						// TODO: Menuから画面を開く時のパラメータ設定対応
						NavigationService.Navigate(PageName, null);
					}));
			}
		}
	}
}

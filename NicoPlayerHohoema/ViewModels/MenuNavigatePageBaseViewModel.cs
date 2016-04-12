using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class MenuNavigatePageBaseViewModel : BindableBase
	{
		public PageManager PageManager { get; private set; }

		public List<MenuListItemViewModel> TopMenuItems { get; private set; }
		public List<MenuListItemViewModel> BottomMenuItems { get; private set; }

		public ReactiveProperty<bool> IsPaneOpen { get; private set; }
		
		public MenuNavigatePageBaseViewModel(PageManager pageManager)
		{
			PageManager = pageManager;

			IsPaneOpen = new ReactiveProperty<bool>(false);

			PageManager.ObserveProperty(x => x.CurrentPageType)
				.Subscribe(x => 
				{
					ClosePane();
				});

			TopMenuItems = new List<MenuListItemViewModel>()
			{
				new MenuListItemViewModel(PageManager)
				{
					Title = "ランキング",
					PageType = HohoemaPageType.Ranking,
				}
				, new MenuListItemViewModel(PageManager)
				{
					Title = "購読",
					PageType = HohoemaPageType.Subscription,
				}
				, new MenuListItemViewModel(PageManager)
				{
					Title = "履歴",
					PageType = HohoemaPageType.History,
				}
				, new MenuListItemViewModel(PageManager)
				{
					Title = "検索",
					PageType = HohoemaPageType.Search,
				}
			};


			BottomMenuItems = new List<MenuListItemViewModel>()
			{
				new MenuListItemViewModel(PageManager)
				{
					Title = "設定",
					PageType = HohoemaPageType.Settings,
				}
				, new MenuListItemViewModel(PageManager)
				{
					Title = "アカウント",
					PageType = HohoemaPageType.Settings,
					PageParameter = "Account"
				}
			};

			ClosePaneCommand = new DelegateCommand(ClosePane);
		}


		public DelegateCommand ClosePaneCommand { get; private set; } 

		public void ClosePane()
		{
			IsPaneOpen.Value = false;
		}

	}

	public class MenuListItemViewModel : BindableBase
	{
		public PageManager PageManager { get; private set; }

		public MenuListItemViewModel(PageManager pageManager)
		{
			PageManager = pageManager;

		}

		public string Title { get; set; }
		public HohoemaPageType PageType { get; set; }
		public string PageParameter { get; set; }

		private DelegateCommand _SelectMenuItemCommand;
		public DelegateCommand SelectMenuItemCommand
		{
			get
			{
				return _SelectMenuItemCommand
					?? (_SelectMenuItemCommand = new DelegateCommand(() =>
					{
						PageManager.OpenPage(PageType, PageParameter);
					}));
			}
		}
	}
}

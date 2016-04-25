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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.ViewModels
{
	public class MenuNavigatePageBaseViewModel : BindableBase
	{
		public PageManager PageManager { get; private set; }

		public List<MenuListItemViewModelBase> TopMenuItems { get; private set; }
		public List<MenuListItemViewModelBase> BottomMenuItems { get; private set; }

		public ReactiveProperty<bool> IsPaneOpen { get; private set; }

		public ReactiveProperty<bool> InvisiblePane { get; private set; }
		
		public MenuNavigatePageBaseViewModel(PageManager pageManager)
		{
			PageManager = pageManager;

			IsPaneOpen = new ReactiveProperty<bool>(false);
			InvisiblePane = new ReactiveProperty<bool>(true);

			PageManager.ObserveProperty(x => x.CurrentPageType)
				.Subscribe(pageType =>
				{
					if (pageType == HohoemaPageType.Login)
					{
						InvisiblePane.Value = true;
					}
					else
					{
						InvisiblePane.Value = false;
					}
				});

			TopMenuItems = new List<MenuListItemViewModelBase>()
			{
				new PageOpenMenuListItemViewModel(PageManager, this)
				{
					Title = "ランキング",
					PageType = HohoemaPageType.RankingCategoryList,
				}
				, new PageOpenMenuListItemViewModel(PageManager, this)
				{
					Title = "マイリスト",
					PageType = HohoemaPageType.Mylist,
				}
				, new PageOpenMenuListItemViewModel(PageManager, this)
				{
					Title = "履歴",
					PageType = HohoemaPageType.History,
				}
				, new PageOpenMenuListItemViewModel(PageManager, this)
				{
					Title = "検索",
					PageType = HohoemaPageType.Search,
				}
			};


			BottomMenuItems = new List<MenuListItemViewModelBase>()
			{
				new PageOpenMenuListItemViewModel(PageManager, this)
				{
					Title = "設定",
					PageType = HohoemaPageType.Settings,
				}
				, new LogoutMenuListItemViewModel(PageManager, this)
				{
					Title = "ログアウト",
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

	abstract public class MenuListItemViewModelBase : BindableBase
	{

		public MenuListItemViewModelBase(PageManager pageManager, MenuNavigatePageBaseViewModel parentVM)
		{
			PageManager = pageManager;
			ParentVM = parentVM;

		}

		abstract protected void OnSelected();

		private DelegateCommand<Visibility?> _SelectMenuItemCommand;
		public DelegateCommand<Visibility?> SelectMenuItemCommand
		{
			get
			{
				return _SelectMenuItemCommand
					?? (_SelectMenuItemCommand = new DelegateCommand<Visibility?>((paneToggleButtonVisiblity) =>
					{
						// ペインの切り替えボタンが使える場合は、ペインを閉じる
						if (paneToggleButtonVisiblity == Visibility.Visible)
						{
							ParentVM.ClosePane();
						}

						OnSelected();
					}));
			}
		}

		public string Title { get; set; }

		public MenuNavigatePageBaseViewModel ParentVM { get; private set; }
		public PageManager PageManager { get; private set; }


	}


	public class PageOpenMenuListItemViewModel : MenuListItemViewModelBase
	{

		public PageOpenMenuListItemViewModel(PageManager pageManager, MenuNavigatePageBaseViewModel parentVM)
			: base(pageManager, parentVM)
		{

		}

		public HohoemaPageType PageType { get; set; }
		public object PageParameter { get; set; }

		protected override void OnSelected()
		{
			PageManager.OpenPage(PageType, PageParameter);
		}
	}

	public class LogoutMenuListItemViewModel : MenuListItemViewModelBase
	{

		public LogoutMenuListItemViewModel(PageManager pageManager, MenuNavigatePageBaseViewModel parentVM)
			: base(pageManager, parentVM)
		{

		}

		protected override void OnSelected()
		{
			// TODO: ダウンロード中の場合、確認を表示する
			PageManager.OpenPage(HohoemaPageType.Login);
		}



	}
}

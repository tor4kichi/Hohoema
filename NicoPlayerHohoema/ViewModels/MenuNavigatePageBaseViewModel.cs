using NicoPlayerHohoema.Events;
using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Events;
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
using Prism.Windows;
using System.Reactive.Linq;
using NicoPlayerHohoema.Views.Service;

namespace NicoPlayerHohoema.ViewModels
{
	public class MenuNavigatePageBaseViewModel : BindableBase
	{
		public PageManager PageManager { get; private set; }

		
		public MenuNavigatePageBaseViewModel(PageManager pageManager, ISearchDialogService searchDialog)
		{
			PageManager = pageManager;
			_SearchDialogService = searchDialog;

			MenuItems = new List<PageTypeSelectableItem>()
			{
				new PageTypeSelectableItem(HohoemaPageType.Portal			  , OnMenuItemSelected, "ホーム"),
				new PageTypeSelectableItem(HohoemaPageType.RankingCategoryList, OnMenuItemSelected, "ランキング"),
				new PageTypeSelectableItem(HohoemaPageType.FavoriteList		  , OnMenuItemSelected, "お気に入り"),
				new PageTypeSelectableItem(HohoemaPageType.UserMylist		  , OnMenuItemSelected, "マイリスト"),
				new PageTypeSelectableItem(HohoemaPageType.History			  , OnMenuItemSelected, "視聴履歴"),
			};

			PersonalMenuItems = new List<PageTypeSelectableItem>()
			{
				new PageTypeSelectableItem(HohoemaPageType.CacheManagement	  , OnMenuItemSelected, "ダウンロード管理"),
				new PageTypeSelectableItem(HohoemaPageType.Settings			  , OnMenuItemSelected, "設定"),
			};

			SelectedItem = new ReactiveProperty<PageTypeSelectableItem>(MenuItems[0], mode: ReactivePropertyMode.DistinctUntilChanged);

			SelectedItem
				.Where(x => x != null)
				.Subscribe(x => 
			{
				OnMenuItemSelected(x.Source);
			});


			PageManager.ObserveProperty(x => x.CurrentPageType)
				.Subscribe(pageType => 
				{
					foreach (var item in MenuItems)
					{
						item.IsSelected = item.Source == pageType;
					}
					foreach (var item in PersonalMenuItems)
					{
						item.IsSelected = item.Source == pageType;
					}

					SelectedItem.Value = null;

					foreach (var item in MenuItems)
					{
						if (item.IsSelected)
						{
							SelectedItem.Value = item;
							break;
						}
					}

					foreach (var item in PersonalMenuItems)
					{
						if (item.IsSelected)
						{
							SelectedItem.Value = item;
							break;
						}
					}
				});
				


			IsPersonalPage = SelectedItem.Select(x =>
			{
				return MenuItems.All(y => x != y);
			})
			.ToReactiveProperty();

			IsPersonalPage.ForceNotify();

			PageManager.ObserveProperty(x => x.PageTitle)
				.Subscribe(x => TitleText = x);


			IsVisibleTopBar = PageManager.ObserveProperty(x => x.CurrentPageType)
				.Select(x => 
				{
					return !(x == HohoemaPageType.Login || x == HohoemaPageType.VideoPlayer);
				})
				.ToReactiveProperty();
		}

		internal void OnMenuItemSelected(HohoemaPageType pageType)
		{
			if (pageType != PageManager.CurrentPageType)
			{
				PageManager.OpenPage(pageType);
			}
		}


		private DelegateCommand _OpenSearchDialogCommand;
		public DelegateCommand OpenSearchDialogCommand
		{
			get
			{
				return _OpenSearchDialogCommand
					?? (_OpenSearchDialogCommand = new DelegateCommand(() => 
					{
						_SearchDialogService.ShowAsync();
					}));
			}
		}

		public List<PageTypeSelectableItem> MenuItems { get; private set; }

		public List<PageTypeSelectableItem> PersonalMenuItems { get; private set; }

		public ReactiveProperty<bool> IsVisibleTopBar { get; private set; }

		public ReactiveProperty<bool> IsPersonalPage { get; private set; }


		private string _TitleText;
		public string TitleText
		{
			get { return _TitleText; }
			set { SetProperty(ref _TitleText, value); }
		}

		public ReactiveProperty<PageTypeSelectableItem> SelectedItem { get; private set; }


		ISearchDialogService _SearchDialogService;
	}

	public class PageTypeSelectableItem : SelectableItem<HohoemaPageType>
	{
		public PageTypeSelectableItem(HohoemaPageType pageType, Action<HohoemaPageType> onSelected, string label)
			: base(pageType, onSelected)
		{
			Label = label;
			IsSelected = false;
		}

		private bool _IsSelected;
		public bool IsSelected
		{
			get { return _IsSelected; }
			set { SetProperty(ref _IsSelected, value); }
		}

		public string Label { get; set; }
	}



}

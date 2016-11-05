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

		
		public MenuNavigatePageBaseViewModel(PageManager pageManager)
		{
			PageManager = pageManager;

			// Symbol see@ https://msdn.microsoft.com/library/windows/apps/dn252842
			SplitViewDisplayMode = new ReactiveProperty<Windows.UI.Xaml.Controls.SplitViewDisplayMode>();
			CanClosePane = SplitViewDisplayMode.Select(x => x != Windows.UI.Xaml.Controls.SplitViewDisplayMode.Inline)
				.ToReactiveProperty();


			MenuItems = new List<PageTypeSelectableItem>()
			{
				new PageTypeSelectableItem(HohoemaPageType.Portal             , OnMenuItemSelected, "ホーム", Symbol.Home),
				new PageTypeSelectableItem(HohoemaPageType.RankingCategoryList, OnMenuItemSelected, "ランキング", Symbol.Flag),
				new PageTypeSelectableItem(HohoemaPageType.FollowManage     , OnMenuItemSelected, "フォロー", Symbol.OutlineStar),
				new PageTypeSelectableItem(HohoemaPageType.UserMylist		  , OnMenuItemSelected, "マイリスト", Symbol.Bookmarks),
				new PageTypeSelectableItem(HohoemaPageType.History			  , OnMenuItemSelected, "視聴履歴", Symbol.Clock),
				new PageTypeSelectableItem(HohoemaPageType.Search             , OnMenuItemSelected, "検索", Symbol.Find),
			};

			PersonalMenuItems = new List<PageTypeSelectableItem>()
			{
				new PageTypeSelectableItem(HohoemaPageType.FeedGroupManage    , OnMenuItemSelected, "フィード", Symbol.List),
				new PageTypeSelectableItem(HohoemaPageType.CacheManagement	  , OnMenuItemSelected, "キャッシュ管理", Symbol.Download),
				new PageTypeSelectableItem(HohoemaPageType.Settings			  , OnMenuItemSelected, "設定", Symbol.Setting),
				new PageTypeSelectableItem(HohoemaPageType.About			  , OnMenuItemSelected, "このアプリについて", Symbol.Help),
				new PageTypeSelectableItem(HohoemaPageType.Feedback           , OnMenuItemSelected, "フィードバック", Symbol.Comment),
				new PageTypeSelectableItem(HohoemaPageType.Login	          , OnMenuItemSelected, "ログアウト", Symbol.LeaveChat),
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
				.Subscribe(x =>
				{
					TitleText = x;
				});


			IsVisibleMenu = PageManager.ObserveProperty(x => x.CurrentPageType)
				.Select(x => 
				{
					return PageManager.DontNeedMenuPageTypes.All(dontNeedMenuPageType => x != dontNeedMenuPageType);
				})
				.ToReactiveProperty();




			PageManager.StartWork += PageManager_StartWork;
			PageManager.ProgressWork += PageManager_ProgressWork;
			PageManager.CompleteWork += PageManager_CompleteWork;
			PageManager.CancelWork += PageManager_CancelWork;
		}

		private void PageManager_StartWork(string title, uint totalCount)
		{
			WorkTitle = title;
			WorkTotalCount = totalCount;

			NowWorking = true;
		}


		private void PageManager_ProgressWork(uint count)
		{
			WorkCount = count;
		}

		private void PageManager_CompleteWork()
		{
			NowWorking = false;
		}

		private void PageManager_CancelWork()
		{
			NowWorking = false;
		}



		internal void OnMenuItemSelected(HohoemaPageType pageType)
		{
			if (pageType != PageManager.CurrentPageType)
			{
				PageManager.OpenPage(pageType);
			}
		}

		public List<PageTypeSelectableItem> MenuItems { get; private set; }

		public List<PageTypeSelectableItem> PersonalMenuItems { get; private set; }

		public ReactiveProperty<bool> IsVisibleMenu { get; private set; }

		public ReactiveProperty<bool> IsPersonalPage { get; private set; }


		/// <summary>
		/// 表示サイズによるPane表示方法の違い
		/// </summary>
		public ReactiveProperty<bool> CanClosePane { get; private set; }
		public ReactiveProperty<SplitViewDisplayMode> SplitViewDisplayMode { get; private set; }


		private string _TitleText;
		public string TitleText
		{
			get { return _TitleText; }
			set { SetProperty(ref _TitleText, value); }
		}



		private bool _NowWorking;
		public bool NowWorking
		{
			get { return _NowWorking; }
			set { SetProperty(ref _NowWorking, value); }
		}

		private string _WorkTitle;
		public string WorkTitle
		{
			get { return _WorkTitle; }
			set { SetProperty(ref _WorkTitle, value); }
		}

		private uint _WorkCount;
		public uint WorkCount
		{
			get { return _WorkCount; }
			set { SetProperty(ref _WorkCount, value); }
		}


		private uint _WorkTotalCount;
		public uint WorkTotalCount
		{
			get { return _WorkTotalCount; }
			set { SetProperty(ref _WorkTotalCount, value); }
		}


		public ReactiveProperty<PageTypeSelectableItem> SelectedItem { get; private set; }
	}

	public class PageTypeSelectableItem : SelectableItem<HohoemaPageType>
	{
		public PageTypeSelectableItem(HohoemaPageType pageType, Action<HohoemaPageType> onSelected, string label, Symbol iconType)
			: base(pageType, onSelected)
		{
			Label = label;
			IsSelected = false;
			IconType = iconType;
		}

		private bool _IsSelected;
		public bool IsSelected
		{
			get { return _IsSelected; }
			set { SetProperty(ref _IsSelected, value); }
		}

		public string Label { get; set; }
		public Symbol IconType { get; set; }
	}



}

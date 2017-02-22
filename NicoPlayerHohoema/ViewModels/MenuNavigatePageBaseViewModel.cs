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
		public HohoemaApp HohoemaApp { get; private set; }
        public AccountManagementDialogService AccountManagementDialogService { get; private set; }


        public ReactiveProperty<bool> IsTVModeEnable { get; private set; }

        public MenuNavigatePageBaseViewModel(
            HohoemaApp hohoemaApp, 
            PageManager pageManager, 
            Views.Service.AccountManagementDialogService accountManageDlgService
            )
		{
			PageManager = pageManager;
			HohoemaApp = hohoemaApp;
            AccountManagementDialogService = accountManageDlgService;

            // TV Mode
            if (Util.DeviceTypeHelper.IsXbox)
            {
                IsTVModeEnable = new ReactiveProperty<bool>(true);
            }
            else
            {
                IsTVModeEnable = HohoemaApp.UserSettings.AppearanceSettings.ObserveProperty(x => x.IsForceTVModeEnable)
                    .ToReactiveProperty();
            }

            IsOpenPane = new ReactiveProperty<bool>(false);

            MenuItems = new List<PageTypeSelectableItem>()
			{
                new PageTypeSelectableItem(HohoemaPageType.Portal             , OnMenuItemSelected, "ホーム", Symbol.Home),
                new PageTypeSelectableItem(HohoemaPageType.Search             , OnMenuItemSelected, "検索", Symbol.Find),
                new PageTypeSelectableItem(HohoemaPageType.RankingCategoryList, OnMenuItemSelected, "ランキング", Symbol.Flag),
                new PageTypeSelectableItem(HohoemaPageType.FollowManage       , OnMenuItemSelected, "フォロー", Symbol.OutlineStar),
                new PageTypeSelectableItem(HohoemaPageType.UserMylist         , OnMenuItemSelected, "マイリスト", Symbol.Bookmarks),
                new PageTypeSelectableItem(HohoemaPageType.History            , OnMenuItemSelected, "視聴履歴", Symbol.Clock),
            };

            SubMenuItems = new List<PageTypeSelectableItem>()
            {
                new PageTypeSelectableItem(HohoemaPageType.FeedGroupManage    , OnMenuItemSelected, "フィード", Symbol.List),
                new PageTypeSelectableItem(HohoemaPageType.Playlist           , OnMenuItemSelected, "プレイリスト", Symbol.Play),
                new PageTypeSelectableItem(HohoemaPageType.CacheManagement    , OnMenuItemSelected, "キャッシュ管理", Symbol.Download),
                new PageTypeSelectableItem(HohoemaPageType.Settings           , OnMenuItemSelected, "設定", Symbol.Setting),
                new PageTypeSelectableItem(HohoemaPageType.UserInfo           , OnAccountMenuItemSelected, "アカウント", Symbol.Account),
            };

            MainSelectedItem = new ReactiveProperty<PageTypeSelectableItem>(MenuItems[0]);
            SubSelectedItem = new ReactiveProperty<PageTypeSelectableItem>();

            Observable.Merge(
                MainSelectedItem, 
                SubSelectedItem
                )
                .Where(x => x != null)
                .Subscribe(x => x.SelectedAction(x.Source));
            
            PageManager.ObserveProperty(x => x.CurrentPageType)
				.Subscribe(pageType => 
				{
//                    IsOpenPane.Value = false;

                    bool isMenuItemOpened = false;
                    foreach (var item in MenuItems)
                    {
                        if (item.Source == pageType)
                        {
                            MainSelectedItem.Value = item;
                            SubSelectedItem.Value = null;
                            isMenuItemOpened = true;
                            break;
                        }
                    }

                    foreach (var item in SubMenuItems)
                    {
                        if (item.Source == pageType)
                        {
                            SubSelectedItem.Value = item;
                            MainSelectedItem.Value = null;
                            isMenuItemOpened = true;
                            break;
                        }
                    }

                    if (!isMenuItemOpened)
                    {
                        MainSelectedItem.Value = null;
                        SubSelectedItem.Value = null;
                    }
                });

            IsSubMenuItemPage = PageManager.ObserveProperty(x => x.CurrentPageType)
                .Select(x => SubMenuItems.Any(y => y.Source == x))
                .ToReactiveProperty();



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

			NowNavigating = PageManager.ObserveProperty(x => x.PageNavigating)
				.ToReactiveProperty();


			PageManager.StartWork += PageManager_StartWork;
			PageManager.ProgressWork += PageManager_ProgressWork;
			PageManager.CompleteWork += PageManager_CompleteWork;
			PageManager.CancelWork += PageManager_CancelWork;



			var updater = HohoemaApp.BackgroundUpdater;

			var bgUpdateStartedObserver = Observable.FromEventPattern<BackgroundUpdateScheduleHandler>(
				handler => updater.BackgroundUpdateStartedEvent += handler,
				handler => updater.BackgroundUpdateStartedEvent -= handler
				);

			var bgUpdateCompletedObserver = Observable.FromEventPattern<BackgroundUpdateScheduleHandler>(
				handler => updater.BackgroundUpdateCompletedEvent += handler,
				handler => updater.BackgroundUpdateCompletedEvent -= handler
				);
				

			var bgUpdateCanceledObserver = Observable.FromEventPattern<BackgroundUpdateScheduleHandler>(
				handler => updater.BackgroundUpdateCanceledEvent += handler,
				handler => updater.BackgroundUpdateCanceledEvent -= handler
				);

			BGUpdateText = new ReactiveProperty<string>();
			HasBGUpdateText = BGUpdateText.Select(x => !string.IsNullOrEmpty(x))
				.ToReactiveProperty();
			bgUpdateStartedObserver.Subscribe(x =>
			{
				if (!string.IsNullOrEmpty(x.EventArgs.Label))
				{
					BGUpdateText.Value = $"{x.EventArgs.Label} を処理中...";
				}
				else
				{
					BGUpdateText.Value = $"{x.EventArgs.Id} を処理中...";
				}
			});


            Observable.Merge(
                bgUpdateCompletedObserver,
                bgUpdateCanceledObserver
                )
                .Throttle(TimeSpan.FromSeconds(2))
                .Subscribe(x => 
				{
					BGUpdateText.Value = null;
				});

            HohoemaApp.ObserveProperty(x => x.IsLoggedIn)
                .Subscribe(x => IsLoggedIn = x);

            HohoemaApp.ObserveProperty(x => x.LoginUserName)
                .Subscribe(x =>
                {
                    UserName = x;
                    UserMail = HohoemaApp.GetPrimaryAccountId();
                });

            HohoemaApp.ObserveProperty(x => x.UserIconUrl)
                .Subscribe(x => UserIconUrl = x);



            // 検索
            SearchKeyword = new ReactiveProperty<string>("");
            SearchTarget = new ReactiveProperty<Models.SearchTarget>(Models.SearchTarget.Keyword);

            SearchCommand = SearchKeyword
                .Select(x => !string.IsNullOrWhiteSpace(x))
                .ToReactiveCommand();
            SearchCommand.Subscribe(_ => 
            {
                ISearchPagePayloadContent searchContent =
                    SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Value, SearchKeyword.Value);
                PageManager.Search(searchContent);

                IsMobileNowSearching = false;
            });
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

        internal void OnAccountMenuItemSelected(HohoemaPageType pageType)
        {
            if (pageType != PageManager.CurrentPageType)
            {
                PageManager.OpenPage(HohoemaPageType.UserInfo, HohoemaApp.LoginUserId.ToString());
            }
        }


        private DelegateCommand _TogglePaneOpenCommand;
        public DelegateCommand TogglePaneOpenCommand
        {
            get
            {
                return _TogglePaneOpenCommand
                    ?? (_TogglePaneOpenCommand = new DelegateCommand(() => 
                    {
                        IsOpenPane.Value = !IsOpenPane.Value;
                    }));
            }
        }

        private DelegateCommand _OpenAccountInfoCommand;
        public DelegateCommand OpenAccountInfoCommand
        {
            get
            {
                return _OpenAccountInfoCommand
                    ?? (_OpenAccountInfoCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.UserInfo, HohoemaApp.LoginUserId.ToString());
                    }));
            }
        }


        private DelegateCommand<PageTypeSelectableItem> _ItemSelectedCommand;
        public DelegateCommand<PageTypeSelectableItem> ItemSelectedCommand
        {
            get
            {
                return _ItemSelectedCommand
                    ?? (_ItemSelectedCommand = new DelegateCommand<PageTypeSelectableItem>((item) =>
                    {
                        if (item != null)
                        {
                            item.SelectedAction(item.Source);
                        }
                    }));
            }
        }


        public List<PageTypeSelectableItem> MenuItems { get; private set; }

        public List<PageTypeSelectableItem> SubMenuItems { get; private set; }

        public ReactiveProperty<PageTypeSelectableItem> MainSelectedItem { get; private set; }
        public ReactiveProperty<PageTypeSelectableItem> SubSelectedItem { get; private set; }

        public ReactiveProperty<bool> IsVisibleMenu { get; private set; }

		public ReactiveProperty<bool> NowNavigating { get; private set; }

        public ReactiveProperty<bool> IsOpenPane { get; private set; }

        public ReactiveProperty<bool> IsForceXboxDisplayMode { get; private set; }
        public ReactiveProperty<bool> IsSubMenuItemPage { get; private set; }



        private string _TitleText;
		public string TitleText
		{
			get { return _TitleText; }
			set { SetProperty(ref _TitleText, value); }
		}

        private string _UserIconUrl;
        public string UserIconUrl
        {
            get { return _UserIconUrl; }
            set { SetProperty(ref _UserIconUrl, value); }
        }

        private string _UserName;
        public string UserName
        {
            get { return _UserName; }
            set { SetProperty(ref _UserName, value); }
        }

        private string _UserMail;
        public string UserMail
        {
            get { return _UserMail; }
            set { SetProperty(ref _UserMail, value); }
        }


        private bool _IsLoggedIn;
        public bool IsLoggedIn
        {
            get { return _IsLoggedIn; }
            set { SetProperty(ref _IsLoggedIn, value); }
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


		public ReactiveProperty<bool> HasBGUpdateText { get; private set; }
		public ReactiveProperty<string> BGUpdateText { get; private set; }




        #region Search


        public static List<SearchTarget> SearchTargetItems { get; private set; } = new List<Models.SearchTarget>()
        {
            Models.SearchTarget.Keyword,
            Models.SearchTarget.Tag,
            Models.SearchTarget.Mylist,
            Models.SearchTarget.Community,
            Models.SearchTarget.Niconama,
        };

       

        public ReactiveProperty<string> SearchKeyword { get; private set; }
        public ReactiveProperty<SearchTarget> SearchTarget { get; private set; }

        public ReactiveCommand SearchCommand { get; private set; }

        private DelegateCommand _OpenSearchPageCommand;
        public DelegateCommand OpenSearchPageCommand
        {
            get
            {
                return _OpenSearchPageCommand
                    ?? (_OpenSearchPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.Search);

                        IsMobileNowSearching = false;
                    }));
            }
        }

        private bool _IsMobileNowSearching;
        public bool IsMobileNowSearching
        {
            get { return _IsMobileNowSearching; }
            set { SetProperty(ref _IsMobileNowSearching, value); }
        }

        private DelegateCommand _StartMobileSearchCommand;
        public DelegateCommand StartMobileSearchCommand
        {
            get
            {
                return _StartMobileSearchCommand
                    ?? (_StartMobileSearchCommand = new DelegateCommand(() =>
                    {
                        IsMobileNowSearching = !IsMobileNowSearching;
                    }));
            }
        }


        #endregion


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


        public bool HasChild { get; set; }
        public List<PageTypeSelectableItem> Children { get; set; }

    }



}

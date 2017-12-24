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
using Windows.UI.ViewManagement;
using NicoPlayerHohoema.Helpers;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel.Core;
using NicoPlayerHohoema.Models.Live;
using Windows.UI.Core;
using Prism.Windows.Mvvm;

namespace NicoPlayerHohoema.ViewModels
{
	public class MenuNavigatePageBaseViewModel : BindableBase
	{
		public PageManager PageManager { get; private set; }
		public HohoemaApp HohoemaApp { get; private set; }
        

        public ReactiveProperty<bool> IsTVModeEnable { get; private set; }
        public bool IsNeedFullScreenToggleHelp { get; private set; }

        public ReadOnlyReactiveProperty<HohoemaAppServiceLevel> ServiceLevel { get; private set; }

        public ReactiveProperty<bool> IsShowInAppNotification { get; private set; }



        public ReactiveProperty<bool> IsShowPlayer { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsShowPlayerInFill { get; private set; }

        public ReactiveProperty<bool> IsContentDisplayFloating { get; private set; }

        private Dictionary<string, object> viewModelState = new Dictionary<string, object>();

        /// <summary>
        /// Playerの小窓状態の変化を遅延させて伝播させます、
        /// 
        /// 遅延させている理由は、Player上のFlyoutを閉じる前にリサイズが発生するとFlyoutが
        /// ゴースト化（FlyoutのUIは表示されず閉じれないが、Visible状態と同じようなインタラクションだけは出来てしまう）
        /// してしまうためです。（タブレット端末で発生、PCだと発生確認されず）
        /// この問題を回避するためにFlyoutが閉じられた後にプレイヤーリサイズが走るように遅延を挟んでいます。
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsShowPlayerInFill_DelayedRead { get; private set; }


        public INavigationService NavigationService { get; private set; }


        public MenuNavigatePageBaseViewModel(
            HohoemaApp hohoemaApp,
            PageManager pageManager
            )
        {
            PageManager = pageManager;
            HohoemaApp = hohoemaApp;

            // TV Mode
            if (Helpers.DeviceTypeHelper.IsXbox)
            {
                IsTVModeEnable = new ReactiveProperty<bool>(true);
            }
            else
            {
                IsTVModeEnable = HohoemaApp.UserSettings
                    .AppearanceSettings.ObserveProperty(x => x.IsForceTVModeEnable)
                    .ToReactiveProperty();
            }

            ServiceLevel = HohoemaApp.ObserveProperty(x => x.ServiceStatus)
                .ToReadOnlyReactiveProperty();

            IsNeedFullScreenToggleHelp
                = ApplicationView.PreferredLaunchWindowingMode == ApplicationViewWindowingMode.FullScreen;

            IsOpenPane = new ReactiveProperty<bool>(false);

            MenuItems = new List<PageTypeSelectableItem>()
            {
                new PageTypeSelectableItem(HohoemaPageType.RankingCategoryList, OnMenuItemSelected, "ランキング", Symbol.Flag),
                new PageTypeSelectableItem(HohoemaPageType.Mylist             , OnWatchAfterMenuItemSelected, "あとで見る", Symbol.Play),
                new PageTypeSelectableItem(HohoemaPageType.UserMylist         , OnMenuItemSelected, "マイリスト", Symbol.Bookmarks),
                new PageTypeSelectableItem(HohoemaPageType.NicoRepo           , OnMenuItemSelected, "ニコレポ", Symbol.Favorite),
            };

            SubMenuItems = new List<PageTypeSelectableItem>()
            {
                new PageTypeSelectableItem(HohoemaPageType.FollowManage       , OnMenuItemSelected, "フォロー", Symbol.OutlineStar),
                new PageTypeSelectableItem(HohoemaPageType.FeedGroupManage    , OnMenuItemSelected, "フィード", Symbol.List),
                new PageTypeSelectableItem(HohoemaPageType.WatchHistory            , OnMenuItemSelected, "視聴履歴", Symbol.Clock),
                new PageTypeSelectableItem(HohoemaPageType.CacheManagement    , OnMenuItemSelected, "キャッシュ", Symbol.Download),
                new PageTypeSelectableItem(HohoemaPageType.Settings           , OnMenuItemSelected, "設定", Symbol.Setting),
                new PageTypeSelectableItem(HohoemaPageType.UserInfo           , OnAccountMenuItemSelected, "アカウント", Symbol.Account),
            };

            AllMenuItems = MenuItems.Concat(SubMenuItems).ToList();

            MainSelectedItem = new ReactiveProperty<PageTypeSelectableItem>(MenuItems[0], ReactivePropertyMode.DistinctUntilChanged);
            SubSelectedItem = new ReactiveProperty<PageTypeSelectableItem>(null, ReactivePropertyMode.DistinctUntilChanged);
            /*
            Observable.Merge(
                MainSelectedItem, 
                SubSelectedItem
                )
                .Where(x => x != null)
                .Subscribe(x => x.SelectedAction(x.Source));
            */
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
                    return !PageManager.IsHiddenMenuPage(x);
                })
                .ToReactiveProperty(false);

            NowNavigating = PageManager.ObserveProperty(x => x.PageNavigating)
                .ToReactiveProperty();


            PageManager.StartWork += PageManager_StartWork;
            PageManager.ProgressWork += PageManager_ProgressWork;
            PageManager.CompleteWork += PageManager_CompleteWork;
            PageManager.CancelWork += PageManager_CancelWork;

            HohoemaApp.ObserveProperty(x => x.IsLoggedIn)
                .Subscribe(x => IsLoggedIn = x);

            HohoemaApp.ObserveProperty(x => x.LoginUserName)
                .Subscribe(x =>
                {
                    UserName = x;
                    UserMail = AccountManager.GetPrimaryAccountId();
                });

            HohoemaApp.ObserveProperty(x => x.UserIconUrl)
                .Subscribe(x => UserIconUrl = x);



            // 検索
            SearchKeyword = new ReactiveProperty<string>("");

            SearchCommand = SearchKeyword
                .Select(x => !string.IsNullOrWhiteSpace(x))
                .ToReactiveCommand();
            SearchCommand
                .Delay(TimeSpan.FromSeconds(0.15))
                .Subscribe(_ =>
            {
                /*
                ISearchPagePayloadContent searchContent =
                    SearchPagePayloadContentHelper.CreateDefault(SearchTarget.Value, SearchKeyword.Value);
                PageManager.Search();
                */
                PageManager.OpenPage(HohoemaPageType.SearchSummary, SearchKeyword.Value);
            });

            var searchKeywordChanged = SearchKeyword
                .Throttle(TimeSpan.FromMilliseconds(500));

            SearchSuggestionWords = searchKeywordChanged
                .SelectMany(async word =>
                {
                    if (string.IsNullOrWhiteSpace(word))
                    {
                        // 検索履歴を表示
                        return Models.Db.SearchHistoryDb.GetHistoryItems()
                            .OrderByDescending(x => x.LastUpdated)
                            .Take(10)
                            .Select(x => x.Keyword);
                    }
                    else if (HohoemaApp.NiconicoContext != null)
                    {
                        var res = await HohoemaApp.ContentProvider.GetSearchSuggestKeyword(word);
                        return res.Candidates.AsEnumerable();
                    }
                    else { return Enumerable.Empty<string>(); }
                })
                .SelectMany(x => x)
                .ToReadOnlyReactiveCollection(searchKeywordChanged.ToUnit());
                

            // InAppNotification
            IsShowInAppNotification = new ReactiveProperty<bool>(true);



            IsShowPlayerInFill = HohoemaApp.Playlist
                .ObserveProperty(x => x.IsPlayerFloatingModeEnable)
                .Select(x => !x)
                .ToReadOnlyReactiveProperty();

            IsShowPlayerInFill_DelayedRead = IsShowPlayerInFill
                .Delay(TimeSpan.FromMilliseconds(300))
                .ToReadOnlyReactiveProperty();


            IsShowPlayer = HohoemaApp.Playlist.ObserveProperty(x => x.IsDisplayMainViewPlayer)
                .ToReactiveProperty(mode: ReactivePropertyMode.DistinctUntilChanged);

            IsContentDisplayFloating = Observable.CombineLatest(
                IsShowPlayerInFill.Select(x => !x),
                IsShowPlayer
                )
                .Select(x => x.All(y => y))
                .ToReactiveProperty();


            HohoemaApp.Playlist.OpenPlaylistItem += HohoemaPlaylist_OpenPlaylistItem;

            IsShowPlayer
                .Where(x => !x)
                .Subscribe(x =>
                {
                    ClosePlayer();
                });

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                Observable.Merge(
                    IsShowPlayer.Where(x => !x),
                    IsContentDisplayFloating.Where(x => x)
                    )
                    .Subscribe(async x =>
                    {
                        var view = ApplicationView.GetForCurrentView();
                        if (view.IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                        {
                            var result = await view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                            if (result)
                            {
                                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
                                view.TitleBar.ButtonBackgroundColor = null;
                                view.TitleBar.ButtonInactiveBackgroundColor = null;

                            }
                        }
                    });
            }
        }

        internal void SetNavigationService(INavigationService ns)
        {
            NavigationService = ns;
        }



        private async void HohoemaPlaylist_OpenPlaylistItem(IPlayableList playlist, PlaylistItem item)
        {
            await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ShowPlayer(item);
            });
        }

        private bool ShowPlayer(PlaylistItem item)
        {
            string pageType = null;
            string parameter = null;
            switch (item.Type)
            {
                case PlaylistItemType.Video:
                    pageType = nameof(Views.VideoPlayerPage);
                    parameter = new VideoPlayPayload()
                    {
                        VideoId = item.ContentId
                    }
                    .ToParameterString();
                    break;
                case PlaylistItemType.Live:
                    pageType = nameof(Views.LivePlayerPage);
                    parameter = new LiveVideoPagePayload(item.ContentId)
                    {
                        LiveTitle = item.Title,
                    }
                    .ToParameterString();
                    break;
                default:
                    break;
            }

            return NavigationService.Navigate(pageType, parameter);
        }

        private void ClosePlayer()
        {
            NavigationService.Navigate("Blank", null);
        }

        private DelegateCommand _PlayerFillDisplayCommand;
        public DelegateCommand TogglePlayerFloatDisplayCommand
        {
            get
            {
                return _PlayerFillDisplayCommand
                    ?? (_PlayerFillDisplayCommand = new DelegateCommand(() =>
                    {
                        // プレイヤー表示中だけ切り替えを受け付け
                        if (!HohoemaApp.Playlist.IsDisplayMainViewPlayer) { return; }

                        // メインウィンドウでの表示状態を「ウィンドウ全体」⇔「小窓表示」と切り替える
                        if (HohoemaApp.Playlist.PlayerDisplayType == PlayerDisplayType.PrimaryView)
                        {
                            HohoemaApp.Playlist.PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
                        }
                        else if (HohoemaApp.Playlist.PlayerDisplayType == PlayerDisplayType.PrimaryWithSmall)
                        {
                            HohoemaApp.Playlist.PlayerDisplayType = PlayerDisplayType.PrimaryView;
                        }
                    }));
            }
        }


        private DelegateCommand _ClosePlayerCommand;
        public DelegateCommand ClosePlayerCommand
        {
            get
            {
                return _ClosePlayerCommand
                    ?? (_ClosePlayerCommand = new DelegateCommand(() =>
                    {
                        HohoemaApp.Playlist.IsDisplayMainViewPlayer = false;
                    }));
            }
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

        internal void OnWatchAfterMenuItemSelected(HohoemaPageType pageType)
        {
            PageManager.OpenPage(HohoemaPageType.Mylist,
                new MylistPagePayload()
                {
                    Id = HohoemaPlaylist.WatchAfterPlaylistId,
                    Origin = PlaylistOrigin.Local
                }.ToParameterString()
            );
        }

        internal async void OnAccountMenuItemSelected(HohoemaPageType pageType)
        {
            await HohoemaApp.CheckSignedInStatus();

            if (pageType != PageManager.CurrentPageType)
            {
                if (ServiceLevel.Value == HohoemaAppServiceLevel.LoggedIn)
                {
                    PageManager.OpenPage(HohoemaPageType.UserInfo, HohoemaApp.LoginUserId.ToString());
                }
                else
                {
                    PageManager.OpenPage(HohoemaPageType.Login);
                }
            }
        }


        private DelegateCommand _NavigationBackCommand;
        public DelegateCommand NavigationBackCommand
        {
            get
            {
                return _NavigationBackCommand
                    ?? (_NavigationBackCommand = new DelegateCommand(() =>
                    {
                        if (PageManager.NavigationService.CanGoBack())
                        {
                            PageManager.NavigationService.GoBack();
                        }
                    } 
                    ));
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
                    ?? (_OpenAccountInfoCommand = new DelegateCommand(async () =>
                    {
                        await HohoemaApp.CheckSignedInStatus();

                        if (ServiceLevel.Value == HohoemaAppServiceLevel.LoggedIn)
                        {
                            PageManager.OpenPage(HohoemaPageType.UserInfo, HohoemaApp.LoginUserId.ToString());
                        }
                        else
                        {
                            PageManager.OpenPage(HohoemaPageType.Login);
                        }
                        
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


        private DelegateCommand _ToggleFullScreenCommand;
        public DelegateCommand ToggleFullScreenCommand
        {
            get
            {
                return _ToggleFullScreenCommand
                    ?? (_ToggleFullScreenCommand = new DelegateCommand(() =>
                    {
                        var appView = ApplicationView.GetForCurrentView();

                        if (!appView.IsFullScreenMode)
                        {
                            appView.TryEnterFullScreenMode();
                        }
                        else
                        {
                            appView.ExitFullScreenMode();
                        }
                    }));
            }
        }

        DelegateCommand<PageTypeSelectableItem> _MenuItemSelectedCommand;
        public DelegateCommand<PageTypeSelectableItem> MenuItemSelectedCommand
        {
            get
            {
                return _MenuItemSelectedCommand
                    ?? (_MenuItemSelectedCommand = new DelegateCommand<PageTypeSelectableItem>((item) => 
                    {
                        item.SelectedAction(item.Source);
                    }));
            }
        }

        public List<PageTypeSelectableItem> MenuItems { get; private set; }

        public List<PageTypeSelectableItem> SubMenuItems { get; private set; }
        public List<PageTypeSelectableItem> AllMenuItems { get; private set; }

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


        public ReactiveProperty<string> SearchKeyword { get; private set; }

        public ReadOnlyReactiveCollection<string> SearchSuggestionWords { get; }

        public ReactiveCommand SearchCommand { get; private set; }

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

    public class EmptyContentViewModel : ViewModelBase
    {

    }



}

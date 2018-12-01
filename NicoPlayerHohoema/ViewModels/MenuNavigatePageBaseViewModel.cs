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
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Diagnostics;

namespace NicoPlayerHohoema.ViewModels
{
	public class MenuNavigatePageBaseViewModel : BindableBase
	{
		public PageManager PageManager { get; private set; }
		public HohoemaApp HohoemaApp { get; private set; }
        public Models.Niconico.Live.NicoLiveSubscriber NicoLiveSubscriber { get; private set; }

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


        public ReactivePropertySlim<bool> CanGoBackNavigation { get; private set; }
        public ReactiveCommand GoBackNavigationCommand { get; private set; }



        public INavigationService NavigationService { get; private set; }

        public MenuNavigatePageBaseViewModel(
            HohoemaApp hohoemaApp,
            PageManager pageManager,
            Models.Niconico.Live.NicoLiveSubscriber nicoLiveSubscriber
            )
        {
            PageManager = pageManager;
            HohoemaApp = hohoemaApp;
            NicoLiveSubscriber = nicoLiveSubscriber;

            HohoemaApp.OnSignin += HohoemaApp_OnSignin;
            HohoemaApp.OnSignout += HohoemaApp_OnSignout;

            CurrentMenuType = new ReactiveProperty<ViewModelBase>();
            VideoMenu = new VideoMenuSubPageContent(HohoemaApp, HohoemaApp.UserMylistManager, HohoemaApp.Playlist);
            LiveMenu = new LiveMenuSubPageContent(HohoemaApp, NicoLiveSubscriber);

            // Back Navigation
            CanGoBackNavigation = new ReactivePropertySlim<bool>();
            GoBackNavigationCommand = CanGoBackNavigation
                .ToReactiveCommand();

            GoBackNavigationCommand.Subscribe(_ => 
            {
                PageManager.NavigationService.GoBack();
            });

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

            MainSelectedItem = new ReactiveProperty<HohoemaListingPageItemBase>(null, ReactivePropertyMode.DistinctUntilChanged);


            PinItems = HohoemaApp.UserSettings.PinSettings.Pins;
                
            ResetMenuItems();

            PinItems.CollectionChangedAsObservable()
                .Subscribe(async _ => 
                {
                    await HohoemaApp.UserSettings.PinSettings.Save();
                });

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
                        if ((item as MenuItemViewModel)?.PageType == pageType)
                        {
                            MainSelectedItem.Value = item;
                            isMenuItemOpened = true;
                            break;
                        }
                    }

                   
                    if (!isMenuItemOpened)
                    {
                        MainSelectedItem.Value = null;
                    }

                    if (Helpers.DeviceTypeHelper.IsXbox || HohoemaApp.UserSettings.AppearanceSettings.IsForceTVModeEnable)
                    {
                        IsOpenPane.Value = false;
                    }
                });


            PageManager.ObserveProperty(x => x.PageTitle)
                .Subscribe(x =>
                {
                    TitleText = x;
                    AddPinToCurrentPageCommand.RaiseCanExecuteChanged();
                });

            PageManager.ObserveProperty(x => x.CurrentPageType)
                .Subscribe(_ => UpdateCanGoBackNavigation());



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
                });

            HohoemaApp.ObserveProperty(x => x.UserIconUrl)
                .Subscribe(x => UserIconUrl = x);



            // 検索
            SearchKeyword = new ReactiveProperty<string>("");

            SearchCommand = new ReactiveCommand();
            SearchCommand
                .Subscribe(async _ =>
            {
                await Task.Delay(50);
                var keyword = SearchKeyword.Value;

                if (string.IsNullOrWhiteSpace(keyword)) { return; }

                SearchTarget? searchType = CurrentMenuType.Value is LiveMenuSubPageContent ? SearchTarget.Niconama : SearchTarget.Keyword;
                var searched = Database.SearchHistoryDb.LastSearchedTarget(keyword);
                if (searched != null)
                {
                    searchType = searched;
                }

                PageManager.Search(SearchPagePayloadContentHelper.CreateDefault(searchType.Value, keyword));

                ResetSearchHistoryItems();
            });

            SearchSuggestionWords = new ObservableCollection<string>();
            
             

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
                        }
                    });
            }


            // 検索履歴アイテムを初期化
            ResetSearchHistoryItems();
        }

        private void HohoemaApp_OnSignout()
        {
            ResetMenuItems();
        }

        private void HohoemaApp_OnSignin()
        {
            ResetMenuItems();
        }

        /// <summary>
        /// メニューアイテムの更新
        /// メニュー初期化時、およびピン留めの内容を更新した際に呼び出します
        /// </summary>
        private void ResetMenuItems()
        {
            MainSelectedItem.Value = null;

            MenuItems.Clear();
            if (HohoemaApp.IsLoggedIn)
            {
                MenuItems.Add(new MenuItemViewModel("ニコレポ", HohoemaPageType.NicoRepo));
                MenuItems.Add(new MenuItemViewModel("フォロー", HohoemaPageType.FollowManage));
                MenuItems.Add(new MenuItemViewModel("マイリスト", HohoemaPageType.UserMylist));
            }
            else
            {
                MenuItems.Add(new MenuItemViewModel("マイリスト", HohoemaPageType.UserMylist));
            }
        }


        private void ResetSearchHistoryItems()
        {
            var histries = Database.SearchHistoryDb.GetAll()
                            .OrderByDescending(x => x.LastUpdated)
                            .Take(20)
                            .Select(x => x.Keyword)
                            .Distinct();

            SearchSuggestionWords.Clear();
            foreach (var history in histries)
            {
                SearchSuggestionWords.Add(history);
            }
        }


        internal void SetNavigationService(INavigationService ns)
        {
            NavigationService = ns;
        }


        private async void UpdateCanGoBackNavigation()
        {
            await Task.Delay(50);
            CanGoBackNavigation.Value = PageManager.NavigationService.CanGoBack();
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

            if (NavigationService.Navigate(pageType, parameter))
            {
                ApplicationView currentView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                if (Helpers.DeviceTypeHelper.IsMobile)
                {
                    currentView.TryEnterFullScreenMode();
                }
                else if (Helpers.DeviceTypeHelper.IsDesktop && !HohoemaApp.Playlist.IsPlayerFloatingModeEnable)
                {
                    // 
                    if (currentView.AdjacentToLeftDisplayEdge && currentView.AdjacentToRightDisplayEdge)
                    {
                        currentView.TryEnterFullScreenMode();
                    }
                }

                return true;
            }
            else { return false; }
        }

        private async void ClosePlayer()
        {
            NavigationService.Navigate("Blank", null);

            if (ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen)
            {
                ApplicationView currentView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
                currentView.ExitFullScreenMode();
            }

            await HohoemaApp.CacheManager.ResumeCacheDownload();
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


        private DelegateCommand _AddPinToCurrentPageCommand;
        public DelegateCommand AddPinToCurrentPageCommand
        {
            get
            {
                return _AddPinToCurrentPageCommand
                    ?? (_AddPinToCurrentPageCommand = new DelegateCommand(() =>
                    {
                        var pinSettings = HohoemaApp.UserSettings.PinSettings;
                        string pageParameter = PageManager.PageNavigationParameter as string;
                        if (pageParameter == null)
                        {
                            System.Diagnostics.Debug.WriteLine("can not Pin this page : " + PageManager.PageTitle);
                            return;
                        }

                        if (pinSettings.Pins.Any(x => x.PageType == PageManager.CurrentPageType && x.Parameter == pageParameter))
                        {
                            System.Diagnostics.Debug.WriteLine("Pin already exist : " + PageManager.PageTitle);
                            return;
                        }

                        pinSettings.Pins.Insert(0, new HohoemaPin()
                        {
                            Label = PageManager.PageTitle,
                            PageType = PageManager.CurrentPageType,
                            Parameter = pageParameter
                        });

                        pinSettings.Save().ConfigureAwait(false);

                        System.Diagnostics.Debug.WriteLine("Pin Added : " + PageManager.PageTitle);
                    }, 
                    () => 
                    {
                        return PageManager.PageNavigationParameter is string;
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

                        if (ServiceLevel.Value == HohoemaAppServiceLevel.LoggedIn || ServiceLevel.Value == HohoemaAppServiceLevel.LoggedInWithPremium)
                        {
                            PageManager.OpenPage(HohoemaPageType.UserInfo, HohoemaApp.LoginUserId.ToString());
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

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; } = new ObservableCollection<HohoemaListingPageItemBase>();
        public ObservableCollection<HohoemaPin> PinItems { get; private set; }

        public ReactiveProperty<HohoemaListingPageItemBase> MainSelectedItem { get; private set; }
        
        public ReactiveProperty<bool> IsVisibleMenu { get; private set; }

		public ReactiveProperty<bool> NowNavigating { get; private set; }

        public ReactiveProperty<bool> IsOpenPane { get; private set; }

        public ReactiveProperty<bool> IsForceXboxDisplayMode { get; private set; }


        public ViewModelBase VideoMenu { get; private set; }
        public ViewModelBase LiveMenu { get; private set; }
        public ReactiveProperty<ViewModelBase> CurrentMenuType { get; }

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

        public ObservableCollection<string> SearchSuggestionWords { get; }

        public ReactiveCommand SearchCommand { get; private set; }

        #endregion


    }


    public enum NiconicoServiceType
    {
        Video,
        Live
    }


    public class MenuSubItemViewModel : HohoemaListingPageItemBase
    {
        public MenuSubItemViewModel(string label)
        {
            Label = label;
        }

        public List<MenuItemViewModel> SubItems { get; set; }
    }

    public class MenuItemViewModel : HohoemaListingPageItemBase
    {
		public MenuItemViewModel(string label, HohoemaPageType pageType, string paramaeter = null)
		{
			Label = label;
            PageType = pageType;
            Parameter = paramaeter;

            IsSelected = false;
		}

        public HohoemaPageType PageType { get; set; }
        public string Parameter { get; set; }
    }

    public class EmptyContentViewModel : ViewModelBase
    {

    }




    public class VideoMenuSubPageContent : ViewModelBase, IDisposable
    {

        private HohoemaApp _HohoemaApp;
        private Models.UserMylistManager _MylistManager;
        private Models.HohoemaPlaylist _Playlist;

        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> LocalMylists { get; private set; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> Mylists { get; private set; }

        public VideoMenuSubPageContent(HohoemaApp hohoemaApp, Models.UserMylistManager mylistManager, Models.HohoemaPlaylist playlist)
        {
            _HohoemaApp = hohoemaApp;
            _Playlist = playlist;
            _MylistManager = mylistManager;
            MenuItems = new ObservableCollection<HohoemaListingPageItemBase>();
            hohoemaApp.OnSignin += HohoemaApp_OnSignin;
            hohoemaApp.OnSignout += HohoemaApp_OnSignout;

            ResetMenuItems();

            LocalMylists = _Playlist.Playlists
               .ToReadOnlyReactiveCollection(x =>
               new MenuItemViewModel(x.Label, HohoemaPageType.Mylist, new Models.MylistPagePayload(x).ToParameterString()) as HohoemaListingPageItemBase
               )
               .AddTo(_CompositeDisposable);
            Mylists = _MylistManager.UserMylists
                .ToReadOnlyReactiveCollection(x =>
                new MenuItemViewModel(x.Label, HohoemaPageType.Mylist, new Models.MylistPagePayload(x).ToParameterString()) as HohoemaListingPageItemBase
                )
                .AddTo(_CompositeDisposable);
        }


        private void ResetMenuItems()
        {
            MenuItems.Clear();
            if (_HohoemaApp.IsLoggedIn)
            {
                MenuItems.Add(new MenuItemViewModel("ランキング", HohoemaPageType.RankingCategoryList));
                MenuItems.Add(new MenuItemViewModel("ニコレポ", HohoemaPageType.NicoRepo));
                MenuItems.Add(new MenuItemViewModel("新着", HohoemaPageType.Subscription));
                MenuItems.Add(new MenuItemViewModel("フォロー", HohoemaPageType.FollowManage));
                MenuItems.Add(new MenuItemViewModel("視聴履歴", HohoemaPageType.WatchHistory));
                MenuItems.Add(new MenuItemViewModel("キャッシュ", HohoemaPageType.CacheManagement));
                MenuItems.Add(new MenuItemViewModel("オススメ", HohoemaPageType.Recommend));
                MenuItems.Add(new MenuItemViewModel("あとで見る", HohoemaPageType.Mylist, new MylistPagePayload(HohoemaPlaylist.WatchAfterPlaylistId).ToParameterString()));
            }
            else
            {
                MenuItems.Add(new MenuItemViewModel("ランキング", HohoemaPageType.RankingCategoryList));
                MenuItems.Add(new MenuItemViewModel("新着", HohoemaPageType.Subscription));
                //                MenuItems.Add(new MenuItemViewModel("視聴履歴", HohoemaPageType.WatchHistory));
                MenuItems.Add(new MenuItemViewModel("キャッシュ", HohoemaPageType.CacheManagement));
                MenuItems.Add(new MenuItemViewModel("あとで見る", HohoemaPageType.Mylist, new MylistPagePayload(HohoemaPlaylist.WatchAfterPlaylistId).ToParameterString()));
            }

            RaisePropertyChanged(nameof(MenuItems));
        }

        private void HohoemaApp_OnSignin()
        {
            ResetMenuItems();

            System.Diagnostics.Debug.WriteLine("サインイン：メニューをリセットしました");
        }

        private void HohoemaApp_OnSignout()
        {
            ResetMenuItems();

            System.Diagnostics.Debug.WriteLine("サインアウト：メニューをリセットしました");
        }


        public void Dispose()
        {
            _CompositeDisposable.Dispose();

            _HohoemaApp.OnSignin -= HohoemaApp_OnSignin;
            _HohoemaApp.OnSignout -= HohoemaApp_OnSignout;
        }

    }

    public class LiveMenuSubPageContent : ViewModelBase
    {
        private Models.Niconico.Live.NicoLiveSubscriber _LiveSubscriber;

        public List<HohoemaListingPageItemBase> MenuItems { get; private set; }

        public ReadOnlyReactiveCollection<OnAirStream> OnAirStreams { get; }
        public ReadOnlyReactiveCollection<OnAirStream> ReservedStreams { get; }

        HohoemaApp _HohoemaApp;

        public LiveMenuSubPageContent(HohoemaApp hohoemaApp, Models.Niconico.Live.NicoLiveSubscriber nicoLiveSubscriber)
        {
            _HohoemaApp = hohoemaApp;
            _LiveSubscriber = nicoLiveSubscriber;

            UpdateOnAirStreamsCommand = new AsyncReactiveCommand();
            
            UpdateOnAirStreamsCommand.Subscribe(async _ => 
            {
                await _LiveSubscriber.UpdateOnAirStreams();
            });

            OnAirStreams = _LiveSubscriber.OnAirStreams.ToReadOnlyReactiveCollection(x => 
            new OnAirStream()
            {
                BroadcasterId = x.Video.UserId.ToString(),
                Id = x.Video.Id,
                Label = x.Video.Title,
                Thumbnail = x.Community?.ThumbnailSmall,
                CommunityName  = x.Community.Name,
                StartAt = x.Video.StartTime.Value
            }
            );

            ReservedStreams = _LiveSubscriber.ReservedStreams.ToReadOnlyReactiveCollection(x =>
            new OnAirStream()
            {
                BroadcasterId = x.Video.UserId.ToString(),
                Id = x.Video.Id,
                Label = x.Video.Title,
                Thumbnail = x.Community?.ThumbnailSmall,
                CommunityName = x.Community.Name,
                StartAt = x.Video.StartTime.Value
            }
            );

            MenuItems = new List<HohoemaListingPageItemBase>();
            
            _HohoemaApp.ObserveProperty(x => x.IsLoggedIn)
                .Subscribe(isLoggedIn => 
                {
                    MenuItems.Clear();
                    if (isLoggedIn)
                    {
                        MenuItems.Add(new MenuItemViewModel("タイムシフト", HohoemaPageType.Timeshift));
                        MenuItems.Add(new MenuItemViewModel("ニコレポ", HohoemaPageType.NicoRepo));
                        MenuItems.Add(new MenuItemViewModel("フォロー", HohoemaPageType.FollowManage));
                    }

                    RaisePropertyChanged(nameof(MenuItems));
                });

            //            MenuItems.Add(new MenuItemViewModel("予約", HohoemaPageType.NicoRepo));
        }

        public AsyncReactiveCommand UpdateOnAirStreamsCommand { get; }
    }

    public class OnAirStream : Interfaces.ILiveContent
    {
        public string BroadcasterId { get; internal set; }
        public string Id { get; internal set; }
        public string Label { get; internal set; }

        public string CommunityName { get; internal set; }
        public string Thumbnail { get; internal set; }

        public DateTimeOffset StartAt { get; internal set; }
    }
}

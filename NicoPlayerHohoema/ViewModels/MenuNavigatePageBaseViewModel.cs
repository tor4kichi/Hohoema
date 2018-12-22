using NicoPlayerHohoema.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.UI.ViewManagement;
using Windows.Foundation.Metadata;
using NicoPlayerHohoema.Models.Live;
using Windows.UI.Core;
using Prism.Windows.Mvvm;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Services;
using System.Reactive.Concurrency;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Cache;
using System.Windows.Input;
using NicoPlayerHohoema.Services.Helpers;
using Microsoft.Practices.Unity;
using Mntone.Nico2.Live;

namespace NicoPlayerHohoema.ViewModels
{
    public class MenuNavigatePageBaseViewModel : BindableBase
    {
        public MenuNavigatePageBaseViewModel(
            IUnityContainer container,
            IScheduler scheduler,
            AppearanceSettings appearanceSettings,
            PinSettings pinSettings,
            NiconicoSession niconicoSession,
            LocalMylistManager localMylistManager,
            UserMylistManager userMylistManager,
            VideoCacheManager videoCacheManager, 
            NicoLiveSubscriber nicoLiveSubscriber,
            PageManager pageManager,
            Commands.LoginToNiconicoCommand loginToNiconicoCommand,
            Commands.LogoutFromNiconicoCommand logoutFromNiconicoCommand
            )
        {
            PageManager = pageManager;
            LoginToNiconicoCommand = loginToNiconicoCommand;
            LogoutFromNiconicoCommand = logoutFromNiconicoCommand;
            Container = container;
            Scheduler = scheduler;
            AppearanceSettings = appearanceSettings;
            PinSettings = pinSettings;
            NiconicoSession = niconicoSession;
            LocalMylistManager = localMylistManager;
            UserMylistManager = userMylistManager;
            VideoCacheManager = videoCacheManager;
            NicoLiveSubscriber = nicoLiveSubscriber;

            NiconicoSession.LogIn += (sender, e) => ResetMenuItems();
            NiconicoSession.LogOut += (sender, e) => ResetMenuItems();

            CurrentMenuType = new ReactiveProperty<ViewModelBase>();
            VideoMenu = App.Current.Container.Resolve<VideoMenuSubPageContent>();
            LiveMenu = App.Current.Container.Resolve<LiveMenuSubPageContent>();

            // Back Navigation
            CanGoBackNavigation = new ReactivePropertySlim<bool>();
            GoBackNavigationCommand = CanGoBackNavigation
                .ToReactiveCommand();

            GoBackNavigationCommand.Subscribe(_ =>
            {
                PageManager.NavigationService.GoBack();
            });

            // TV Mode
            if (Services.Helpers.DeviceTypeHelper.IsXbox)
            {
                IsTVModeEnable = new ReactiveProperty<bool>(true);
            }
            else
            {
                IsTVModeEnable = AppearanceSettings.ObserveProperty(x => x.IsForceTVModeEnable)
                    .ToReactiveProperty();
            }


            ServiceLevel = NiconicoSession.ObserveProperty(x => x.ServiceStatus)
                .ToReadOnlyReactiveProperty();

            IsNeedFullScreenToggleHelp
                = ApplicationView.PreferredLaunchWindowingMode == ApplicationViewWindowingMode.FullScreen;

            IsOpenPane = new ReactiveProperty<bool>(false);

            MainSelectedItem = new ReactiveProperty<HohoemaListingPageItemBase>(null, ReactivePropertyMode.DistinctUntilChanged);


            PinItems = new ObservableCollection<PinItemViewModel>(
                PinSettings.Pins.Select(x => Container.Resolve<PinItemViewModel>(new ParameterOverride("pin", x)))
                );
            PinSettings.Pins.CollectionChangedAsObservable()
                .Subscribe(args => 
                {
                    if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                    {
                        foreach (var item in args.NewItems)
                        {
                            PinItems.Add(Container.Resolve<PinItemViewModel>(new ParameterOverride("pin", item as HohoemaPin)));
                        }
                    }
                    else if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                    {
                        foreach (var item in args.OldItems)
                        {
                            var removedPin = item as HohoemaPin;
                            var pinVM = PinItems.FirstOrDefault(x => x.Pin == removedPin);
                            if (pinVM != null)
                            {
                                PinItems.Remove(pinVM);
                            }
                        }
                    }
                });

            ResetMenuItems();

            // TODO; PinSettings側で自動保存するようにしたい
            PinItems.CollectionChangedAsObservable()
                .Subscribe(async _ =>
                {
                    await PinSettings.Save();
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

                    if (Services.Helpers.DeviceTypeHelper.IsXbox || AppearanceSettings.IsForceTVModeEnable)
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

            NiconicoSession.ObserveProperty(x => x.IsLoggedIn)
                .Subscribe(x => IsLoggedIn = x);

            NiconicoSession.ObserveProperty(x => x.UserName)
                .Subscribe(x =>
                {
                    UserName = x;
                });

            NiconicoSession.ObserveProperty(x => x.UserIconUrl)
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


            // 検索履歴アイテムを初期化
            ResetSearchHistoryItems();
        }

        public PageManager PageManager { get; private set; }
        public Commands.LoginToNiconicoCommand LoginToNiconicoCommand { get; }
        public Commands.LogoutFromNiconicoCommand LogoutFromNiconicoCommand { get; }
        public Microsoft.Practices.Unity.IUnityContainer Container { get; }
        public IScheduler Scheduler { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public PinSettings PinSettings { get; }
        public NiconicoSession NiconicoSession { get; }
        public Models.LocalMylist.LocalMylistManager LocalMylistManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public VideoCacheManager VideoCacheManager { get; }
        public PlayerViewManager PlayerViewManager { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public Services.NicoLiveSubscriber NicoLiveSubscriber { get; private set; }

        public ReactiveProperty<bool> IsTVModeEnable { get; private set; }
        public bool IsNeedFullScreenToggleHelp { get; private set; }

        public ReadOnlyReactiveProperty<HohoemaAppServiceLevel> ServiceLevel { get; private set; }

        public ReactiveProperty<bool> IsShowInAppNotification { get; private set; }

        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }
        
        

        private Dictionary<string, object> viewModelState = new Dictionary<string, object>();


        public ReactivePropertySlim<bool> CanGoBackNavigation { get; private set; }
        public ReactiveCommand GoBackNavigationCommand { get; private set; }



        /// <summary>
        /// メニューアイテムの更新
        /// メニュー初期化時、およびピン留めの内容を更新した際に呼び出します
        /// </summary>
        private void ResetMenuItems()
        {
            MainSelectedItem.Value = null;

            MenuItems.Clear();
            if (NiconicoSession.IsLoggedIn)
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

       
        private async void UpdateCanGoBackNavigation()
        {
            await Task.Delay(50);
            CanGoBackNavigation.Value = PageManager.NavigationService.CanGoBack();
        }


        //private void HohoemaPlaylist_OpenPlaylistItem(Interfaces.IMylist playlist, PlaylistItem item)
        //{
        //    Scheduler.Schedule(() =>
        //    {
        //        ShowPlayer(item);
        //    });
        //}

        //private bool ShowPlayer(PlaylistItem item)
        //{
        //    string pageType = null;
        //    string parameter = null;
        //    switch (item.Type)
        //    {
        //        case PlaylistItemType.Video:
        //            pageType = nameof(Views.VideoPlayerPage);
        //            parameter = new VideoPlayPayload()
        //            {
        //                VideoId = item.ContentId
        //            }
        //            .ToParameterString();
        //            break;
        //        case PlaylistItemType.Live:
        //            pageType = nameof(Views.LivePlayerPage);
        //            parameter = new LiveVideoPagePayload(item.ContentId)
        //            {
        //                LiveTitle = item.Title,
        //            }
        //            .ToParameterString();
        //            break;
        //        default:
        //            break;
        //    }

        //    if (NavigationService.Navigate(pageType, parameter))
        //    {
        //        ApplicationView currentView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
        //        if (Services.Helpers.DeviceTypeHelper.IsMobile)
        //        {
        //            currentView.TryEnterFullScreenMode();
        //        }
        //        else if (Services.Helpers.DeviceTypeHelper.IsDesktop && !HohoemaPlaylist.IsPlayerFloatingModeEnable)
        //        {
        //            // 
        //            if (currentView.AdjacentToLeftDisplayEdge && currentView.AdjacentToRightDisplayEdge)
        //            {
        //                currentView.TryEnterFullScreenMode();
        //            }
        //        }

        //        return true;
        //    }
        //    else { return false; }
        //}

        //private async void ClosePlayer()
        //{
        //    NavigationService.Navigate("Blank", null);

        //    if (ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen)
        //    {
        //        ApplicationView currentView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();
        //        currentView.ExitFullScreenMode();
        //    }

        //    await VideoCacheManager.ResumeCacheDownload();
        //}

        //private DelegateCommand _PlayerFillDisplayCommand;
        //public DelegateCommand TogglePlayerFloatDisplayCommand
        //{
        //    get
        //    {
        //        return _PlayerFillDisplayCommand
        //            ?? (_PlayerFillDisplayCommand = new DelegateCommand(() =>
        //            {
        //                // プレイヤー表示中だけ切り替えを受け付け
        //                if (!HohoemaPlaylist.IsDisplayMainViewPlayer) { return; }

        //                // メインウィンドウでの表示状態を「ウィンドウ全体」⇔「小窓表示」と切り替える
        //                if (HohoemaPlaylist.PlayerDisplayType == PlayerViewMode.PrimaryView)
        //                {
        //                    HohoemaPlaylist.PlayerDisplayType = PlayerViewMode.PrimaryWithSmall;
        //                }
        //                else if (HohoemaPlaylist.PlayerDisplayType == PlayerViewMode.PrimaryWithSmall)
        //                {
        //                    HohoemaPlaylist.PlayerDisplayType = PlayerViewMode.PrimaryView;
        //                }
        //            }));
        //    }
        //}


        //private DelegateCommand _ClosePlayerCommand;
        //public DelegateCommand ClosePlayerCommand
        //{
        //    get
        //    {
        //        return _ClosePlayerCommand
        //            ?? (_ClosePlayerCommand = new DelegateCommand(() =>
        //            {
        //                HohoemaPlaylist.IsDisplayMainViewPlayer = false;
        //            }));
        //    }
        //}


        private DelegateCommand _AddPinToCurrentPageCommand;
        public DelegateCommand AddPinToCurrentPageCommand
        {
            get
            {
                return _AddPinToCurrentPageCommand
                    ?? (_AddPinToCurrentPageCommand = new DelegateCommand(() =>
                    {
                        string pageParameter = PageManager.PageNavigationParameter as string;
                        if (pageParameter == null)
                        {
                            System.Diagnostics.Debug.WriteLine("can not Pin this page : " + PageManager.PageTitle);
                            return;
                        }

                        if (PinSettings.Pins.Any(x => x.PageType == PageManager.CurrentPageType && x.Parameter == pageParameter))
                        {
                            System.Diagnostics.Debug.WriteLine("Pin already exist : " + PageManager.PageTitle);
                            return;
                        }

                        PinSettings.Pins.Insert(0, new HohoemaPin()
                        {
                            Label = PageManager.PageTitle,
                            PageType = PageManager.CurrentPageType,
                            Parameter = pageParameter
                        });

                        PinSettings.Save().ConfigureAwait(false);

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
                        await NiconicoSession.CheckSignedInStatus();

                        if (NiconicoSession.IsLoggedIn)
                        {
                            PageManager.OpenPage(HohoemaPageType.UserInfo, NiconicoSession.UserIdString);
                        }
                    }));
            }
        }

        public DelegateCommand OpenDebugPageCommand
        {
            get
            {
                return  new DelegateCommand(() =>
                    {
                        PageManager.OpenDebugPage();
                    });
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
        public ObservableCollection<PinItemViewModel> PinItems { get; private set; }

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


    public sealed class PinItemViewModel : BindableBase
    {
        public PinItemViewModel(
            HohoemaPin pin,
            DialogService dialogService,
            PinSettings pinSettings
            )
        {
            Pin = pin;
            DialogService = dialogService;
            PinSettings = pinSettings;
        }

        public HohoemaPin Pin { get; }
        public DialogService DialogService { get; }
        public PinSettings PinSettings { get; }


        public HohoemaPageType PageType => Pin.PageType;
        public string Parameter => Pin.Parameter;
        public string Label => Pin.Label;
        public string OverrideLabel => Pin.OverrideLabel;

        ICommand _ChangeOverrideLabelCommand;
        public ICommand ChangeOverrideLabelCommand
        {
            get
            {
                return _ChangeOverrideLabelCommand
                    ?? (_ChangeOverrideLabelCommand = new DelegateCommand(async () =>
                    {
                        var name = OverrideLabel ?? $"{Label} ({PageType.ToCulturelizeString()})";
                        var result = await DialogService.GetTextAsync(
                            $"{name} のリネーム",
                            "例）音楽のランキング（空欄にするとデフォルト名に戻せます）",
                            name,
                            (s) => true
                            );

                        Pin.OverrideLabel = string.IsNullOrEmpty(result) ? null : result;
                        RaisePropertyChanged(nameof(OverrideLabel));
                        
                    }));
            }
        }

        ICommand _RemovePinCommand;
        public ICommand RemovePinCommand
        {
            get
            {
                return _RemovePinCommand
                    ?? (_RemovePinCommand = new DelegateCommand(() =>
                    {
                        PinSettings.Pins.Remove(Pin);
                    }));
            }
        }
    }



    public enum NiconicoServiceType
    {
        Video,
        Live
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
        public VideoMenuSubPageContent(
            NiconicoSession niconicoSession,  
            Models.LocalMylist.LocalMylistManager localMylistManager,
            Models.UserMylistManager mylistManager,
            PageManager pageManager
            )
        {
            NiconicoSession = niconicoSession;
            LocalMylistManager = localMylistManager;
            MylistManager = mylistManager;
            PageManager = pageManager;
            MenuItems = new ObservableCollection<HohoemaListingPageItemBase>();

            ResetMenuItems();

            LocalMylists = LocalMylistManager.Mylists
               .ToReadOnlyReactiveCollection(x =>
               new MenuItemViewModel(x.Label, HohoemaPageType.Mylist, new MylistPagePayload(x.Id) { Origin = x.ToMylistOrigin()}.ToParameterString()) as HohoemaListingPageItemBase
               )
               .AddTo(_CompositeDisposable);
            Mylists = MylistManager.Mylists
                .ToReadOnlyReactiveCollection(x =>
                new MenuItemViewModel(x.Label, HohoemaPageType.Mylist, new MylistPagePayload(x.Id) { Origin = x.ToMylistOrigin() }.ToParameterString()) as HohoemaListingPageItemBase
                )
                .AddTo(_CompositeDisposable);

            NiconicoSession.LogIn += OnLogIn;
            NiconicoSession.LogOut += OnLogOut;
        }

        public Models.UserMylistManager MylistManager { get;  }
        public NiconicoSession NiconicoSession { get; }
        public Models.LocalMylist.LocalMylistManager LocalMylistManager { get; }
        public PageManager PageManager { get; }

        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> LocalMylists { get; private set; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> Mylists { get; private set; }

        private void ResetMenuItems()
        {
            MenuItems.Clear();
            if (NiconicoSession.IsLoggedIn)
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

        private void OnLogIn(object sender, NiconicoSessionLoginEventArgs e)
        {
            ResetMenuItems();

            System.Diagnostics.Debug.WriteLine("サインイン：メニューをリセットしました");
        }

        private void OnLogOut(object sender, object e)
        {
            ResetMenuItems();

            System.Diagnostics.Debug.WriteLine("サインアウト：メニューをリセットしました");
        }


        public void Dispose()
        {
            _CompositeDisposable.Dispose();

            NiconicoSession.LogIn -= OnLogIn;
            NiconicoSession.LogOut -= OnLogOut;
        }

    }

    public class LiveMenuSubPageContent : ViewModelBase
    {
        
        public LiveMenuSubPageContent(
            NiconicoSession niconicoSession, 
            NicoLiveSubscriber nicoLiveSubscriber,
            Services.HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager
            )
        {
            NiconicoSession = niconicoSession;
            LiveSubscriber = nicoLiveSubscriber;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            UpdateOnAirStreamsCommand = new AsyncReactiveCommand();
            
            UpdateOnAirStreamsCommand.Subscribe(async _ => 
            {
                await LiveSubscriber.UpdateOnAirStreams();
            });

            OnAirStreams = LiveSubscriber.OnAirStreams.ToReadOnlyReactiveCollection(x => 
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

            ReservedStreams = LiveSubscriber.ReservedStreams.ToReadOnlyReactiveCollection(x =>
            new OnAirStream()
            {
                BroadcasterId = x.Video.UserId.ToString(),
                Id = x.Video.Id,
                Label = x.Video.Title,
                Thumbnail = x.Community?.ThumbnailSmall,
                CommunityName = x.Community.Name,
                StartAt = x.Video.StartTime.Value,
            }
            );

            MenuItems = new List<HohoemaListingPageItemBase>();

            NiconicoSession.ObserveProperty(x => x.IsLoggedIn)
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

        public NiconicoSession NiconicoSession { get; }
        public NicoLiveSubscriber LiveSubscriber { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }

        public AsyncReactiveCommand UpdateOnAirStreamsCommand { get; }

        public List<HohoemaListingPageItemBase> MenuItems { get; private set; }

        public ReadOnlyReactiveCollection<OnAirStream> OnAirStreams { get; }
        public ReadOnlyReactiveCollection<OnAirStream> ReservedStreams { get; }

    }

    public class OnAirStream : Interfaces.ILiveContent
    {
        public string BroadcasterId { get; internal set; }
        public string Id { get; internal set; }
        public string Label { get; internal set; }

        public string CommunityName { get; internal set; }
        public string Thumbnail { get; internal set; }

        public DateTimeOffset StartAt { get; internal set; }

        public string ProviderId => BroadcasterId;

        public string ProviderName => CommunityName;

        public CommunityType ProviderType => CommunityType.Community; // TODO: 
    }
}

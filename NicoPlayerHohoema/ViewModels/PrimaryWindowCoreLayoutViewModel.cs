using Mntone.Nico2.Live;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Services.Player;
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.UseCase.Page.Commands;
using NicoPlayerHohoema.UseCase.Pin;
using NicoPlayerHohoema.UseCase.Playlist;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{


    public enum NiconicoServiceType
    {
        Video,
        Live
    }

    

    public class MenuItemViewModel : HohoemaListingPageItemBase, IPageNavigatable
    {
        public MenuItemViewModel(string label, HohoemaPageType pageType, INavigationParameters paramaeter = null)
        {
            Label = label;
            PageType = pageType;
            Parameter = paramaeter;

            IsSelected = false;
        }

        public HohoemaPageType PageType { get; set; }
        public INavigationParameters Parameter { get; set; }
    }


    public abstract class MenuItemBase : BindableBase
    {

    }

    public class EmptyContentViewModel : MenuItemBase
    {

    }


    public sealed class PrimaryWindowCoreLayoutViewModel : BindableBase
    {
        public PrimaryWindowCoreLayoutViewModel(
            IEventAggregator eventAggregator,
            NiconicoSession niconicoSession,
            PageManager pageManager,
            PinSettings pinSettings,
            AppearanceSettings appearanceSettings,
            UseCase.Page.Commands.SearchCommand searchCommand,
            PinRemoveCommand pinRemoveCommand,
            PinChangeOverrideLabelCommand pinChangeOverrideLabelCommand,
            VideoMenuSubPageContent videoMenuSubPageContent,
            LiveMenuSubPageContent liveMenuSubPageContent,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            ObservableMediaPlayer observableMediaPlayer
            )
        {
            EventAggregator = eventAggregator;
            NiconicoSession = niconicoSession;
            PageManager = pageManager;
            PinSettings = pinSettings;
            AppearanceSettings = appearanceSettings;
            SearchCommand = searchCommand;
            PinRemoveCommand = pinRemoveCommand;
            PinChangeOverrideLabelCommand = pinChangeOverrideLabelCommand;
            VideoMenu = videoMenuSubPageContent;
            LiveMenu = liveMenuSubPageContent;
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            ObservableMediaPlayer = observableMediaPlayer;
        }

        public IEventAggregator EventAggregator { get; }
        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }
        public PinSettings PinSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public SearchCommand SearchCommand { get; }
        public PinRemoveCommand PinRemoveCommand { get; }
        public PinChangeOverrideLabelCommand PinChangeOverrideLabelCommand { get; }
        public VideoMenuSubPageContent VideoMenu { get; set; }
        public LiveMenuSubPageContent LiveMenu { get; set; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public ObservableMediaPlayer ObservableMediaPlayer { get; }


        // call from PrimaryWindowsCoreLayout.xaml.cs
        public void AddPin(HohoemaPin pin)
        {
            if (pin != null)
            {
                PinSettings.Pins.Add(pin);
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
                            PageManager.OpenPageWithId(HohoemaPageType.UserInfo, NiconicoSession.UserIdString);
                        }
                    }));
            }
        }

        public DelegateCommand OpenDebugPageCommand
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    PageManager.OpenDebugPage();
                });
            }
        }


    }


    public class VideoMenuSubPageContent : MenuItemBase, IDisposable
    {
        public VideoMenuSubPageContent(
            NiconicoSession niconicoSession,
            LocalMylistManager localMylistManager,
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

            LocalMylists = LocalMylistManager.LocalPlaylists
                .ToReadOnlyReactiveCollection(x =>
                new MenuItemViewModel(x.Label, HohoemaPageType.LocalPlaylist, new NavigationParameters { { "id", x.Id } }) as HohoemaListingPageItemBase
                )
                .AddTo(_CompositeDisposable);
            Mylists = MylistManager.Mylists
                .ToReadOnlyReactiveCollection(x =>
                new MenuItemViewModel(x.Label, HohoemaPageType.Mylist, new NavigationParameters { { "id", x.Id } }) as HohoemaListingPageItemBase
                )
                .AddTo(_CompositeDisposable);

            NiconicoSession.LogIn += OnLogIn;
            NiconicoSession.LogOut += OnLogOut;
        }

        public Models.UserMylistManager MylistManager { get; }
        public NiconicoSession NiconicoSession { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public PageManager PageManager { get; }

        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> LocalMylists { get; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> Mylists { get; }

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
                //                MenuItems.Add(new MenuItemViewModel("オススメ", HohoemaPageType.Recommend));
                MenuItems.Add(new MenuItemViewModel("あとで見る", HohoemaPageType.LocalPlaylist, new NavigationParameters { { "id", HohoemaPlaylist.WatchAfterPlaylistId } }));
            }
            else
            {
                MenuItems.Add(new MenuItemViewModel("ランキング", HohoemaPageType.RankingCategoryList));
                MenuItems.Add(new MenuItemViewModel("新着", HohoemaPageType.Subscription));
                //                MenuItems.Add(new MenuItemViewModel("視聴履歴", HohoemaPageType.WatchHistory));
                MenuItems.Add(new MenuItemViewModel("キャッシュ", HohoemaPageType.CacheManagement));
                MenuItems.Add(new MenuItemViewModel("あとで見る", HohoemaPageType.LocalPlaylist, new NavigationParameters { { "id", HohoemaPlaylist.WatchAfterPlaylistId } }));
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

    public class LiveMenuSubPageContent : MenuItemBase
    {

        public LiveMenuSubPageContent(
            NiconicoSession niconicoSession,
            PageManager pageManager
            )
        {
            NiconicoSession = niconicoSession;
            PageManager = pageManager;
            MenuItems = new ObservableCollection<HohoemaListingPageItemBase>();

            NiconicoSession.LogIn += (_, __) => ResetItems();
            NiconicoSession.LogOut += (_, __) => ResetItems();

            ResetItems();
        }

        private async void ResetItems()
        {
            using (await NiconicoSession.SigninLock.LockAsync())
            {
                MenuItems.Clear();

                if (NiconicoSession.IsLoggedIn)
                {
                    MenuItems.Add(new MenuItemViewModel("タイムシフト", HohoemaPageType.Timeshift));
                    MenuItems.Add(new MenuItemViewModel("ニコレポ", HohoemaPageType.NicoRepo));
                    MenuItems.Add(new MenuItemViewModel("フォロー", HohoemaPageType.FollowManage));
                }

                RaisePropertyChanged(nameof(MenuItems));
            }
        }


        public NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }

        public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; }

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

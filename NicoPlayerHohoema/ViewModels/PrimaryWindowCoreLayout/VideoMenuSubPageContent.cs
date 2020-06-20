using I18NPortable;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.Playlist;
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

namespace NicoPlayerHohoema.ViewModels.PrimaryWindowCoreLayout
{
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
        public ObservableCollection<HohoemaListingPageItemBase> SecondaryMenuItems { get; private set; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> LocalMylists { get; }
        public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> Mylists { get; }

        private void ResetMenuItems()
        {
            MenuItems.Clear();
            if (NiconicoSession.IsLoggedIn)
            {
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList));
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo));
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.FollowManage.Translate(), HohoemaPageType.FollowManage));
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.WatchHistory.Translate(), HohoemaPageType.WatchHistory));
                MenuItems.Add(new SeparatorMenuItemViewModel());
                MenuItems.Add(new MenuItemViewModel("@view".Translate(), HohoemaPageType.WatchAfter));
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement));
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement));
                //                MenuItems.Add(new MenuItemViewModel("オススメ".Translate(), HohoemaPageType.Recommend));
            }
            else
            {
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList));
                MenuItems.Add(new SeparatorMenuItemViewModel());
                MenuItems.Add(new MenuItemViewModel("@view".Translate(), HohoemaPageType.WatchAfter));
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement));
                //                MenuItems.Add(new MenuItemViewModel("視聴履歴", HohoemaPageType.WatchHistory));
                MenuItems.Add(new MenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement));
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
}

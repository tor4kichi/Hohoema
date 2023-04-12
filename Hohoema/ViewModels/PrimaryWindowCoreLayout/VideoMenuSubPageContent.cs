#nullable enable
using Hohoema.Models.Niconico;
using Hohoema.Models.PageNavigation;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Niconico;
using I18NPortable;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace Hohoema.ViewModels.PrimaryWindowCoreLayout;

public sealed class VideoMenuSubPageContent : MenuItemBase, IDisposable
{
    public VideoMenuSubPageContent(
        NiconicoSession niconicoSession,
        LocalMylistManager localMylistManager,
        LoginUserOwnedMylistManager mylistManager,
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
            new NavigateAwareMenuItemViewModel(x.Name, HohoemaPageType.LocalPlaylist, new NavigationParameters { { "id", x.PlaylistId.Id } }) as HohoemaListingPageItemBase
            )
            .AddTo(_CompositeDisposable);
        Mylists = MylistManager.Mylists
            .ToReadOnlyReactiveCollection(x =>
            new NavigateAwareMenuItemViewModel(x.Name, HohoemaPageType.Mylist, new NavigationParameters { { "id", x.MylistId } }) as HohoemaListingPageItemBase
            )
            .AddTo(_CompositeDisposable);

        NiconicoSession.LogIn += OnLogIn;
        NiconicoSession.LogOut += OnLogOut;
    }

    public LoginUserOwnedMylistManager MylistManager { get; }
    public NiconicoSession NiconicoSession { get; }
    public LocalMylistManager LocalMylistManager { get; }
    public PageManager PageManager { get; }

    private CompositeDisposable _CompositeDisposable = new CompositeDisposable();

    public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; }
    public ObservableCollection<HohoemaListingPageItemBase> SecondaryMenuItems { get; private set; }
    public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> LocalMylists { get; }
    public ReadOnlyReactiveCollection<HohoemaListingPageItemBase> Mylists { get; }

    QueueMenuItemViewModel _watchAfterMenuItemViewModel;
    private void ResetMenuItems()
    {
        MenuItems.Clear();
        if (NiconicoSession.IsLoggedIn)
        {
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList));
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.NicoRepo.Translate(), HohoemaPageType.NicoRepo));
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.FollowManage.Translate(), HohoemaPageType.FollowManage));
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.WatchHistory.Translate(), HohoemaPageType.WatchHistory));
            MenuItems.Add(new SeparatorMenuItemViewModel());
            MenuItems.Add(_watchAfterMenuItemViewModel);
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement));
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement));
            //                MenuItems.Add(new MenuItemViewModel("オススメ".Translate(), HohoemaPageType.Recommend));
        }
        else
        {
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.RankingCategoryList.Translate(), HohoemaPageType.RankingCategoryList));
            MenuItems.Add(new SeparatorMenuItemViewModel());
            MenuItems.Add(_watchAfterMenuItemViewModel);
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.SubscriptionManagement.Translate(), HohoemaPageType.SubscriptionManagement));
            //                MenuItems.Add(new MenuItemViewModel("視聴履歴", HohoemaPageType.WatchHistory));
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.CacheManagement.Translate(), HohoemaPageType.CacheManagement));
        }

        OnPropertyChanged(nameof(MenuItems));
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

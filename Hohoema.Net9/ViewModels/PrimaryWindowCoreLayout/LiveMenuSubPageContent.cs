﻿#nullable enable
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.PageNavigation;
using I18NPortable;
using NiconicoToolkit.Live;
using System;
using System.Collections.ObjectModel;

namespace Hohoema.ViewModels.PrimaryWindowCoreLayout;

public class LiveMenuSubPageContent : MenuItemBase
{

    public LiveMenuSubPageContent(
        NiconicoSession niconicoSession
        )
    {
        NiconicoSession = niconicoSession;        
        MenuItems = new ObservableCollection<HohoemaListingPageItemBase>();

        NiconicoSession.LogIn += (_, __) => ResetItems();
        NiconicoSession.LogOut += (_, __) => ResetItems();

        ResetItems();
    }

    private void ResetItems()
    {
        MenuItems.Clear();

        if (NiconicoSession.IsLoggedIn)
        {
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.Timeshift.Translate(), HohoemaPageType.Timeshift));
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.FollowingsActivity.Translate(), HohoemaPageType.FollowingsActivity));
            MenuItems.Add(new NavigateAwareMenuItemViewModel(HohoemaPageType.FollowManage.Translate(), HohoemaPageType.FollowManage));
        }

        OnPropertyChanged(nameof(MenuItems));
    }


    public NiconicoSession NiconicoSession { get; }
    public ObservableCollection<HohoemaListingPageItemBase> MenuItems { get; private set; }

}

public class OnAirStream : ILiveContent, ILiveContentProvider
{
    public string BroadcasterId { get; internal set; }
    public LiveId LiveId { get; internal set; }
    public string Title { get; internal set; }

    public string CommunityName { get; internal set; }
    public string Thumbnail { get; internal set; }

    public DateTimeOffset StartAt { get; internal set; }

    public string ProviderId => BroadcasterId;

    public string ProviderName => CommunityName;

    public ProviderType ProviderType => ProviderType.Community; // TODO
}

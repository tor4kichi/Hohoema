#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Services;
using Hohoema.Services.Navigations;
using Hohoema.ViewModels.Niconico.Follow;
using Reactive.Bindings;
using System;

namespace Hohoema.ViewModels.Pages.Niconico.Follow;

public class FollowManagePageViewModel : HohoemaPageViewModelBase
{
    public FollowManagePageViewModel(
       ApplicationLayoutManager applicationLayoutManager,
       PageManager pageManager,
       NiconicoSession niconicoSession,
       IMessenger messenger,
       UserFollowProvider userFollowProvider,
       TagFollowProvider tagFollowProvider,
       MylistFollowProvider mylistFollowProvider,
       ChannelFollowProvider channelFollowProvider,
       CommunityFollowProvider communityFollowProvider
       )
    {
        ApplicationLayoutManager = applicationLayoutManager;
        PageManager = pageManager;
        NiconicoSession = niconicoSession;
        _messenger = messenger;
        _userFollowProvider = userFollowProvider;
        _tagFollowProvider = tagFollowProvider;
        _mylistFollowProvider = mylistFollowProvider;
        _channelFollowProvider = channelFollowProvider;
        _communityFollowProvider = communityFollowProvider;
    }



    public ReactiveProperty<bool> NowUpdatingFavList { get; }


    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public PageManager PageManager { get; }
    public NiconicoSession NiconicoSession { get; }

    object[] _FollowGroups_AvoidListViewMemoryLeak;
    private readonly IMessenger _messenger;
    private readonly UserFollowProvider _userFollowProvider;
    private readonly TagFollowProvider _tagFollowProvider;
    private readonly MylistFollowProvider _mylistFollowProvider;
    private readonly ChannelFollowProvider _channelFollowProvider;
    private readonly CommunityFollowProvider _communityFollowProvider;

    public object[]? FollowGroups { get; private set; }

    public override void OnNavigatingTo(INavigationParameters parameters)
    {
        FollowGroups = new object[]
        {
            new FollowUserGroupViewModel(_userFollowProvider, PageManager, _messenger),
            new FollowTagGroupViewModel(_tagFollowProvider, PageManager, _messenger),
            new FolloMylistGroupViewModel(_mylistFollowProvider, PageManager, _messenger),
            new FollowChannelGroupViewModel(_channelFollowProvider, PageManager, _messenger),
            new FollowCommunityGroupViewModel(_communityFollowProvider, NiconicoSession.UserId ?? 0, PageManager, _messenger),
        }; ;
        OnPropertyChanged(nameof(FollowGroups));

        base.OnNavigatingTo(parameters);
    }

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        foreach (var group in FollowGroups!)
        {
            (group as IDisposable)?.Dispose();
        }

        FollowGroups = null;
        OnPropertyChanged(nameof(FollowGroups));

        base.OnNavigatedFrom(parameters);
    }

    //private RelayCommand<FollowItemInfo> _RemoveFavoriteCommand;
    //public RelayCommand<FollowItemInfo> RemoveFavoriteCommand
    //{
    //    get
    //    {
    //        return _RemoveFavoriteCommand
    //            ?? (_RemoveFavoriteCommand = new RelayCommand<FollowItemInfo>(async (followItem) =>
    //            {
    //                switch (followItem.FollowItemType)
    //                {
    //                    case FollowItemType.Tag:
    //                        await FollowManager.Tag.RemoveFollow(followItem.Id);
    //                        break;
    //                    case FollowItemType.Mylist:
    //                        await FollowManager.Mylist.RemoveFollow(followItem.Id);
    //                        break;
    //                    case FollowItemType.User:
    //                        await FollowManager.User.RemoveFollow(followItem.Id);
    //                        break;
    //                    case FollowItemType.Community:
    //                        await FollowManager.Community.RemoveFollow(followItem.Id);
    //                        break;
    //                    case FollowItemType.Channel:
    //                        await FollowManager.Channel.RemoveFollow(followItem.Id);
    //                        break;
    //                    default:
    //                        break;
    //                }
    //            }));
    //    }
    //}
}

using System;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using System.Diagnostics;
using Hohoema.Services;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Contracts.Services.Navigations;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow;
using System.ComponentModel;
using Hohoema.ViewModels.Niconico.Follow;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Community;
using Hohoema.Contracts.Services.Navigations;
using CommunityToolkit.Mvvm.Messaging;

namespace Hohoema.ViewModels.Pages.Niconico.Follow
{
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

            _FollowGroups_AvoidListViewMemoryLeak = new object[]
            {
                new FollowUserGroupViewModel(userFollowProvider, pageManager, messenger),
                new FollowTagGroupViewModel(tagFollowProvider, pageManager, messenger),
                new FolloMylistGroupViewModel(mylistFollowProvider, pageManager, messenger),
                new FollowChannelGroupViewModel(channelFollowProvider, pageManager, messenger),
                new FollowCommunityGroupViewModel(communityFollowProvider, NiconicoSession.UserId, pageManager, messenger),
            };
        }



        public ReactiveProperty<bool> NowUpdatingFavList { get; }


        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }

        object[] _FollowGroups_AvoidListViewMemoryLeak;
        public object[] FollowGroups { get; private set; }

        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            FollowGroups = _FollowGroups_AvoidListViewMemoryLeak;
            OnPropertyChanged(nameof(FollowGroups));

            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
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
}

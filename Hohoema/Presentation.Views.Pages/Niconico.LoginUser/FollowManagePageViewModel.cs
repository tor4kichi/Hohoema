using System;
using System.Linq;
using System.Text;
using Prism.Commands;
using System.Collections.ObjectModel;
using Reactive.Bindings;
using System.Diagnostics;
using Hohoema.Models.UseCase;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Follow;
using System.ComponentModel;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Community;
using Prism.Navigation;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.LoginUser
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
            RaisePropertyChanged(nameof(FollowGroups));

            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            FollowGroups = null;
            RaisePropertyChanged(nameof(FollowGroups));

            base.OnNavigatedFrom(parameters);
        }

        //private DelegateCommand<FollowItemInfo> _RemoveFavoriteCommand;
        //public DelegateCommand<FollowItemInfo> RemoveFavoriteCommand
        //{
        //    get
        //    {
        //        return _RemoveFavoriteCommand
        //            ?? (_RemoveFavoriteCommand = new DelegateCommand<FollowItemInfo>(async (followItem) =>
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

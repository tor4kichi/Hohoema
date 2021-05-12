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

namespace Hohoema.Presentation.ViewModels.Pages.Niconico.LoginUser
{
    public class FollowManagePageViewModel : HohoemaPageViewModelBase
	{
     	public FollowManagePageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            NiconicoSession niconicoSession,
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

            FollowGroups = new FollowGroupViewModel[]
            {
                new FollowGroupViewModel(FollowItemType.User, new FollowUserIncrementalSource(userFollowProvider)),
                new FollowGroupViewModel(FollowItemType.Tag, new FollowTagIncrementalSource(tagFollowProvider)),
                new FollowGroupViewModel(FollowItemType.Mylist, new FollowMylistIncrementalSource(mylistFollowProvider)),
                new FollowGroupViewModel(FollowItemType.Channel, new FollowChannelIncrementalSource(channelFollowProvider)),
                new FollowGroupViewModel(FollowItemType.Community, new FollowCommunityIncrementalSource(communityFollowProvider)),
            };
        }

        public ReactiveProperty<bool> NowUpdatingFavList { get; }


        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public PageManager PageManager { get; }
        public NiconicoSession NiconicoSession { get; }


        public FollowGroupViewModel[] FollowGroups { get; }

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

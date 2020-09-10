using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using Mntone.Nico2.Users.Video;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
using Prism.Mvvm;
using Windows.UI.Core;
using Hohoema.Models.Domain.Helpers;
using Prism.Commands;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.UserFeature;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Models.Domain.Niconico.UserFeature.Follow
{
	public class FollowManager : AsyncInitialize
	{

		#region Niconico follow constants

		// Note: 2016/10/31 から お気に入りユーザー枠は一般プレミアムどちらも600に変更
		public const uint FOLLOW_USER_MAX_COUNT = 600;
		public const uint PREMIUM_FOLLOW_USER_MAX_COUNT = 600;

		public const uint FOLLOW_MYLIST_MAX_COUNT = 20;
		public const uint PREMIUM_FOLLOW_MYLIST_MAX_COUNT = 50;

		public const uint FOLLOW_TAG_MAX_COUNT = 30;
		public const uint PREMIUM_FOLLOW_TAG_MAX_COUNT = 30;

		public const uint FOLLOW_COMMUNITY_MAX_COUNT = 100;
		public const uint PREMIUM_FOLLOW_COMMUNITY_MAX_COUNT = 600;

        public const uint FOLLOW_CHANNEL_MAX_COUNT = uint.MaxValue;
        public const uint PREMIUM_FOLLOW_CHANNEL_MAX_COUNT = uint.MaxValue;

        #endregion


		#region Properties 


		public IFollowInfoGroup Tag { get; private set; }
		public IFollowInfoGroup Mylist { get; private set; }
		public IFollowInfoGroup User { get; private set; }
		public IFollowInfoGroup Community { get; private set; }
        public IFollowInfoGroup Channel { get; private set; }
        public NiconicoSession NiconicoSession { get; }
        public TagFollowProvider TagFollowProvider { get; }
        public MylistFollowProvider MylistFollowProvider { get; }
        public UserFollowProvider UserFollowProvider { get; }
        public CommunityFollowProvider CommunityFollowProvider { get; }
        public ChannelFollowProvider ChannelFollowProvider { get; }

        IReadOnlyList<IFollowInfoGroup> _AllFollowInfoGroups;

        Dictionary<FollowItemType, IFollowInfoGroup> _FollowGroupsMap;



        public IReadOnlyList<IFollowInfoGroup> GetAllFollowInfoGroups() => _AllFollowInfoGroups ?? (_AllFollowInfoGroups = new List<IFollowInfoGroup> 
		{
			Tag,
			Mylist,
			User,
			Community,
            Channel
        });


        #endregion

        #region Fields

        AsyncLock _SyncLock = new AsyncLock();

        #endregion

        public FollowManager(
            NiconicoSession niconicoSession, 
            TagFollowProvider tagFollowProvider,
            MylistFollowProvider mylistFollowProvider,
            UserFollowProvider userFollowProvider,
            CommunityFollowProvider communityFollowProvider,
            ChannelFollowProvider channelFollowProvider
            )
		{
            NiconicoSession = niconicoSession;
            TagFollowProvider = tagFollowProvider;
            MylistFollowProvider = mylistFollowProvider;
            UserFollowProvider = userFollowProvider;
            CommunityFollowProvider = communityFollowProvider;
            ChannelFollowProvider = channelFollowProvider;

            Tag = new TagFollowInfoGroup(NiconicoSession, TagFollowProvider);
            Mylist = new MylistFollowInfoGroup(NiconicoSession, MylistFollowProvider);
            User = new UserFollowInfoGroup(NiconicoSession, UserFollowProvider);
            Community = new CommunityFollowInfoGroup(NiconicoSession, CommunityFollowProvider);
            Channel = new ChannelFollowInfoGroup(NiconicoSession, ChannelFollowProvider);

            _FollowGroupsMap = new Dictionary<FollowItemType, IFollowInfoGroup>();

            _FollowGroupsMap.Add(FollowItemType.Tag, Tag);
            _FollowGroupsMap.Add(FollowItemType.Mylist, Mylist);
            _FollowGroupsMap.Add(FollowItemType.User, User);
            _FollowGroupsMap.Add(FollowItemType.Community, Community);
            _FollowGroupsMap.Add(FollowItemType.Channel, Channel);

            NiconicoSession.LogIn += NiconicoSession_LogIn;
            NiconicoSession.LogOut += NiconicoSession_LogOut;
        }

        public bool IsLoginUserFollowsReady { get; private set; }

        private async void NiconicoSession_LogIn(object sender, NiconicoSessionLoginEventArgs e)
        {
            IsLoginUserFollowsReady = false;
            try
            {
                using (var cancelTokenSource = new CancellationTokenSource())
                {
                    await SyncAll(cancelTokenSource.Token);

                    IsLoginUserFollowsReady = true;
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

        private async void NiconicoSession_LogOut(object sender, EventArgs e)
        {
            IsLoginUserFollowsReady = false;

            try
            {
                using (var cancelTokenSource = new CancellationTokenSource())
                {
                    await SyncAll(cancelTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {

            }
        }

        protected override Task OnInitializeAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public bool CanMoreAddFollow(IFollowable followItem)
        {
            if (followItem == null) { return false; }

            return CanMoreAddFollow(ToFollowItemType(followItem));
        }

        public bool CanMoreAddFollow(FollowItemType itemType)
		{
			return _FollowGroupsMap[itemType].CanMoreAddFollow();
		}


        static FollowItemType ToFollowItemType(IFollowable followItem)
        {
            switch (followItem)
            {
                case IChannel _: return FollowItemType.Channel;
                case ICommunity _: return FollowItemType.Community;
                case IUser _: return FollowItemType.User;
                case IMylist _: return FollowItemType.Mylist;
                case ITag _: return FollowItemType.Tag;
                case ISearchWithtag _: return FollowItemType.Tag;

                default: throw new NotSupportedException();
            }
        }

        public bool IsFollowItem(IFollowable followItem)
        {
            if (followItem == null) { return false; }

            return IsFollowItem(ToFollowItemType(followItem), followItem.Id);
        }

        public bool IsFollowItem(FollowItemType itemType, string id)
		{
			var group = _FollowGroupsMap[itemType];

			if (itemType == FollowItemType.Tag)
			{
				id = TagStringHelper.ToEnsureHankakuNumberTagString(id);
			}

			return group.IsFollowItem(id);
		}


	

		public async Task SyncAll(CancellationToken token = default)
		{
            using (var releaser = await _SyncLock.LockAsync())
            {
                foreach (var followInfoGroup in GetAllFollowInfoGroups())
                {
                    token.ThrowIfCancellationRequested();

                    await followInfoGroup.SyncFollowItems();

                    token.ThrowIfCancellationRequested();

                    await Task.Delay(250);
                }
            }
        }

		public FollowItemInfo FindFollowInfo(FollowItemType itemType, string id)
		{
			return _FollowGroupsMap[itemType].FollowInfoItems.SingleOrDefault(x => x.Id == id);
		}

        public async Task<ContentManageResult> AddFollow(IFollowable followItem, object token = null)
        {
            return await AddFollow(ToFollowItemType(followItem), followItem.Id, followItem.Label, token);
        }

        public async Task<ContentManageResult> AddFollow(FollowItemType itemType, string id, string name, object token = null)
		{
			var group = _FollowGroupsMap[itemType];

			var result = await group.AddFollow(name, id, token);
		
			return result;
		}

        public async Task<ContentManageResult> RemoveFollow(IFollowable followItem)
        {
            return await RemoveFollow(ToFollowItemType(followItem), followItem.Id);
        }

        public async Task<ContentManageResult> RemoveFollow(FollowItemType itemType, string id)
		{
			var group = _FollowGroupsMap[itemType];

			var result = await group.RemoveFollow(id);

			return result;
		}


        private DelegateCommand<IFollowable> _RemoveFollowCommand;
        public DelegateCommand<IFollowable> RemoveFollowCommand => _RemoveFollowCommand
            ?? (_RemoveFollowCommand = new DelegateCommand<IFollowable>(async followItem => 
            {
                var result = await RemoveFollow(followItem);
            }
            , followItem => followItem is IFollowable
            ));



    }

}

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
using NicoPlayerHohoema.Helpers;

namespace NicoPlayerHohoema.Models
{
	public class FollowManager : AsyncInitialize
	{

		#region Niconico follow constants

		// Note: 2016/10/31 から お気に入りユーザー枠は一般プレミアムどちらも600に変更
		public const uint FOLLOW_USER_MAX_COUNT = 600;
		public const uint PREMIUM_FOLLOW_USER_MAX_COUNT = 600;

		public const uint FOLLOW_MYLIST_MAX_COUNT = 20;
		public const uint PREMIUM_FOLLOW_MYLIST_MAX_COUNT = 50;

		public const uint FOLLOW_TAG_MAX_COUNT = 10;
		public const uint PREMIUM_FOLLOW_TAG_MAX_COUNT = 10;

		public const uint FOLLOW_COMMUNITY_MAX_COUNT = 100;
		public const uint PREMIUM_FOLLOW_COMMUNITY_MAX_COUNT = 600;

        public const uint FOLLOW_CHANNEL_MAX_COUNT = uint.MaxValue;
        public const uint PREMIUM_FOLLOW_CHANNEL_MAX_COUNT = uint.MaxValue;

        #endregion


        public static async Task<FollowManager> Create(HohoemaApp hohoemaApp, uint userId)
		{
			var followManager = new FollowManager(hohoemaApp, userId);

            await followManager.Initialize();

            return followManager;
		}


		#region Properties 

		public uint UserId { get; set; }


		public IFollowInfoGroup Tag { get; private set; }
		public IFollowInfoGroup Mylist { get; private set; }
		public IFollowInfoGroup User { get; private set; }
		public IFollowInfoGroup Community { get; private set; }
        public IFollowInfoGroup Channel { get; private set; }

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

        HohoemaApp _HohoemaApp;

        AsyncLock _SyncLock = new AsyncLock();

        #endregion

        internal FollowManager(HohoemaApp hohoemaApp, uint userId)
		{
			_HohoemaApp = hohoemaApp;
			UserId = userId;

            Tag = new TagFollowInfoGroup(_HohoemaApp);
            Mylist = new MylistFollowInfoGroup(_HohoemaApp);
            User = new UserFollowInfoGroup(_HohoemaApp);
            Community = new CommunityFollowInfoGroup(_HohoemaApp);
            Channel = new ChannelFollowInfoGroup(_HohoemaApp);

            _FollowGroupsMap = new Dictionary<FollowItemType, IFollowInfoGroup>();

            _FollowGroupsMap.Add(FollowItemType.Tag, Tag);
            _FollowGroupsMap.Add(FollowItemType.Mylist, Mylist);
            _FollowGroupsMap.Add(FollowItemType.User, User);
            _FollowGroupsMap.Add(FollowItemType.Community, Community);
            _FollowGroupsMap.Add(FollowItemType.Channel, Channel);
        }


        protected override Task OnInitializeAsync(CancellationToken token)
        {
            return HohoemaApp.UIDispatcher.RunIdleAsync(async (_) => 
            {
                await SyncAll(token);
            })
            .AsTask();
        }


		public bool CanMoreAddFollow(FollowItemType itemType)
		{
			return _FollowGroupsMap[itemType].CanMoreAddFollow();
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


	

		public async Task SyncAll(CancellationToken token)
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

		public async Task<ContentManageResult> AddFollow(FollowItemType itemType, string id, string name, object token = null)
		{
			var group = _FollowGroupsMap[itemType];

			var result = await group.AddFollow(name, id, token);
		
			return result;
		}

		public async Task<ContentManageResult> RemoveFollow(FollowItemType itemType, string id)
		{
			var group = _FollowGroupsMap[itemType];

			var result = await group.RemoveFollow(id);

			return result;
		}

	}

}

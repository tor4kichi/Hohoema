using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Follow;
using Mntone.Nico2.Users.Follow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Follow
{
	public class MylistFollowInfoGroup : FollowInfoGroupWithFollowData
	{
		public MylistFollowInfoGroup(
            NiconicoSession niconicoSession, 
            MylistFollowProvider mylistFollowProvider
            )
		{
            NiconicoSession = niconicoSession;
            MylistFollowProvider = mylistFollowProvider;
        }

		public override FollowItemType FollowItemType => FollowItemType.Mylist;

		public override uint MaxFollowItemCount =>
            NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_MYLIST_MAX_COUNT : FollowManager.FOLLOW_MYLIST_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public MylistFollowProvider MylistFollowProvider { get; }

        protected override async Task<List<FollowData>> GetFollowSource()
		{
            return await MylistFollowProvider.GetAllAsync();
        }
		protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
            return await MylistFollowProvider.AddFollowAsync(id);
        }
		protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
            return await MylistFollowProvider.RemoveFollowAsync(id);
        }
	}
}

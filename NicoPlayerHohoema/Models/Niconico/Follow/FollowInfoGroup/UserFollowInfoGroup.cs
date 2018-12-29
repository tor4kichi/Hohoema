using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class UserFollowInfoGroup : FollowInfoGroupWithFollowData
	{
		public UserFollowInfoGroup(
            NiconicoSession niconicoSession, 
            Provider.UserFollowProvider userFollowProvider
            )
        {
            NiconicoSession = niconicoSession;
            UserFollowProvider = userFollowProvider;
        }
    

		public override FollowItemType FollowItemType => FollowItemType.User;

		public override uint MaxFollowItemCount =>
            NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_USER_MAX_COUNT : FollowManager.FOLLOW_USER_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public Provider.UserFollowProvider UserFollowProvider { get; }


        protected override async Task<List<FollowData>> GetFollowSource()
		{
            return await UserFollowProvider.GetAllAsync();

        }

		protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
            return await UserFollowProvider.AddFollowAsync(id);
		}
		protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
            return await UserFollowProvider.RemoveFollowAsync(id);
        }
	}
}

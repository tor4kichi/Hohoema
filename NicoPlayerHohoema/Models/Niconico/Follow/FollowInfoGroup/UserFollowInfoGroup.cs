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
		public UserFollowInfoGroup(HohoemaApp hohoemaApp)
			: base(hohoemaApp)
		{

		}

		public override FollowItemType FollowItemType => FollowItemType.User;

		public override uint MaxFollowItemCount =>
			HohoemaApp.IsPremiumUser ? FollowManager.PREMIUM_FOLLOW_USER_MAX_COUNT : FollowManager.FOLLOW_USER_MAX_COUNT;

		protected override Task<List<FollowData>> GetFollowSource()
		{
			return HohoemaApp.ContentProvider.GetFollowUsers();
		}

		protected override Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
			return HohoemaApp.NiconicoContext.User.AddUserFollowAsync(NiconicoItemType.User, id);
		}
		protected override Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.RemoveUserFollowAsync(NiconicoItemType.User, id);
		}
	}
}

using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class MylistFollowInfoGroup : FollowInfoGroupWithFollowData
	{
		public MylistFollowInfoGroup(HohoemaApp hohoemaApp)
			: base(hohoemaApp)
		{

		}

		public override FollowItemType FollowItemType => FollowItemType.Mylist;

		public override uint MaxFollowItemCount =>
			HohoemaApp.IsPremiumUser ? FollowManager.PREMIUM_FOLLOW_MYLIST_MAX_COUNT : FollowManager.FOLLOW_MYLIST_MAX_COUNT;


		protected override Task<List<FollowData>> GetFollowSource()
		{
			return HohoemaApp.ContentProvider.GetFavMylists();
		}
		protected override Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
			return HohoemaApp.NiconicoContext.User.AddUserFollowAsync(NiconicoItemType.Mylist, id);
		}
		protected override Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.RemoveUserFollowAsync(NiconicoItemType.Mylist, id);
		}
	}
}

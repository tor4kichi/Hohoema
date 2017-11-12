using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2;
using Mntone.Nico2.Users.FollowCommunity;

namespace NicoPlayerHohoema.Models
{
	public class CommunityFollowInfoGroup : FollowInfoGroupBaseTemplate<FollowCommunityInfo>
	{
		public CommunityFollowInfoGroup(HohoemaApp hohoemaApp) 
			: base(hohoemaApp)
		{

		}

		public override FollowItemType FollowItemType => FollowItemType.Community;

		public override uint MaxFollowItemCount =>
			HohoemaApp.IsPremiumUser ? FollowManager.PREMIUM_FOLLOW_COMMUNITY_MAX_COUNT : FollowManager.FOLLOW_COMMUNITY_MAX_COUNT;


		protected override FollowItemInfo ConvertToFollowInfo(FollowCommunityInfo source)
		{
			return new FollowItemInfo()
			{
				FollowItemType = FollowItemType,
				Name = source.CommunityName,
				Id = source.CommunityId,
			};
		}

		protected override string FollowSourceToItemId(FollowCommunityInfo source)
		{
			return source.CommunityId;
		}

		protected override async Task<List<FollowCommunityInfo>> GetFollowSource()
		{
			var res = await HohoemaApp.ContentProvider.GetFavCommunities();
			return res.Items;
		}


		protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
			var title = "";
			var comment = "";
			var notify = false;

			if (token is CommunituFollowAdditionalInfo)
			{
				var additionalInfo = token as CommunituFollowAdditionalInfo;
				title = additionalInfo.Title;
				comment = additionalInfo.Comment;
				notify = additionalInfo.Notify;
			}

			var result = await HohoemaApp.NiconicoContext.User.AddFollowCommunityAsync(id, title, comment, notify);

			return result ? ContentManageResult.Success : ContentManageResult.Failed;
		}

		protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
			var leaveToken = await HohoemaApp.NiconicoContext.User.GetFollowCommunityLeaveTokenAsync(id);
			var result = await HohoemaApp.NiconicoContext.User.RemoveFollowCommunityAsync(leaveToken);

			return result ? ContentManageResult.Success : ContentManageResult.Failed;
		}
	}
}

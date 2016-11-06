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
			var res = await HohoemaApp.ContentFinder.GetFavCommunities();
			return res.Items;
		}


		protected override Task<ContentManageResult> AddFollow_Internal(string id)
		{
			throw new NotImplementedException();
		}

		protected override Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
			throw new NotImplementedException();
		}
	}
}

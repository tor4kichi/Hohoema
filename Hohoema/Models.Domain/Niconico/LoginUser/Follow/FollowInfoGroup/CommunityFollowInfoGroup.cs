using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2;
using static Mntone.Nico2.Users.Follow.FollowCommunityResponse;

namespace Hohoema.Models.Domain.Niconico.LoginUser.Follow
{
	public class CommunityFollowInfoGroup : FollowInfoGroupBaseTemplate<FollowCommunity>
	{
		public CommunityFollowInfoGroup(
            NiconicoSession niconicoSession, CommunityFollowProvider communityFollowProvider) 
		{
            NiconicoSession = niconicoSession;
            CommunityFollowProvider = communityFollowProvider;
        }

		public override FollowItemType FollowItemType => FollowItemType.Community;

		public override uint MaxFollowItemCount =>
            NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_COMMUNITY_MAX_COUNT : FollowManager.FOLLOW_COMMUNITY_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public CommunityFollowProvider CommunityFollowProvider { get; }

        protected override FollowItemInfo ConvertToFollowInfo(FollowCommunity source)
		{
			return new FollowItemInfo()
			{
				FollowItemType = FollowItemType,
				Name = source.Name,
				Id = source.GlobalId,
				ThumbnailUrl = source.ThumbnailUrl.Small.OriginalString,
				UpdateTime = source.CreateTime.DateTime,
			};
		}

		protected override string FollowSourceToItemId(FollowCommunity source)
		{
			return source.GlobalId;
		}

		protected override async Task<List<FollowCommunity>> GetFollowSource()
		{
            return await CommunityFollowProvider.GetAllAsync();
        }

		protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
            CommunityFollowProvider.CommunituFollowAdditionalInfo =
                token as CommunituFollowAdditionalInfo;

            return await CommunityFollowProvider.AddFollowAsync(id);

        }

		protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
            return await CommunityFollowProvider.RemoveFollowAsync(id);
        }
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2;
using Mntone.Nico2.Users.FollowCommunity;

namespace Hohoema.Models
{
	public class CommunityFollowInfoGroup : FollowInfoGroupBaseTemplate<FollowCommunityInfo>
	{
		public CommunityFollowInfoGroup(
            NiconicoSession niconicoSession, Provider.CommunityFollowProvider communityFollowProvider) 
		{
            NiconicoSession = niconicoSession;
            CommunityFollowProvider = communityFollowProvider;
        }

		public override FollowItemType FollowItemType => FollowItemType.Community;

		public override uint MaxFollowItemCount =>
            NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_COMMUNITY_MAX_COUNT : FollowManager.FOLLOW_COMMUNITY_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public Provider.CommunityFollowProvider CommunityFollowProvider { get; }

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
            return await CommunityFollowProvider.GetAllAsync();
        }

		protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
            Provider.CommunityFollowProvider.CommunituFollowAdditionalInfo =
                token as CommunituFollowAdditionalInfo;

            return await CommunityFollowProvider.AddFollowAsync(id);

        }

		protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
            return await CommunityFollowProvider.RemoveFollowAsync(id);
        }
	}
}

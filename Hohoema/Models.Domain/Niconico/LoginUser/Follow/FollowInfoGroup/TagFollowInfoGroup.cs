using Mntone.Nico2;
using Mntone.Nico2.Users.Follow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.LoginUser.Follow
{
	public class TagFollowInfoGroup : FollowInfoGroupBaseTemplate<FollowTagsResponse.Tag>
	{
		public TagFollowInfoGroup(
            NiconicoSession niconicoSession, 
            TagFollowProvider tagFollowProvider
            )
        {
            NiconicoSession = niconicoSession;
            TagFollowProvider = tagFollowProvider;
        }

		public override FollowItemType FollowItemType => FollowItemType.Tag;

		public override uint MaxFollowItemCount =>
            NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_TAG_MAX_COUNT : FollowManager.FOLLOW_TAG_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public TagFollowProvider TagFollowProvider { get; }

        protected override FollowItemInfo ConvertToFollowInfo(FollowTagsResponse.Tag source)
		{
			return new FollowItemInfo()
			{
				Id = source.Name,
				Name = source.Name,
				FollowItemType = FollowItemType
			};
		}

		protected override string FollowSourceToItemId(FollowTagsResponse.Tag source)
		{
			return source.Name;
		}

		protected override async Task<List<FollowTagsResponse.Tag>> GetFollowSource()
		{
            return await TagFollowProvider.GetAllAsync();
        }

		protected override async Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
            return await TagFollowProvider.AddFollowAsync(id);
        }

        protected override async Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
            return await TagFollowProvider.RemoveFollowAsync(id);
        }


	}
}

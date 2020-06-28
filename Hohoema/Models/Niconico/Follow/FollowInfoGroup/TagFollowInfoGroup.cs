using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
	public class TagFollowInfoGroup : FollowInfoGroupBaseTemplate<string>
	{
		public TagFollowInfoGroup(
            NiconicoSession niconicoSession, 
            Provider.TagFollowProvider tagFollowProvider
            )
        {
            NiconicoSession = niconicoSession;
            TagFollowProvider = tagFollowProvider;
        }

		public override FollowItemType FollowItemType => FollowItemType.Tag;

		public override uint MaxFollowItemCount =>
            NiconicoSession.IsPremiumAccount ? FollowManager.PREMIUM_FOLLOW_TAG_MAX_COUNT : FollowManager.FOLLOW_TAG_MAX_COUNT;

        public NiconicoSession NiconicoSession { get; }
        public Provider.TagFollowProvider TagFollowProvider { get; }

        protected override FollowItemInfo ConvertToFollowInfo(string source)
		{
			return new FollowItemInfo()
			{
				Id = source,
				Name = source,
				FollowItemType = FollowItemType
			};
		}

		protected override string FollowSourceToItemId(string source)
		{
			return source;
		}

		protected override async Task<List<string>> GetFollowSource()
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

using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class TagFollowInfoGroup : FollowInfoGroupBaseTemplate<string>
	{
		public TagFollowInfoGroup(HohoemaApp hohoemaApp)
			: base(hohoemaApp)
		{
		}

		public override FollowItemType FollowItemType => FollowItemType.Tag;

		public override uint MaxFollowItemCount =>
			HohoemaApp.IsPremiumUser ? FollowManager.PREMIUM_FOLLOW_TAG_MAX_COUNT : FollowManager.FOLLOW_TAG_MAX_COUNT;

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

		protected override Task<List<string>> GetFollowSource()
		{
			return HohoemaApp.ContentProvider.GetFavTags();
		}



		protected override Task<ContentManageResult> AddFollow_Internal(string id, object token)
		{
			return HohoemaApp.NiconicoContext.User.AddFollowTagAsync(id);
		}
		protected override Task<ContentManageResult> RemoveFollow_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.RemoveFollowTagAsync(id);
		}


	}
}

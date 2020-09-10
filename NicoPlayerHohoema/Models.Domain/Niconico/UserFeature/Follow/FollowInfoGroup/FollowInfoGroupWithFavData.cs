using Mntone.Nico2.Users.Follow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.UserFeature.Follow
{
	public abstract class FollowInfoGroupWithFollowData : FollowInfoGroupBaseTemplate<FollowData>
	{

		public FollowInfoGroupWithFollowData()
		{
		}
		protected override FollowItemInfo ConvertToFollowInfo(FollowData source)
		{
			return new FollowItemInfo()
			{
				Id = source.ItemId,
				FollowItemType = FollowItemType,
				Name = source.Title,
			};
		}

		protected override string FollowSourceToItemId(FollowData source)
		{
			return source.ItemId;
		}

	}
}

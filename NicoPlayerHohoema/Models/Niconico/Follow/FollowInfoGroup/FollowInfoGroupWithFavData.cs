using Mntone.Nico2.Users.Follow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public abstract class FollowInfoGroupWithFollowData : FollowInfoGroupBaseTemplate<FollowData>
	{

		public FollowInfoGroupWithFollowData(HohoemaApp hohoemaApp)
			: base(hohoemaApp)
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

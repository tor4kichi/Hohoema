using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{

	public enum FollowItemType
	{
		Tag,
		Mylist,
		User,
		Community,
	}


	public static class FollowItemTypeExtention
	{
		public static NiconicoItemType? ToNiconicoItemType(this FollowItemType favorite)
		{
			switch (favorite)
			{
				case FollowItemType.Tag:
					return null;
				case FollowItemType.Mylist:
					return NiconicoItemType.Mylist;					
				case FollowItemType.User:
					return NiconicoItemType.User;
				case FollowItemType.Community:
					return null;
				default:
					throw new NotSupportedException();
			}
		}

		public static FollowItemType FromNiconicoItemType(NiconicoItemType? niconicoItemType)
		{
			if (niconicoItemType.HasValue)
			{
				switch (niconicoItemType.Value)
				{
					case NiconicoItemType.Mylist:
						return FollowItemType.Mylist;

					case NiconicoItemType.User:
						return FollowItemType.User;
					default:
						throw new NotSupportedException();
				}
			}
			else
			{
				return FollowItemType.Tag;
			}
		}
	}
}

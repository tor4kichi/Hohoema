using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{

	public enum FavoriteItemType
	{
		Tag,
		Mylist,
		User,
	}


	public static class FavoriteItemTypeExtention
	{
		public static NiconicoItemType? ToNiconicoItemType(this FavoriteItemType favorite)
		{
			switch (favorite)
			{
				case FavoriteItemType.Tag:
					return null;
				case FavoriteItemType.Mylist:
					return NiconicoItemType.Mylist;					
				case FavoriteItemType.User:
					return NiconicoItemType.User;
				default:
					throw new NotSupportedException();
			}
		}

		public static FavoriteItemType FromNiconicoItemType(NiconicoItemType? niconicoItemType)
		{
			if (niconicoItemType.HasValue)
			{
				switch (niconicoItemType.Value)
				{
					case NiconicoItemType.Mylist:
						return FavoriteItemType.Mylist;

					case NiconicoItemType.User:
						return FavoriteItemType.User;
					default:
						throw new NotSupportedException();
				}
			}
			else
			{
				return FavoriteItemType.Tag;
			}
		}
	}
}

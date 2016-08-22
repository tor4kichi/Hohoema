using Mntone.Nico2.Users.Fav;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	abstract public class FavInfoGroupWithFavData : FavInfoGroupBaseTemplate<FavData>
	{

		public FavInfoGroupWithFavData(HohoemaApp hohoemaApp, List<FavInfo> items)
			: base(hohoemaApp, items)
		{
		}
		protected override FavInfo ConvertToFavInfo(FavData source)
		{
			return new FavInfo()
			{
				Id = source.ItemId,
				FavoriteItemType = FavoriteItemType,
				Name = source.Title,
				FeedSource = FeedSource.Account,
			};
		}

		protected override string FavSourceToItemId(FavData source)
		{
			return source.ItemId;
		}

	}
}

using Mntone.Nico2;
using Mntone.Nico2.Users.Fav;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class MylistFavInfoGroup : FavInfoGroupWithFavData
	{
		public MylistFavInfoGroup(HohoemaApp hohoemaApp, List<FavInfo> items)
			: base(hohoemaApp, items)
		{

		}

		public override FavoriteItemType FavoriteItemType => FavoriteItemType.Mylist;


		public override bool CanMoreAddFavorite()
		{
			if (HohoemaApp.IsPremiumUser)
			{
				return FavInfoItems.Count < FavManager.PREMIUM_FAV_MYLIST_MAX_COUNT;
			}
			else
			{
				return FavInfoItems.Count < FavManager.FAV_MYLIST_MAX_COUNT;
			}
		}

		protected override Task<List<FavData>> GetFavSource()
		{
			return HohoemaApp.ContentFinder.GetFavMylists();
		}
		protected override Task<ContentManageResult> AddFav_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.AddUserFavAsync(NiconicoItemType.Mylist, id);
		}
		protected override Task<ContentManageResult> RemoveFav_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.RemoveUserFavAsync(NiconicoItemType.Mylist, id);
		}
	}
}

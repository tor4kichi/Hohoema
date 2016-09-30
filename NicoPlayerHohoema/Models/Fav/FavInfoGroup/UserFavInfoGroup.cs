using Mntone.Nico2;
using Mntone.Nico2.Users.Fav;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class UserFavInfoGroup : FavInfoGroupWithFavData
	{
		public UserFavInfoGroup(HohoemaApp hohoemaApp)
			: base(hohoemaApp)
		{

		}

		public override FavoriteItemType FavoriteItemType => FavoriteItemType.User;

		public override uint MaxFavItemCount =>
			HohoemaApp.IsPremiumUser ? FavManager.PREMIUM_FAV_USER_MAX_COUNT : FavManager.FAV_USER_MAX_COUNT;

		protected override Task<List<FavData>> GetFavSource()
		{
			return HohoemaApp.ContentFinder.GetFavUsers();
		}

		protected override Task<ContentManageResult> AddFav_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.AddUserFavAsync(NiconicoItemType.User, id);
		}
		protected override Task<ContentManageResult> RemoveFav_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.RemoveUserFavAsync(NiconicoItemType.User, id);
		}
	}
}

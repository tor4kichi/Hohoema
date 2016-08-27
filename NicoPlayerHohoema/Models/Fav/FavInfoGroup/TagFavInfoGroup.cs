using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class TagFavInfoGroup : FavInfoGroupBaseTemplate<string>
	{
		public TagFavInfoGroup(HohoemaApp hohoemaApp, List<FavInfo> items)
			: base(hohoemaApp, items)
		{
		}

		public override FavoriteItemType FavoriteItemType => FavoriteItemType.Tag;


		public override bool CanMoreAddFavorite()
		{
			if (HohoemaApp.IsPremiumUser)
			{
				return FavInfoItems.Count < FavManager.PREMIUM_FAV_TAG_MAX_COUNT;
			}
			else
			{
				return FavInfoItems.Count < FavManager.FAV_TAG_MAX_COUNT;
			}
		}

		protected override FavInfo ConvertToFavInfo(string source)
		{
			return new FavInfo()
			{
				Id = source,
				Name = source,
				FavoriteItemType = FavoriteItemType
			};
		}

		protected override string FavSourceToItemId(string source)
		{
			return source;
		}

		protected override Task<List<string>> GetFavSource()
		{
			return HohoemaApp.ContentFinder.GetFavTags();
		}



		protected override Task<ContentManageResult> AddFav_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.AddFavTagAsync(id);
		}
		protected override Task<ContentManageResult> RemoveFav_Internal(string id)
		{
			return HohoemaApp.NiconicoContext.User.RemoveFavTagAsync(id);
		}


	}
}

using Mntone.Nico2.Users.FavCommunity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2;

namespace NicoPlayerHohoema.Models
{
	public class CommunityFavInfoGroup : FavInfoGroupBaseTemplate<FavCommunityInfo>
	{
		public CommunityFavInfoGroup(HohoemaApp hohoemaApp) 
			: base(hohoemaApp)
		{

		}

		public override FavoriteItemType FavoriteItemType => FavoriteItemType.Community;

		public override uint MaxFavItemCount =>
			HohoemaApp.IsPremiumUser ? FavManager.PREMIUM_FAV_COMMUNITY_MAX_COUNT : FavManager.FAV_COMMUNITY_MAX_COUNT;


		protected override FavInfo ConvertToFavInfo(FavCommunityInfo source)
		{
			return new FavInfo()
			{
				FavoriteItemType = FavoriteItemType,
				Name = source.CommunityName,
				Id = source.CommunityId,
			};
		}

		protected override string FavSourceToItemId(FavCommunityInfo source)
		{
			return source.CommunityId;
		}

		protected override async Task<List<FavCommunityInfo>> GetFavSource()
		{
			var res = await HohoemaApp.ContentFinder.GetFavCommunities();
			return res.Items;
		}


		protected override Task<ContentManageResult> AddFav_Internal(string id)
		{
			throw new NotImplementedException();
		}

		protected override Task<ContentManageResult> RemoveFav_Internal(string id)
		{
			throw new NotImplementedException();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class FavAppMapContainer : SelectableAppMapContainerBase
	{

		public FavManager FavManager { get; private set; }

		public FavAppMapContainer(FavManager favManager)
			: base(HohoemaPageType.FavoriteManage, label:"お気に入り")
		{
			FavManager = favManager;
		}

		protected override Task<IEnumerable<IAppMapItem>> MakeAllItems()
		{
			// TODO: FavManagerを最新の情報に更新


			List<IAppMapItem> items = new List<IAppMapItem>();

			var userFavItems = FavManager.User.FavInfoItems;
			var mylistFavItems = FavManager.Mylist.FavInfoItems;
			var tagFavItems = FavManager.Tag.FavInfoItems;

			var allFavItems = userFavItems
				.Union(mylistFavItems)
				.Union(tagFavItems);

			foreach (var fav in allFavItems)
			{
				var favAppMapItem = new FavAppMapItem(fav);
				items.Add(favAppMapItem);
			}

			return Task.FromResult(items.AsEnumerable());
		}
	}

	public class FavAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; private set; }

		public HohoemaPageType PageType { get; private set; }
		public string Parameter { get; private set; }

		public FavoriteItemType FavType { get; private set; }

		public FavAppMapItem(FavInfo favInfo)
		{
			PrimaryLabel = favInfo.Name;
			SecondaryLabel = favInfo.FavoriteItemType.ToString();
			FavType = favInfo.FavoriteItemType;
			switch (FavType)
			{
				case FavoriteItemType.Tag:
					PageType = HohoemaPageType.Search;
					Parameter = new SearchOption()
					{
						Keyword = favInfo.Id,
						SearchTarget = SearchTarget.Tag,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					}
					.ToParameterString();
					break;
				case FavoriteItemType.Mylist:
					PageType = HohoemaPageType.Mylist;
					Parameter = favInfo.Id;
					break;
				case FavoriteItemType.User:
					PageType = HohoemaPageType.UserVideo;
					Parameter = favInfo.Id;
					break;
				default:
					throw new Exception();
			}
		}
	}
}

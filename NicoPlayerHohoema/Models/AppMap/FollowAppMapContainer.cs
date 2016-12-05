using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class FollowAppMapContainer : SelectableAppMapContainerBase
	{

		public FollowManager FollowManager { get; private set; }

		public FollowAppMapContainer(FollowManager FollowManager)
			: base(HohoemaPageType.FollowManage, label:"フォロー")
		{
			this.FollowManager = FollowManager;
		}


		public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Card;


		protected override async Task<IEnumerable<IAppMapItem>> MakeAllItems()
		{
			// TODO: FavManagerを最新の情報に更新
			

			List<IAppMapItem> items = new List<IAppMapItem>();


			if (FollowManager?.User == null) { return Enumerable.Empty<IAppMapItem>(); }

			var userFavItems = FollowManager.User.FollowInfoItems;
			var mylistFavItems = FollowManager.Mylist.FollowInfoItems;
			var tagFavItems = FollowManager.Tag.FollowInfoItems;

			var allFavItems = userFavItems
				.Union(mylistFavItems)
				.Union(tagFavItems);

			foreach (var fav in allFavItems)
			{
				var favAppMapItem = new FollowAppMapItem(fav);
				items.Add(favAppMapItem);
			}

			return items.AsEnumerable();
		}
	}

	public class FollowAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; private set; }

		public HohoemaPageType PageType { get; private set; }
		public string Parameter { get; private set; }

		public FollowItemType FollowItemType { get; private set; }

		public FollowAppMapItem(FollowItemInfo followInfo)
		{
			PrimaryLabel = followInfo.Name;
			SecondaryLabel = followInfo.FollowItemType.ToString();
			FollowItemType = followInfo.FollowItemType;
			switch (FollowItemType)
			{
				case FollowItemType.Tag:
					PageType = HohoemaPageType.SearchResultTag;
					Parameter = new TagSearchPagePayloadContent()
					{
						Keyword = followInfo.Id,
						Sort = Mntone.Nico2.Sort.FirstRetrieve,
						Order = Mntone.Nico2.Order.Descending
					}
					.ToParameterString();
					break;
				case FollowItemType.Mylist:
					PageType = HohoemaPageType.Mylist;
					Parameter = followInfo.Id;
					break;
				case FollowItemType.User:
					PageType = HohoemaPageType.UserVideo;
					Parameter = followInfo.Id;
					break;
				default:
					throw new Exception();
			}
		}
	}
}

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

		public FollowAppMapContainer()
			: base(HohoemaPageType.FollowManage, label:"フォロー")
		{
			this.FollowManager = HohoemaApp.FollowManager;
		}


		public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Card;


        public override Task Refresh()
        {
            foreach (var item in AllItems.ToArray())
            {
                Add(item);
            }
            return base.Refresh();
        }

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

	public class FollowAppMapItem : AppMapItemBase
	{
		public FollowItemType FollowItemType { get; private set; }

        public FollowItemInfo Info { get; private set; }

        public FollowAppMapItem(FollowItemInfo followInfo)
		{
			PrimaryLabel = followInfo.Name;
			SecondaryLabel = followInfo.FollowItemType.ToString();
			FollowItemType = followInfo.FollowItemType;
			
		}


        public override void SelectedAction()
        {
            HohoemaPageType pageType;
            string parameter;
            switch (FollowItemType)
            {
                case FollowItemType.Tag:
                    pageType = HohoemaPageType.SearchResultTag;
                    parameter = new TagSearchPagePayloadContent()
                    {
                        Keyword = Info.Id,
                        Sort = Mntone.Nico2.Sort.FirstRetrieve,
                        Order = Mntone.Nico2.Order.Descending
                    }
                    .ToParameterString();
                    break;
                case FollowItemType.Mylist:
                    pageType = HohoemaPageType.Mylist;
                    parameter = Info.Id;
                    break;
                case FollowItemType.User:
                    pageType = HohoemaPageType.UserVideo;
                    parameter = Info.Id;
                    break;
                default:
                    throw new Exception();
            }

            PageManager.OpenPage(pageType, parameter);
        }
    }
}

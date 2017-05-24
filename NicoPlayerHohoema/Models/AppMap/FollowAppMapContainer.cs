using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class FollowAppMapContainer : AppMapContainerBase
    {
        // TODO: FollowManagrの初期化待ち
        // TODO: フォローアイテムの変更をイベントで受け取りたい

		public FollowManager FollowManager { get; private set; }

		public FollowAppMapContainer()
			: base(HohoemaPageType.FollowManage, label:"フォロー")
		{
            HohoemaApp.OnSignin += HohoemaApp_OnSignin;
        }

        private void HohoemaApp_OnSignin()
        {
            this.FollowManager = HohoemaApp.FollowManager;
            FollowManager.Completed += FollowManager_Completed;
        }

        private async void FollowManager_Completed(object sender)
        {
            await Refresh();
        }

        public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Card;


        protected override Task OnRefreshing()
        {
            _DisplayItems.Clear();

            List<IAppMapItem> items = new List<IAppMapItem>();


            if (FollowManager?.User == null) { return Task.CompletedTask; }

            var userFavItems = FollowManager.User.FollowInfoItems;
            var mylistFavItems = FollowManager.Mylist.FollowInfoItems;
            var tagFavItems = FollowManager.Tag.FollowInfoItems;

            var allFavItems = userFavItems
                .Union(mylistFavItems)
                .Union(tagFavItems);

            foreach (var fav in allFavItems)
            {
                var favAppMapItem = new FollowAppMapItem(fav);
                _DisplayItems.Add(favAppMapItem);
            }

            return Task.CompletedTask;
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
            Info = followInfo;

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

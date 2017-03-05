using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class UserMylistAppMapContainer : AppMapContainerBase
    {
		public UserMylistManager UserMylistManager { get; private set; }

		public UserMylistAppMapContainer()
			: base(HohoemaPageType.UserMylist, label:"マイリスト")
		{
			UserMylistManager = HohoemaApp.UserMylistManager;
            UserMylistManager.Completed += UserMylistManager_Completed;
        }

        private async void UserMylistManager_Completed(object sender)
        {
            await Refresh();
        }

        public override ContainerItemDisplayType ItemDisplayType => ContainerItemDisplayType.Card;

        protected override Task OnRefreshing()
        {
            _DisplayItems.Clear();

            var mylists = UserMylistManager.UserMylists.ToArray();
            var items = new List<IAppMapItem>();

            foreach (var mylist in mylists)
            {
                var item = new MylistAppMapContainer(mylist);
                _DisplayItems.Add(item);
            }

            return Task.CompletedTask;
        }

       
	}

	public class MylistAppMapContainer : AppMapContainerBase
    {
		public MylistGroupInfo MylistGroupInfo { get; private set; }

		public MylistAppMapContainer(MylistGroupInfo mylistGroup)
			: base(HohoemaPageType.Mylist, mylistGroup.GroupId, mylistGroup.Name)
		{
			MylistGroupInfo = mylistGroup;
		}

        protected override Task OnRefreshing()
        {
            // do nothing

            return Task.CompletedTask;
        }

      
	}
	/*
	public class MylistVideoAppMapItem : IAppMapItem
	{
		public MylistVideoAppMapItem(string videoId)
		{

		}
	}
	*/
}

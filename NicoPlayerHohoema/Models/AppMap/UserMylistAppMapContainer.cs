using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class UserMylistAppMapContainer : SelectableAppMapContainerBase
	{
		public UserMylistManager UserMylistManager { get; private set; }

		public UserMylistAppMapContainer(UserMylistManager mylistManager)
			: base(HohoemaPageType.UserMylist, label:"マイリスト")
		{
			UserMylistManager = mylistManager;
		}

		protected override Task<IEnumerable<IAppMapItem>> MakeAllItems()
		{
			var mylists = UserMylistManager.UserMylists.ToArray();
			var items = new List<IAppMapItem>();

			foreach (var mylist in mylists)
			{
				var item = new MylistAppMapContainer(mylist);
				items.Add(item);
			}

			return Task.FromResult(items.AsEnumerable());
		}
	}

	public class MylistAppMapContainer : SelfGenerateAppMapContainerBase
	{
		public MylistGroupInfo MylistGroupInfo { get; private set; }

		public MylistAppMapContainer(MylistGroupInfo mylistGroup)
			: base(HohoemaPageType.Mylist, mylistGroup.GroupId, mylistGroup.Name)
		{
			MylistGroupInfo = mylistGroup;
		}

		protected override Task<IEnumerable<IAppMapItem>> GenerateItems(int count)
		{
			var items = new List<IAppMapItem>();

//			foreach (var mylistItem in MylistGroupInfo.VideoItems)
//			{
//				items.Add
//			}

			return Task.FromResult(items.AsEnumerable());
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	public class MylistFavFeedList : FavFeedList
	{
		public MylistFavFeedList(string groupdId, StorageFolder saveFolder)
			: base(groupdId)
		{

		}




		#region override FavFeedList

		public override FavoriteItemType FavItemType => FavoriteItemType.Mylist;

		#endregion

		public Task Initialize()
		{
			return Task.CompletedTask;
		}


		// Update
		public Task Refresh()
		{
			return Task.CompletedTask;
		}

		/*
		*/

		/*
			public async Task SaveMylistFavFeedLists()
		{
			foreach (var list in GetFavMylistFeedListAll())
			{
				await SaveFavFeedList(list);
			}
		} 
		*/

		// Save

		public Task Save()
		{
			return Task.CompletedTask;
		}
	}
}

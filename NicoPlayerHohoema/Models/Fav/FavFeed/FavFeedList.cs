using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	abstract public class FavFeedList : BindableBase
	{
		public string Id { get; private set; }

		public List<FavFeedItem> FeedItems { get; private set; }

		public FavFeedList(string id)
		{
			Id = id;

			FeedItems = new List<FavFeedItem>();
		}


		abstract public FavoriteItemType FavItemType { get; }

	}
}

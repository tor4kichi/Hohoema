using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class FavoriteVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public FavoriteVideoInfoControlViewModel(FavFeedItem feedItem, NGSettings ngSettings, NiconicoMediaManager mediaMan, PageManager pageMan)
			: base(feedItem.Title, feedItem.VideoId, ngSettings, mediaMan, pageMan)
		{
			IsNewItem = feedItem.IsNewItem;
		}



		private bool _IsNewItem;
		public bool IsNewItem
		{
			get { return _IsNewItem; }
			set { SetProperty(ref _IsNewItem, value); }
		}
	}

}

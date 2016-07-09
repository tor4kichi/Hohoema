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
		public FavoriteVideoInfoControlViewModel(FavFeedItem feedItem, NicoVideo nicoVideo, PageManager pageMan)
			: base(nicoVideo, pageMan)
		{
			IsNewItem = feedItem.IsUnread;
		}



		private bool _IsNewItem;
		public bool IsNewItem
		{
			get { return _IsNewItem; }
			set { SetProperty(ref _IsNewItem, value); }
		}
	}

}

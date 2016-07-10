using NicoPlayerHohoema.Models;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
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
			IsUnread = feedItem.ToReactivePropertyAsSynchronized(x => x.IsUnread);
		}


		public ReactiveProperty<bool> IsUnread { get; private set; }

	}

}

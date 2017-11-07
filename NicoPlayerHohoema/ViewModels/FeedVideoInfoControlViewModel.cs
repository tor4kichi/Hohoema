using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
	public class FeedVideoInfoControlViewModel : VideoInfoControlViewModel
	{
		public FeedVideoInfoControlViewModel(FeedItem feedItem, IFeedGroup feedGroup)
			: base(feedItem.VideoId)
		{
			IsUnread = feedItem.ToReactivePropertyAsSynchronized(x => x.IsUnread)
				.AddTo(_CompositeDisposable);

			_FafFeedItem = feedItem;
			_FeedGroup = feedGroup;
		}

		FeedItem _FafFeedItem;
		IFeedGroup _FeedGroup;

		public ReactiveProperty<bool> IsUnread { get; private set; }
		public FollowItemType SourceType { get; private set; }
		public string SourceTitle { get; private set; }
	}

}

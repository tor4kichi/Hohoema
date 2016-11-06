using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class FeedAppMapContainer : SelectableAppMapContainerBase
	{
		public FeedManager FeedManager { get; private set; }

		public FeedAppMapContainer(FeedManager feedManager)
			: base(HohoemaPageType.FeedGroupManage, label:"フィード")
		{
			FeedManager = feedManager;
		}

		protected override Task<IEnumerable<IAppMapItem>> MakeAllItems()
		{
			var items = new List<IAppMapItem>();
			var feedGroups = FeedManager.FeedGroups;
			foreach (var feedGroup in feedGroups)
			{
				var feedGroupContainer = new FeedGroupAppMapContainer(feedGroup);
				items.Add(feedGroupContainer);
			}
			return Task.FromResult(items.AsEnumerable());
		}
	}


	public class FeedGroupAppMapContainer : SelfGenerateAppMapContainerBase
	{
		public IFeedGroup FeedGroup { get; private set; }

		public FeedGroupAppMapContainer(IFeedGroup group)
			: base(HohoemaPageType.FeedVideoList, parameter:group.Id.ToString(), label:group.Label)
		{
			FeedGroup = group;
			SecondaryLabel = FeedGroup.GetUnreadItemCount().ToString();
		}

		protected override Task<IEnumerable<IAppMapItem>> GenerateItems(int count)
		{
			var feedItems = FeedGroup.FeedItems.Take(count);
			var items = new List<IAppMapItem>();
			foreach (var feedItem in feedItems)
			{
				var item = new FeedAppMapItem(feedItem);
				items.Add(item);
			}

			return Task.FromResult(items.AsEnumerable());
		}
	}

	public class FeedAppMapItem : IAppMapItem
	{
		public string PrimaryLabel { get; private set; }
		public string SecondaryLabel { get; protected set; }
		public HohoemaPageType PageType { get; private set; }
		public string Parameter { get; private set; }

		public FeedAppMapItem(FeedItem feedItem)
		{
			PrimaryLabel = feedItem.Title;
			SecondaryLabel = feedItem.IsUnread ? "New" : null;
			PageType = HohoemaPageType.VideoPlayer;

			Parameter = new VideoPlayPayload()
			{
				VideoId = feedItem.VideoId
			}
			.ToParameterString();

		}
	}
}

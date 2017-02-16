using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.AppMap
{
	public class FeedAppMapContainer : AppMapContainerBase
    {
		public FeedManager FeedManager { get; private set; }

		public FeedAppMapContainer()
			: base(HohoemaPageType.FeedGroupManage, label:"フィード")
		{
			FeedManager = HohoemaApp.FeedManager;
		}


        protected override Task OnRefreshing()
        {
            _DisplayItems.Clear();

            
            var items = new List<IAppMapItem>();
            var feedGroups = FeedManager.FeedGroups;
            foreach (var feedGroup in feedGroups)
            {
                var feedGroupContainer = new FeedGroupAppMapContainer(feedGroup);
                _DisplayItems.Add(feedGroupContainer);
            }

            return Task.CompletedTask;
        }
        
	}


	public class FeedGroupAppMapContainer : AppMapContainerBase
    {
        public const int FeedItemDisplayCount = 3;

		public IFeedGroup FeedGroup { get; private set; }

		public FeedGroupAppMapContainer(IFeedGroup group)
			: base(HohoemaPageType.FeedVideoList, parameter:group.Id.ToString(), label:group.Label)
		{
			FeedGroup = group;
			SecondaryLabel = FeedGroup.GetUnreadItemCount().ToString();
        }


        protected override Task OnRefreshing()
        {
            _DisplayItems.Clear();

            var feedItems = FeedGroup.FeedItems.Take(FeedItemDisplayCount);
            foreach (var feedItem in feedItems)
            {
                var item = new FeedAppMapItem(feedItem);
                _DisplayItems.Add(item);
            }

            return Task.CompletedTask;
        }
	}

	public class FeedAppMapItem : VideoAppMapItemBase
    {
        public FeedAppMapItem(FeedItem feedItem)
		{
            PrimaryLabel = feedItem.Title;
			SecondaryLabel = feedItem.IsUnread ? "New" : null;
            Parameter = feedItem.VideoId;
		}
	}
}

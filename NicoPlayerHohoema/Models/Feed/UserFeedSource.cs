using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class UserFeedSource : FeedSource
	{
		public UserFeedSource(string name, string userId)
			: base(name, userId)
		{

		}

		public override FollowItemType FollowItemType => FollowItemType.User;

		public override async Task<IEnumerable<FeedItem>> GetLatestItems(HohoemaApp hohoemaApp)
		{
			var items = await hohoemaApp.ContentProvider.GetUserVideos(uint.Parse(this.Id), 1);

			if (items?.Items != null)
			{
				var feedItems = items.Items.Select(x =>
					new FeedItem()
					{
						Title = x.Title,
						VideoId = x.VideoId,
						SubmitDate = x.SubmitTime,
					})
					.ToList();

/*
				foreach (var item in feedItems)
				{
					var nicoVideo = await hohoemaApp.MediaManager.GetNicoVideo(item.VideoId);
					if (nicoVideo != null)
					{
						var thumbnail = await nicoVideo.GetThumbnailResponse();
						item.SubmitDate = thumbnail?.PostedAt.DateTime ?? DateTime.MinValue;
						item.IsDeleted = nicoVideo.IsDeleted;
					}
				}
				*/
				return feedItems;
			}
			else
			{
				return new List<FeedItem>();
			}
		}

	}
}

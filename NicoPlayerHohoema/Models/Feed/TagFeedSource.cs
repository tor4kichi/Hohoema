using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class TagFeedSource : FeedSource
	{
		public TagFeedSource(string tag)
			: base(tag, tag)
		{

		}

		public override FollowItemType FollowItemType => FollowItemType.Tag;

		public override async Task<IEnumerable<FeedItem>> GetLatestItems(HohoemaApp hohoemaApp)
		{
			var items = await hohoemaApp.ContentProvider.GetTagSearch(this.Id, 0, 32);

			if (items?.VideoInfoItems != null)
			{
				return items.VideoInfoItems.Select(x =>
					new FeedItem()
					{
						Title = x.Video.Title,
						VideoId = x.Video.Id,
						CheckedTime = DateTime.MinValue,
						SubmitDate = x.Video.FirstRetrieve,
						IsDeleted = x.Video.IsDeleted,
					})
					.ToList();
			}
			else
			{
				return new List<FeedItem>();
			}
		}

	}
}

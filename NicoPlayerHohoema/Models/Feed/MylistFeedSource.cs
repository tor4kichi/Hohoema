using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class MylistFeedSource : FeedSource
	{
		public MylistFeedSource(string name, string groupdId)
			: base(name, groupdId)
		{

		}

		public override FollowItemType FollowItemType => FollowItemType.Mylist;

		public override async Task<IEnumerable<FeedItem>> GetLatestItems(HohoemaApp hohoemaApp)
		{
			var items = await hohoemaApp.ContentProvider.GetMylistGroupVideo(this.Id, 0, 32);

			if (items?.MylistVideoInfoItems != null)
			{
				return items.MylistVideoInfoItems.Select(x =>
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

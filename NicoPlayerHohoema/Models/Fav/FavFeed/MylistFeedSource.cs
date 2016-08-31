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

		public override FavoriteItemType FavoriteItemType => FavoriteItemType.Mylist;

		public override async Task<IEnumerable<FavFeedItem>> GetLatestItems(HohoemaApp hohoemaApp)
		{
			var items = await hohoemaApp.ContentFinder.GetMylistItems(this.Id, 0, 32);

			if (items?.Video_info != null)
			{
				return items.Video_info.Select(x =>
					new FavFeedItem()
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
				return new List<FavFeedItem>();
			}
		}

	}
}

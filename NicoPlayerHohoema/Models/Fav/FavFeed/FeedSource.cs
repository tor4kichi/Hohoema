using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	abstract public class FeedSource : IFeedSource
	{
		public FeedSource(string name, string id)
		{
			Id = id;
			Name = name;
		}

		[DataMember(Name = "id")]
		public string Id { get; set; }


		[DataMember(Name = "name")]
		public string Name { get; set; }

		abstract public FavoriteItemType FavoriteItemType { get; }

		abstract public Task<IEnumerable<FavFeedItem>> GetLatestItems(HohoemaApp hohoemaApp);

	}
}

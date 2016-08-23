using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class FavFeedItemComparer : IEqualityComparer<FavFeedItem>
	{
		public static FavFeedItemComparer Default = new FavFeedItemComparer();

		public bool Equals(FavFeedItem x, FavFeedItem y)
		{
			return x.VideoId == y.VideoId;
		}

		public int GetHashCode(FavFeedItem obj)
		{
			return obj.VideoId.GetHashCode();
		}
	}
}

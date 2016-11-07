using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class FeedItemComparer : IEqualityComparer<FeedItem>, IComparer<FeedItem>
	{
		public static FeedItemComparer Default = new FeedItemComparer();

		public int Compare(FeedItem x, FeedItem y)
		{
			return (int)y.SubmitDate.Subtract(x.SubmitDate).TotalMinutes;
		}

		public bool Equals(FeedItem x, FeedItem y)
		{
			return x.VideoId == y.VideoId;
		}

		public int GetHashCode(FeedItem obj)
		{
			return obj.VideoId.GetHashCode();
		}
	}
}

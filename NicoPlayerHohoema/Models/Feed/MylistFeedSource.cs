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

	}
}

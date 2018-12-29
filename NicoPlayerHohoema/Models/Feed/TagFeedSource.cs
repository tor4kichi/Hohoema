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

	}
}

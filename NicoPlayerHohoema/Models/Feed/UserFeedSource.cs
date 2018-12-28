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

	}
}

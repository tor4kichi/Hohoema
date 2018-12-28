using System.Collections.Generic;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public interface IFeedSource
	{
		FollowItemType FollowItemType { get; }
		string Id { get; }
		string Name { get; set; }
	}
}
using Hohoema.Models.Niconico.Follow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Pages.PagePayload
{
	public class FollowPageParameter : PagePayloadBase<FollowPageParameter>
	{
		public string Id { get; set; }
		public FollowItemType ItemType { get; set; }
	}
}

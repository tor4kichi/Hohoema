using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Live
{

	public class LiveVideoPagePayload
	{
		public string LiveId { get; set; }

		public string LiveTitle { get; set; }
		public string CommunityId { get; set; }
		public string CommunityName { get; set; }

		public LiveVideoPagePayload(string liveId)
		{
			LiveId = liveId;
		}

		public string ToParameterString()
		{
			return JsonSerializer.Serialize(this);
		}

		public static LiveVideoPagePayload FromParameterString(string json)
		{
			return JsonSerializer.Deserialize<LiveVideoPagePayload>(json);
		}

	}
}

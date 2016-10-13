using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Live
{

	public class LiveVidePagePayload
	{
		public string LiveId { get; set; }

		public string LiveTitle { get; set; }
		public string CommunityId { get; set; }
		public string CommunityName { get; set; }

		public LiveVidePagePayload(string liveId)
		{
			LiveId = liveId;
		}

		public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public static LiveVidePagePayload FromParameterString(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<LiveVidePagePayload>(json);
		}

	}
}

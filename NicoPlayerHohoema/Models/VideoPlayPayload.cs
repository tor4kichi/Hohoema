using Mntone.Nico2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class VideoPlayPayload
	{
		public string VideoId { get; set; }
		public NicoVideoQuality? Quality { get; set; }

		public string ToParameterString()
		{
			return Newtonsoft.Json.JsonConvert.SerializeObject(this);
		}

		public static VideoPlayPayload FromParameterString(string json)
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<VideoPlayPayload>(json);
		}

	}
}

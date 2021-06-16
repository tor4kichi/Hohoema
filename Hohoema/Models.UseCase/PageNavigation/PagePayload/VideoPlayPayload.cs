using Hohoema.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.Video;
using System.Text.Json;
using NiconicoToolkit.Video;

namespace Hohoema.Models.UseCase.PageNavigation
{
	public class VideoPlayPayload
	{
		public VideoId VideoId { get; set; }
		public NicoVideoQuality? Quality { get; set; }

		public string ToParameterString()
		{
			return JsonSerializer.Serialize(this);
		}

		public static VideoPlayPayload FromParameterString(string json)
		{
			return JsonSerializer.Deserialize<VideoPlayPayload>(json);
		}

	}
}

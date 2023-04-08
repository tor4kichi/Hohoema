#nullable enable
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Video;
using System.Text.Json;

namespace Hohoema.Services.Navigations;

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

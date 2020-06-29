using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
	public enum MediaProtocolType
	{
		NotSupported,

		RTSPoverHTTP,
		RTMP,
		RTMPE,
	}

	public static class MediaProtocolTypeHelper
	{
		public static MediaProtocolType ParseMediaProtocolType(Uri uri)
		{
			switch (uri.Scheme)
			{
				case "rtmp":
					return MediaProtocolType.RTMP;
				case "rtmpe":
					return MediaProtocolType.RTMPE;
				case "http":
					return MediaProtocolType.RTSPoverHTTP;
				default:
					break;
			}

			return MediaProtocolType.NotSupported;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace NicoVideoRtmpClient
{
	public sealed class NicovideoRtmpClientStartedEventArgs
	{
		public MediaStreamSource MediaStreamSource { get; private set; }

		internal NicovideoRtmpClientStartedEventArgs(MediaStreamSource mediaStreamSource)
		{
			MediaStreamSource = mediaStreamSource;
		}
	}
}

using Mntone.Rtmp;
using Mntone.Rtmp.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoVideoRtmpClient
{
	public interface INicoVideoRtmpConnection
	{
		RtmpUri Uri { get; }
		NetConnectionConnectCommand Command { get; }
		Task PostConnectionProcess(NetConnection connection);
	}
}

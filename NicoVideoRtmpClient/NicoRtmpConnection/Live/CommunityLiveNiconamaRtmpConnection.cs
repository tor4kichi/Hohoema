using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;


namespace NicoVideoRtmpClient.NicoRtmpConnection.Live
{
	public class CommunityLiveNiconamaRtmpConnection : ChannelLiveNiconamaRtmpConnection
	{
		public CommunityLiveNiconamaRtmpConnection(PlayerStatusResponse res)
			: base(res)
		{
		}

	}
}

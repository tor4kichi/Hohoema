using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;

namespace NicoVideoRtmpClient.NicoRtmpConnection.Archive
{
	public class CommunityArchiveNiconamaRtmpConnection : NiconamaRtmpConnectionBase
	{
		public CommunityArchiveNiconamaRtmpConnection(PlayerStatusResponse res)
			: base(res)
		{
		}
	}
}

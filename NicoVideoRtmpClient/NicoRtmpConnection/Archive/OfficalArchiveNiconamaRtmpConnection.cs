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
	public class OfficalArchiveNiconamaRtmpConnection : NiconamaRtmpConnectionBase
	{
		public OfficalArchiveNiconamaRtmpConnection(PlayerStatusResponse res) : base(res)
		{
		}

		public override NetConnectionConnectCommand Command
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override Task PostConnectionProcess(NetConnection connection)
		{
			throw new NotImplementedException();
		}
	}

}

using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoVideoRtmpClient.NicoRtmpConnection
{
	abstract public class NiconamaRtmpConnectionBase : INicoVideoRtmpConnection
	{
		public PlayerStatusResponse PlayerStatus { get; private set; }

		public NiconamaRtmpConnectionBase(PlayerStatusResponse res)
		{
			PlayerStatus = res;
		}

		public virtual NetConnectionConnectCommand Command
		{
			get
			{
				var command = new NetConnectionConnectCommand(Uri.App);
				command.TcUrl = Uri.ToString();
				return command;
			}
		}

		public virtual RtmpUri Uri
		{
			get
			{
				return new RtmpUri(PlayerStatus.Stream.RtmpUrl);
			}
		}

		public virtual Task PostConnectionProcess(NetConnection connection)
		{
			return Task.CompletedTask;
		}



	}
}

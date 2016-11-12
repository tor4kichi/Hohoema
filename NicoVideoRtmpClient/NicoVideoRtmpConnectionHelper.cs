using Mntone.Nico2.Live.PlayerStatus;
using NicoVideoRtmpClient.NicoRtmpConnection.Archive;
using NicoVideoRtmpClient.NicoRtmpConnection.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoVideoRtmpClient
{
	public static class NicoVideoRtmpConnectionHelper
	{
		public static INicoVideoRtmpConnection MakeConnectionImpl(PlayerStatusResponse res)
		{
			var isLive = !res.Program.IsArchive;
			switch (res.Program.CommunityType)
			{
				case Mntone.Nico2.Live.CommunityType.Official:
					if (isLive)
					{
						return new OfficalLiveNiconamaRtmpConnection(res);
					}
					else
					{
						return new OfficalArchiveNiconamaRtmpConnection(res);
					}
				case Mntone.Nico2.Live.CommunityType.Channel:
					if (isLive)
					{
						return new ChannelLiveNiconamaRtmpConnection(res);
					}
					else
					{
						return new ChannelArchiveNiconamaRtmpConnection(res);
					}
				case Mntone.Nico2.Live.CommunityType.Community:
					if (isLive)
					{
						return new CommunityLiveNiconamaRtmpConnection(res);
					}
					else
					{
						return new CommunityArchiveNiconamaRtmpConnection(res);
					}
				default:
					break;
			}

			throw new NotSupportedException();
		}
	}
}

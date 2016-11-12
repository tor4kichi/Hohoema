using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;
using System.Net;

namespace NicoVideoRtmpClient.NicoRtmpConnection.Live
{
	public class OfficalLiveNiconamaRtmpConnection : NiconamaRtmpConnectionBase
	{
		public static readonly Uri OfficailNiconamaBaseUri = new Uri("rtmp://nlakmjpk.live.nicovideo.jp:1935/live");

		public static readonly Uri OfficialNiconamaBase = new Uri("rtmp://smilevideo.fc.llnwd.net:1935/smilevideo");

		public RtmpUri RtmpUri { get; private set; }
		public string BaseUri = null;
		public string sLiveId = "";


		class LiveContentInfo
		{
			public string PlayEnvType { get; set; }
			public string ServerType { get; set; }
			public string RtmpUri { get; set; }
			public string sLiveId { get; set; }
		}


		public OfficalLiveNiconamaRtmpConnection(PlayerStatusResponse res)
			: base(res)
		{
			// res.Stream.ContentsのValueに入ってる値
			// アニメなどの高画質放送
			// "case:sp:akamai%3Artmp%3A%2F%2Fnlakmjpk.live.nicovideo.jp%3A1935%2Flive%2Cnlarr_25%40s28582,mobile:akamai%3Artmp%3A%2F%2Fnlakmjpk.live.nicovideo.jp%3A1935%2Flive%2Cnlarr_26%40s18564,premium:akamai%3Artmp%3A%2F%2Fnlakmjpk.live.nicovideo.jp%3A1935%2Flive%2Cnlarr_26%40s18564,default:akamai%3Artmp%3A%2F%2Fnlakmjpk.live.nicovideo.jp%3A1935%2Flive%2Cnlarr_25%40s28582"
			// その他の公式放送
			// "case:sp:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv276840177,mobile:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv276840177_sub1,premium:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv276840177_sub1,default:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv276840177"

			var content = res.Stream.Contents.ElementAt(0);
			var splited = content.Value.Split(',');
			var decodedSplit = splited.Select(x => WebUtility.UrlDecode(x)).ToList();

			var contentInfoList = decodedSplit.Select(x =>
			{
				var uriFragments = x.Split(':');

				if (uriFragments.Length == 6)
				{
					uriFragments = uriFragments.Skip(1).ToArray();
				}

				if (uriFragments.Length == 5)
				{
					var type = uriFragments[0];
					var serverType = uriFragments[1];
					var uriAndLiveId = string.Join(":", uriFragments.Skip(2)).Split(',');
					var uri = uriAndLiveId[0];
					var sliveId = uriAndLiveId[1];

					return new LiveContentInfo()
					{
						PlayEnvType = type,
						ServerType = serverType,
						RtmpUri = uri,
						sLiveId = sliveId
					};
				}
				else
				{
					throw new Exception();
				}
			});

			var ticket = PlayerStatus.Stream.Tickets.ElementAt(0);
			var playpath = $"{ticket.Key}?{ticket.Value}";

			var contentInfo = contentInfoList.First();
			BaseUri = contentInfo.RtmpUri;
			sLiveId = contentInfo.sLiveId;

			RtmpUri = new RtmpUri(BaseUri);

			RtmpUri.Instance = playpath;
		}

		public override RtmpUri Uri
		{
			get
			{
				return RtmpUri;
			}
		}
	}
}

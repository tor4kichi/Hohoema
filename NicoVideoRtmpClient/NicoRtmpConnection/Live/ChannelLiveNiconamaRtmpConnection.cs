using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;
using Mntone.Data.Amf;


namespace NicoVideoRtmpClient.NicoRtmpConnection.Live
{
	public class ChannelLiveNiconamaRtmpConnection : NiconamaRtmpConnectionBase
	{
		private NetConnectionCallCommand _NlPlayNoticeCommand;


		public ChannelLiveNiconamaRtmpConnection(PlayerStatusResponse res) : base(res)
		{
			var uri = PlayerStatus.Stream.RtmpUrl.OriginalString;
			var rtmpUri = new RtmpUri(uri);
			var pp = uri.Split('/');

			rtmpUri.App = string.Join("/", pp[3], pp[4]);
			rtmpUri.Instance = PlayerStatus.Program.Id;
			_Uri = rtmpUri;

			_NlPlayNoticeCommand = MakeNLPlayNoticeCommand();

			_Command = new NetConnectionConnectCommand(Uri.App);
			_Command.SwfUrl = "http://live.nicovideo.jp/nicoliveplayer.swf?160530135720";
			_Command.PageUrl = "http://live.nicovideo.jp/watch/" + PlayerStatus.Program.Id;
			_Command.FlashVersion = "WIN 23,0,0,162";

			// TcUrl は RtmpUriのInstanceを省いた文字列
			_Command.TcUrl = $"{Uri.Scheme.ToString().ToLower()}://{Uri.Host}:{Uri.Port}/{Uri.App}";
			// Rtmpのconnectメソッドのextrasとして PlayerStatus.Stream.Ticket の値を追加
			_Command.OptionalUserArguments = AmfValue.CreateStringValue($"{PlayerStatus.Stream.Ticket}");

		}

		private NetConnectionConnectCommand _Command;
		public override NetConnectionConnectCommand Command => _Command;

		private RtmpUri _Uri;
		public override RtmpUri Uri
		{
			get
			{
				return _Uri;
			}
		}

		public override async Task PostConnectionProcess(NetConnection connection)
		{
			await connection.CallAsync(_NlPlayNoticeCommand);
		}


		protected NetConnectionCallCommand MakeNLPlayNoticeCommand()
		{
			var command = new NetConnectionCallCommand("nlPlayNotice");

			// TODO: コンテンツが生放送ではなくて動画IDになっている場合がある
			// smile:sm0000000
			var nlplaynoticeText = PlayerStatus.Stream.Contents[0].Value;

			if (nlplaynoticeText.StartsWith("smile:"))
			{
				throw new Exception("cant play with live player -> " + nlplaynoticeText);
			}
#if true
			var split = nlplaynoticeText.Split(',');
			var nlplaypath = split[0].Remove(0, "rtmp:".Length);
			var nltoken = split[1];

			var nlid = split[1].Split('?').ElementAt(0);
#else
			var split = nlplaynoticeText.Split(',', '?');
			var nlplaypath = split[0].Remove(0, "rtmp:".Length);
			var nlid = split[1];
			var nltoken = split[2];
#endif
			var nlplaynoticeParameter = new AmfArray();

			// 先頭にnullを入れる
			command.OptionalArguments.Add(new AmfValue());

			command.OptionalArguments.Add(AmfValue.CreateStringValue(nlplaypath));
			command.OptionalArguments.Add(AmfValue.CreateStringValue(nltoken));
			command.OptionalArguments.Add(AmfValue.CreateStringValue(nlid));

			// これがわからない
			// パケットキャプチャした結果を真似しているだけで
			// 意図を理解して追加しているわけではないです
			command.OptionalArguments.Add(AmfValue.CreateNumberValue(-2));


			return command;
		}
	}
}

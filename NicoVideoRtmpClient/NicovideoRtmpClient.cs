using Mntone.Data.Amf;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.MediaProperties;

namespace NicoVideoRtmpClient
{
	// see@ http://nico-lab.net/nicolive_rtmpdump_commands/

	// rtmp options see@ https://www.ffmpeg.org/ffmpeg-protocols.html#rtmp

	public interface INicoVideoRtmpConnection
	{
		RtmpUri Uri { get; }
		NetConnectionConnectCommand Command { get; }
		Task PostConnectionProcess(NetConnection connection);
	}

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

	abstract public class LiveNiconamaRtmpConnectionBase : NiconamaRtmpConnectionBase
	{
		public LiveNiconamaRtmpConnectionBase(PlayerStatusResponse res)
			: base(res)
		{
			

			
		}
	}


	public class OfficalLiveNiconamaRtmpConnection : LiveNiconamaRtmpConnectionBase
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


	public class ChannelLiveNiconamaRtmpConnection : LiveNiconamaRtmpConnectionBase
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
			// TcUrl は RtmpUriのInstanceを省いた文字列
			_Command.TcUrl = $"{Uri.Scheme.ToString().ToLower()}://{Uri.Host}:{Uri.Port}/{Uri.App}";
			_Command.FlashVersion = "WIN 23,0,0,162";
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

	public class ChannelArchiveNiconamaRtmpConnection : NiconamaRtmpConnectionBase
	{
		public ChannelArchiveNiconamaRtmpConnection(PlayerStatusResponse res) 
			: base(res)
		{
		}
	}


	public class CommunityLiveNiconamaRtmpConnection : ChannelLiveNiconamaRtmpConnection
	{
		public CommunityLiveNiconamaRtmpConnection(PlayerStatusResponse res) 
			: base(res)
		{
		}

	}

	public class CommunityArchiveNiconamaRtmpConnection : NiconamaRtmpConnectionBase
	{
		public CommunityArchiveNiconamaRtmpConnection(PlayerStatusResponse res) 
			: base(res)
		{
		}
	}





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


	public sealed class NicovideoRtmpClient : IDisposable
    {
		// ニコニコ生放送の種別ごとにRtmpの接続オプションの切り替えをサポートする

		// ハートビート用の定期呼び出しされるイベントをサポートする

		// 公式ニコ生やチャンネル生放送、ユーザー

		public event Action<NicovideoRtmpClientStartedEventArgs> Started;
		public event Action<NicovideoRtmpClientStoppedEventArgs> Stopped;



		private NetConnection _Connection;
		private NetStream _Stream;
		private MediaStreamSource _MediaStreamSource;

		private BufferingHelper _BufferingHelper;


		bool _IsClosed;

		static int _IdSeed;
		int ClientId;
		public NicovideoRtmpClient()
		{
			ClientId = _IdSeed++;
		}

		public void Dispose()
		{
			Close();
		}

		private void Close()
		{
			if (_MediaStreamSource != null)
			{
				_MediaStreamSource.Starting -= OnStarting;
				_MediaStreamSource.SampleRequested -= OnSampleRequested;

				_MediaStreamSource = null;
			}
			if (_Stream != null)
			{
				_Stream.Attached -= OnAttached;
				_Stream.StatusUpdated -= OnNetStreamStatusUpdated;
				_Stream.AudioStarted -= OnAudioStarted;
				_Stream.VideoStarted -= OnVideoStarted;
				_Stream = null;
			}
			if (_BufferingHelper != null)
			{
				_BufferingHelper.Stop();
				_BufferingHelper = null;
			}


			if (_Connection != null)
			{
				_Connection.StatusUpdated -= OnNetConnectionStatusUpdated;
				try
				{
					_Connection.Close();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
				}
				finally
				{
					_Connection = null;
				}
			}

			_IsClosed = true;

			Debug.WriteLine($"RTMP : {ClientId} closed.");
		}


		private void CreateMediaStream(IMediaStreamDescriptor descriptor)
		{
			//			Close();

			if (_MediaStreamSource != null)
			{
				throw new Exception();
			}

			_MediaStreamSource = new MediaStreamSource(descriptor);
			_MediaStreamSource.BufferTime = new TimeSpan(2 * 10000000);
			_MediaStreamSource.Duration = TimeSpan.MaxValue;

			_MediaStreamSource.Starting += OnStarting;
			_MediaStreamSource.SampleRequested += OnSampleRequested;

			Debug.WriteLine($"RTMP : {ClientId} media stream created.");
		}


		#region Connection

		INicoVideoRtmpConnection _ConnectionImpl;
		public async Task ConnectAsync(PlayerStatusResponse res)
		{
			_Connection = new NetConnection();

			_ConnectionImpl = NicoVideoRtmpConnectionHelper.MakeConnectionImpl(res);

			_Connection.StatusUpdated += new EventHandler<NetStatusUpdatedEventArgs>(OnNetConnectionStatusUpdated);


			var uri = _ConnectionImpl.Uri;
			var command = _ConnectionImpl.Command;
			if (command != null)
			{
				await _Connection.ConnectAsync(_ConnectionImpl.Uri, command);
			}
			else
			{
				await _Connection.ConnectAsync(_ConnectionImpl.Uri);
			}


		}

		#endregion

		#region NetConnection

		private async void OnNetConnectionStatusUpdated(object sender, NetStatusUpdatedEventArgs args)
		{
			var ncs = args.NetStatusCode;
			if (ncs == NetStatusCodeType.NetConnectionConnectSuccess)
			{
				_Stream = new NetStream();
				_Stream.Attached += OnAttached;
				_Stream.StatusUpdated += OnNetStreamStatusUpdated;
				_Stream.AudioStarted += OnAudioStarted;
				_Stream.VideoStarted += OnVideoStarted;

				_BufferingHelper = new BufferingHelper(_Stream);

				// createStream
				await _Stream.AttachAsync(_Connection);

				// nlPlayNotice(チャンネル/ユーザー生放送のライブ配信のみ)
				await _ConnectionImpl.PostConnectionProcess(_Connection);
			}
			else if ((ncs & NetStatusCodeType.Level2Mask) == NetStatusCodeType.NetConnectionConnect)
			{
				Close();
				Stopped?.Invoke(new NicovideoRtmpClientStoppedEventArgs());
			}
		}



#endregion


#region NetStream

		private async void OnAttached(object sender, NetStreamAttachedEventArgs args)
		{
			// createStreamとplayの間にnlPlayNoticeを挟むための待ち

			await _Stream.PlayAsync(_Connection.Uri.Instance);
		}


		private void OnNetStreamStatusUpdated(object sender, NetStatusUpdatedEventArgs args)
		{
			var nsc = args.NetStatusCode;
			if (nsc == NetStatusCodeType.NetStreamPlayStop)
			{
				Close();
				Stopped?.Invoke(new NicovideoRtmpClientStoppedEventArgs());
			}
		}

		bool isAlreadHaveAudio = false;
		private void OnAudioStarted(object sender, NetStreamAudioStartedEventArgs args)
		{
			if (_IsClosed)
			{
				throw new Exception();
			}
			if (_Connection == null)
			{
				Debug.WriteLine("すでに閉じられたRTMP接続です");
				return;
			}
			if (isAlreadHaveAudio) { return; }

			var info = args.Info;
			AudioEncodingProperties prop;
			if (info.Format == Mntone.Rtmp.Media.AudioFormat.Mp3)
			{
				prop = AudioEncodingProperties.CreateMp3(info.SampleRate, info.ChannelCount, info.Bitrate);
			}
			else if (info.Format == Mntone.Rtmp.Media.AudioFormat.Aac)
			{
				prop = AudioEncodingProperties.CreateAac(info.SampleRate, info.ChannelCount, info.Bitrate);
			}
			else
			{
				if (_MediaStreamSource != null)
				{
					Started?.Invoke(new NicovideoRtmpClientStartedEventArgs(_MediaStreamSource));
				}
				return;
			}

			prop.BitsPerSample = info.BitsPerSample;

			var desc = new AudioStreamDescriptor(prop);
			if (_MediaStreamSource != null)
			{
				try
				{
					_MediaStreamSource.AddStreamDescriptor(desc);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.ToString());
				}

				Started?.Invoke(new NicovideoRtmpClientStartedEventArgs(_MediaStreamSource));
			}
			else
			{
				CreateMediaStream(desc);
				if (args.AudioOnly)
				{
					Started?.Invoke(new NicovideoRtmpClientStartedEventArgs(_MediaStreamSource));
				}
			}

			isAlreadHaveAudio = true;

			Debug.WriteLine($"{nameof(NicovideoRtmpClient)}: audio : id:{ClientId}");
		}

		bool isAlreadHaveVideo = false;
		private void OnVideoStarted(object sender, NetStreamVideoStartedEventArgs args)
		{
			if (_IsClosed)
			{
				throw new Exception();
			}
			if (_Connection == null)
			{
				Debug.WriteLine("すでに閉じられたRTMP接続です");
				return;
			}
			if (isAlreadHaveVideo)
			{
				Debug.WriteLine("すでにビデオプロパティは初期化済み");
				return;
			}


			var info = args.Info;
			VideoEncodingProperties prop = null;
			if (info.Format == Mntone.Rtmp.Media.VideoFormat.Avc)
			{
				prop = VideoEncodingProperties.CreateH264();
				prop.ProfileId = (int)info.ProfileIndication;
			}
			else
			{
				if (_MediaStreamSource != null)
				{
					Started?.Invoke(new NicovideoRtmpClientStartedEventArgs(_MediaStreamSource));
				}
			}

			prop.Bitrate = info.Bitrate;
			prop.Height = info.Height;
			prop.Width = info.Width;

			var desc = new VideoStreamDescriptor(prop);
			if (_MediaStreamSource != null)
			{
				_MediaStreamSource.AddStreamDescriptor(desc);
				Started?.Invoke(new NicovideoRtmpClientStartedEventArgs(_MediaStreamSource));
			}
			else
			{
				CreateMediaStream(desc);
				if (args.VideoOnly)
				{
					Started?.Invoke(new NicovideoRtmpClientStartedEventArgs(_MediaStreamSource));
				}
			}

			isAlreadHaveVideo = true;

			Debug.WriteLine($"{nameof(NicovideoRtmpClient)}: video : id:{ClientId}");

		}

		#endregion


		#region MediaStreamSource

		private void OnStarting(MediaStreamSource sender, MediaStreamSourceStartingEventArgs args)
		{
			var request = args.Request;
			request.SetActualStartPosition(TimeSpan.Zero);
		}


		private async void OnSampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
		{
			var request = args.Request;
			var deferral = request.GetDeferral();
			if (request.StreamDescriptor is AudioStreamDescriptor)
			{
				await _BufferingHelper.GetAudioAsync()
					.AsTask()
					.ContinueWith(prevTask =>
					{
						request.Sample = prevTask.Result;
						deferral.Complete();
					});
			}
			else
			{
				await _BufferingHelper.GetVideoAsync()
					.AsTask()
					.ContinueWith(prevTask =>
					{
						request.Sample = prevTask.Result;
						deferral.Complete();
					});
			}
		}

#endregion





	}
}

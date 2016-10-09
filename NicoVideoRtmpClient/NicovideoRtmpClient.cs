using Mntone.Data.Amf;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.MediaProperties;

namespace NicoVideoRtmpClient
{

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


		public OfficalLiveNiconamaRtmpConnection(PlayerStatusResponse res) 
			: base(res)
		{
			// "case:sp:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv277498614,mobile:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv277498614_sub1,premium:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv277498614_sub1,default:limelight%3Artmp%3A%2F%2Fsmilevideo.fc.llnwd.net%3A1935%2Fsmilevideo%2Cs_lv277498614"

			var content = res.Stream.Contents.ElementAt(0);
			var splited = content.Value.Split(':', ',');
			var decodedSplit = splited.Select(x => WebUtility.UrlDecode(x));
			for (int i = 1; i < splited.Length; i++)
			{
				var str = splited[i];
				i++;
				var url = WebUtility.UrlDecode(splited[i]).Remove(0, "limelight:".Length);
				var splitUrl = url.Split(',');
				var rtmpUri = splitUrl[0];
				var liveId = splitUrl[1];
				switch (str)
				{
					case "sp":
						break;

					case "mobile":
						break;

					case "premium":
						if (res.User.IsPremium)
						{
							BaseUri = rtmpUri;
						}
						sLiveId = liveId;
						break;

					case "default":
						BaseUri = rtmpUri;
						sLiveId = liveId;
						break;
				}

				if (BaseUri != null)
				{
					break;
				}
			}


			var ticket = PlayerStatus.Stream.Tickets.ElementAt(0);
			var playpath = $"{ticket.Key}?{ticket.Value}";

			if (PlayerStatus.Stream.Contents.Count == 3)
			{
				BaseUri = BaseUri != null ? BaseUri : OfficailNiconamaBaseUri.OriginalString;
				RtmpUri = new RtmpUri(BaseUri);

				RtmpUri.Instance = playpath;
			}
			else
			{
				BaseUri = BaseUri != null ? BaseUri : OfficialNiconamaBase.OriginalString;
				RtmpUri = new RtmpUri(BaseUri);

				RtmpUri.Instance = playpath;
			}
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
		public ChannelLiveNiconamaRtmpConnection(PlayerStatusResponse res) : base(res)
		{
			var uri = PlayerStatus.Stream.RtmpUrl.OriginalString;
			var rtmpUri = new RtmpUri(uri);
			var pp = uri.Split('/');

			rtmpUri.App = string.Join("/", pp[3], pp[4]);
			rtmpUri.Instance = PlayerStatus.Program.Id;
			_Uri = rtmpUri;
			
		}


		public override NetConnectionConnectCommand Command
		{
			get
			{
				var command = new NetConnectionConnectCommand(Uri.App);
				command.SwfUrl = "http://live.nicovideo.jp/nicoliveplayer.swf?20130722";
				command.PageUrl = "http://live.nicovideo.jp/watch/" + PlayerStatus.Program.Id;
				// TcUrl は RtmpUriのInstanceを省いた文字列
				command.TcUrl = $"{Uri.Scheme.ToString().ToLower()}://{Uri.Host}:{Uri.Port}/{Uri.App}";
				command.FlashVersion = "WIN 23,0,0,162";
				// Rtmpのconnectのextrasとして PlayerStatus.Stream.Ticket の値を追加
				var amfArray = new AmfArray();
				amfArray.Add(AmfValue.CreateStringValue($"{PlayerStatus.Stream.Ticket}"));

				command.OptionalUserArguments = amfArray;

				return command;
			}
		}

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
			await connection.CallAsync(MakeNLPlayNoticeCommand());
		}


		protected NetConnectionCallCommand MakeNLPlayNoticeCommand()
		{
			var command = new NetConnectionCallCommand("nlPlayNotice");

			// TODO: コンテンツが生放送ではなくて動画IDになっている場合がある
			// smile:sm0000000
			var nlplaynoticeText = PlayerStatus.Stream.Contents[0].Value;

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

			command.OptionalArguments.Add(new AmfValue());
			command.OptionalArguments.Add(AmfValue.CreateStringValue(nlplaypath));
			command.OptionalArguments.Add(AmfValue.CreateStringValue(nltoken));
			command.OptionalArguments.Add(AmfValue.CreateStringValue(nlid));
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


		public NicovideoRtmpClient()
		{

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

				_Stream.Attached -= OnAttached;
				_Stream.StatusUpdated -= OnNetStreamStatusUpdated;
				_Stream.AudioStarted -= OnAudioStarted;
				_Stream.VideoStarted -= OnVideoStarted;
				_BufferingHelper.Stop();

				
				_BufferingHelper = null;
				_Stream = null;
				
				_Connection.StatusUpdated -= OnNetConnectionStatusUpdated;
				_Connection.Close();
				_Connection = null;
			}
		}


		private void CreateMediaStream(IMediaStreamDescriptor descriptor)
		{
			Close();

			_MediaStreamSource = new MediaStreamSource(descriptor);
			_MediaStreamSource.BufferTime = new TimeSpan(2 * 10000000);
			_MediaStreamSource.Duration = TimeSpan.MaxValue;

			_MediaStreamSource.Starting += OnStarting;
			_MediaStreamSource.SampleRequested += OnSampleRequested;
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
				await _Stream.AttachAsync(_Connection);

				await _ConnectionImpl.PostConnectionProcess(_Connection);

				await Task.Delay(100);
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


		private void OnAudioStarted(object sender, NetStreamAudioStartedEventArgs args)
		{
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
				_MediaStreamSource.AddStreamDescriptor(desc);
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
		}


		void OnVideoStarted(object sender, NetStreamVideoStartedEventArgs args)
		{
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

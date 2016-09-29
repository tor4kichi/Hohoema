using Mntone.Data.Amf;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;
using System;
using System.Collections.Generic;
using System.Linq;
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
				var uri = new RtmpUri(PlayerStatus.Stream.RtmpUrl);
				var command = new NetConnectionConnectCommand(uri.App);

				var nlplaynoticeText = PlayerStatus.Stream.Contents[0].Value;

				var split = nlplaynoticeText.Split(',', '?');
				var nlplaypath = split[0];
				var nlid = split[1];
				var nltoken = split[2];

				var nlplaynoticeParameter = new AmfArray();
				nlplaynoticeParameter.Add(AmfValue.CreateStringValue(nlplaypath));
				nlplaynoticeParameter.Add(AmfValue.CreateStringValue(nltoken));
				nlplaynoticeParameter.Add(AmfValue.CreateStringValue(nlid));


				command.OptionalUserArguments = AmfValue.CreateStringValue($"S:{PlayerStatus.Stream.Ticket}");

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

		protected NetConnectionCallCommand MakeConnCommand()
		{
			var command = new NetConnectionCallCommand("NLPlayNotice");

			command.CommandObject = AmfValue.CreateStringValue($"S:{PlayerStatus.Stream.Ticket}"); ;

			return command;
		}

	
	}

	abstract public class LiveNiconamaRtmpConnectionBase : NiconamaRtmpConnectionBase
	{


		public LiveNiconamaRtmpConnectionBase(PlayerStatusResponse res)
			: base(res)
		{

		}
		public override NetConnectionConnectCommand Command
		{
			get
			{
				var uri = new RtmpUri(PlayerStatus.Stream.RtmpUrl);
				var command = new NetConnectionConnectCommand(uri.App);
//				command.OptionalUserArguments = AmfValue.CreateStringValue($"S:{PlayerStatus.Stream.Ticket}");

				return command;
			}
		}




	}


	public class OfficalLiveNiconamaRtmpConnection : LiveNiconamaRtmpConnectionBase
	{
		public OfficalLiveNiconamaRtmpConnection(PlayerStatusResponse res) 
			: base(res)
		{
		}

		public override RtmpUri Uri
		{
			get
			{
				return base.Uri;
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
		}

		public override RtmpUri Uri
		{
			get
			{
				var uri = PlayerStatus.Stream.RtmpUrl.OriginalString;
				return new RtmpUri($"{uri}/{PlayerStatus.Program.Id}");
			}
		}

		public override async Task PostConnectionProcess(NetConnection connection)
		{
			//			var nlplaynoticeCommand = MakeNLPlayNoticeCommnad();

			//			await connection.CallAsync(nlplaynoticeCommand);
			await Task.Delay(0);
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
				_Connection = null;
			}
		}


		private void CreateMediaStream(IMediaStreamDescriptor descriptor)
		{
			Close();

			_MediaStreamSource = new MediaStreamSource(descriptor);
			_MediaStreamSource.BufferTime = new TimeSpan(5 * 10000000);
			_MediaStreamSource.Duration = TimeSpan.MaxValue;

			_MediaStreamSource.Starting += OnStarting;
			_MediaStreamSource.SampleRequested += OnSampleRequested;
		}


		#region Connection


		public async Task ConnectAsync(PlayerStatusResponse res)
		{
			_Connection = new NetConnection();



			string rtmpuri = null;


			var s = NicoVideoRtmpConnectionHelper.MakeConnectionImpl(res);

			_Connection.StatusUpdated += new EventHandler<NetStatusUpdatedEventArgs>(OnNetConnectionStatusUpdated);


			var uri = s.Uri;
			var command = s.Command;
			if (command != null)
			{
				await _Connection.ConnectAsync(s.Uri, command);
			}
			else
			{
				await _Connection.ConnectAsync(s.Uri);
			}

			await s.PostConnectionProcess(_Connection);			
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

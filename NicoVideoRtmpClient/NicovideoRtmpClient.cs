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
using Mntone.Data.Amf;
using Mntone.Nico2.Live.PlayerStatus;
using Mntone.Rtmp;
using Mntone.Rtmp.Client;
using Mntone.Rtmp.Command;

namespace NicoVideoRtmpClient
{
	// niconico rtmp command
	// http://nico-lab.net/nicolive_rtmpdump_commands/

	// rtmp options 
	// https://www.ffmpeg.org/ffmpeg-protocols.html#rtmp


	// Note: ニコ生向けのRTMPクライアントの実装です。
	// 生配信向けのRTMPは実装が完了していますが、
	// アーカイブ向けは未着手です。

	// Note: nlPlayNoticeの実装が知りたい場合は
	// NicoRtmpConnection.Live.ChannelLiveNiconamaRtmpConnection.cs
	// を参照のこと

	// Note: 新配信（β）について
	// unama/api/v1 みたいなURLから情報取得するみたいですが詳細は不明
	// βが外れたら対応予定です

	public sealed class NicovideoRtmpClient : IDisposable
    {
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

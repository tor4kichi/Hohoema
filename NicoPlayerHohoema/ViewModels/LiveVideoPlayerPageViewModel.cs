using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Windows.Navigation;
using FFmpegInterop;
using Windows.Media.Core;
using System.Threading;
using NicoPlayerHohoema.Util;
using System.Diagnostics;
using Windows.Foundation.Collections;
using NicoVideoRtmpClient;
using Mntone.Nico2.Live.PlayerStatus;

namespace NicoPlayerHohoema.ViewModels
{
	public class LiveVideoPlayerPageViewModel : HohoemaViewModelBase, IDisposable
	{
		// TODO: MediaElementがCloseになった場合に対応する
		

		public string LiveId { get; private set; }

		
		private MediaStreamSource _VideoStreamSource;
		public MediaStreamSource VideoStreamSource
		{
			get { return _VideoStreamSource; }
			set { SetProperty(ref _VideoStreamSource, value); }
		}

		AsyncLock _HeartbeatTimerLock = new AsyncLock();
		Timer _HeartbeatTimer;
		TimeSpan _HeartbeatInterval = TimeSpan.FromSeconds(45);


		NicovideoRtmpClient _RtmpClient;


		public LiveVideoPlayerPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{
			
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				LiveId = e.Parameter as string;
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (LiveId == null) { return; }

			await TryStartViewing();


			await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}


		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			EndLiveSubscribeAction().ConfigureAwait(false);

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}


		// see@ http://nico-lab.net/nicolive_rtmpdump_commands/

		// options is see@ https://www.ffmpeg.org/ffmpeg-protocols.html#rtmp

		private async Task TryStartViewing()
		{
			try
			{
				var res = await HohoemaApp.NiconicoContext.Live.GetPlayerStatusAsync(LiveId);

				if (res == null) { return; }

				await OpenRtmpConnection(res);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				return;
			}


			await StartLiveSubscribeAction();
		}

		private async Task OpenRtmpConnection(PlayerStatusResponse res)
		{
			_RtmpClient = new NicovideoRtmpClient();

			_RtmpClient.Started += _RtmpClient_Started;
			_RtmpClient.Stopped += _RtmpClient_Stopped;

			await _RtmpClient.ConnectAsync(res);
		}

		private void CloseRtmpConnection()
		{
			if (_RtmpClient != null)
			{
				_RtmpClient.Started -= _RtmpClient_Started;
				_RtmpClient.Stopped -= _RtmpClient_Stopped;

				_RtmpClient?.Dispose();
			}
		}


		private async void _RtmpClient_Started(NicovideoRtmpClientStartedEventArgs args)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => 
			{
				VideoStreamSource = args.MediaStreamSource;

				await StartLiveSubscribeAction();
			});
		}

		private async void _RtmpClient_Stopped(NicovideoRtmpClientStoppedEventArgs args)
		{
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
			{
				VideoStreamSource = null;

				await EndLiveSubscribeAction();
			});
		}

		private async Task StartLiveSubscribeAction()
		{
			// 定期的にHeartbeatAPIを叩く処理を開始する
			await StartHeartbeatTimer();
		}


		
		private async Task EndLiveSubscribeAction()
		{
			if (LiveId == null) { return; }

			// UI上での映像の再生を止める
			VideoStreamSource = null;

			// HeartbeatAPIへのアクセスを停止
			await ExitHeartbeatTimer();

			// ニコ生サーバーとのコネクションを切断
			CloseRtmpConnection();

			// 放送からの離脱APIを叩く
			await HohoemaApp.NiconicoContext.Live.LeaveAsync(LiveId);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		/// <remarks>https://www59.atwiki.jp/nicoapi/pages/19.html</remarks>
		private async Task TryHeartbeat()
		{
			using (var releaser = await _HeartbeatTimerLock.LockAsync())
			{
				if (LiveId == null) { return; }

				try
				{
					var res = await HohoemaApp.NiconicoContext.Live.HeartbeatAsync(LiveId);

					
					await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
					{
						// TODO: 視聴者数やコメント数の更新

						await Task.Delay(0);
					});
				}
				catch
				{
					// ハートビートに失敗した場合は、放送終了か追い出された
					await EndLiveSubscribeAction();
				}
			}
		}


		private async Task StartHeartbeatTimer()
		{
			await ExitHeartbeatTimer();

			using (var releaser = await _HeartbeatTimerLock.LockAsync())
			{
				_HeartbeatTimer = new Timer(
					async state => await TryHeartbeat(),
					null, 
					TimeSpan.Zero, 
					_HeartbeatInterval
					);
			}
		}


		private async Task ExitHeartbeatTimer()
		{
			using (var releaser = await _HeartbeatTimerLock.LockAsync())
			{
				if (_HeartbeatTimer != null)
				{
					_HeartbeatTimer.Dispose();
					_HeartbeatTimer = null;
				}
			}
		} 
		
	}
}

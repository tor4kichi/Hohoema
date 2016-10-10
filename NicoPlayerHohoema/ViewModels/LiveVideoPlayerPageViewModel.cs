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
using NicoPlayerHohoema.Models.Live;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Windows.UI;

namespace NicoPlayerHohoema.ViewModels
{
	public class LiveVideoPlayerPageViewModel : HohoemaViewModelBase, IDisposable
	{
		// TODO: MediaElementがCloseになった場合に対応する


		/// <summary>
		/// 生放送の再生時間をローカルで更新する頻度
		/// </summary>
		public static TimeSpan VPosUpdateInterval { get; private set; } 
			= TimeSpan.FromSeconds(0.008);




		public string LiveId { get; private set; }

		NicoLiveVideo _NicoLiveVideo;



		public ReactiveProperty<object> VideoStream { get; private set; }

		public ReadOnlyReactiveCollection<Views.Comment> LiveComments { get; private set; }



		private TimeSpan _CurrentTime;
		public TimeSpan CurrentTime
		{
			get { return _CurrentTime; }
			set { SetProperty(ref _CurrentTime, value); }
		}

		AsyncLock _VPosIncrementTimingTimerLock = new AsyncLock();
		Timer _VPosIncrementTimingTimer;


		public ReactiveProperty<bool> IsVisibleComment { get; private set; }
		public ReactiveProperty<int> CommentRenderFPS { get; private set; }
		public ReactiveProperty<double> RequestCommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }

		public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
		public ReactiveProperty<Color> CommentDefaultColor { get; private set; }
		

		public LiveVideoPlayerPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{

			VideoStream = new ReactiveProperty<object>();

			IsVisibleComment = new ReactiveProperty<bool>(true);
			CommentRenderFPS = new ReactiveProperty<int>(60);
			RequestCommentDisplayDuration = new ReactiveProperty<double>(5.0);
			CommentFontScale = new ReactiveProperty<double>(1.0);

			CommentCanvasHeight = new ReactiveProperty<double>(0.0);
			CommentDefaultColor = new ReactiveProperty<Color>(Colors.White);
		}


		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				LiveId = e.Parameter as string;
			}
			
			if (LiveId != null)
			{
				_NicoLiveVideo = new NicoLiveVideo(LiveId, HohoemaApp);
				_NicoLiveVideo.ObserveProperty(x => x.VideoStreamSource)
					.Subscribe(x => VideoStream.Value = x)
					.AddTo(_NavigatingCompositeDisposable);
				LiveComments = _NicoLiveVideo.LiveComments.ToReadOnlyReactiveCollection(x =>
				{
					var comment = new Views.Comment();

					comment.CommentText = x.Text;
					comment.CommentId = x.GetCommentNo();
					comment.IsAnonimity = x.GetAnonymity();
					comment.VideoPosition = Math.Max(0,  x.GetVpos());
					comment.EndPosition = comment.VideoPosition + 1000;
					// TODO: LiveCommentのコマンドの解析

					return comment;
				});

				OnPropertyChanged(nameof(LiveComments));
			}

			base.OnNavigatedTo(e, viewModelState);
		}

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			await TryStartViewing();

			await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}


		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			_NicoLiveVideo.Dispose();
			_NicoLiveVideo = null;

			_VPosIncrementTimingTimer?.Dispose();
			_VPosIncrementTimingTimer = null;

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}


		
		private async Task TryStartViewing()
		{
			if (_NicoLiveVideo == null) { return; }

			try
			{
				var success = await _NicoLiveVideo.SetupLive();

				// 放送の再生位置を初期化
				CurrentTime = DateTime.Now - _NicoLiveVideo.PlayerStatusResponse.Program.BaseAt;

				using (var releaser = await _VPosIncrementTimingTimerLock.LockAsync())
				{
					_VPosIncrementTimingTimer = new Timer(TimeIncrement
					, null,
					TimeSpan.Zero,
					VPosUpdateInterval
					);
				}

				if (!success)
				{
					Debug.WriteLine("生放送情報の取得失敗しました "  + LiveId);
					return;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
				return;
			}
		}

		
		async void TimeIncrement(object state)
		{
			using (var releaser = await _VPosIncrementTimingTimerLock.LockAsync())
			{
				if (_VPosIncrementTimingTimer == null ) { return; }

				if (_NicoLiveVideo == null) { return; }

				await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
				{
					if (_NicoLiveVideo == null) { return; }

					CurrentTime = DateTime.Now -_NicoLiveVideo.PlayerStatusResponse.Program.BaseAt;
				});
			}
		}
		

		



		

		

	}
}

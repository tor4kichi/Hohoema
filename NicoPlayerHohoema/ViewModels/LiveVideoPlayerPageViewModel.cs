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
using Prism.Commands;
using System.Reactive.Concurrency;
using Windows.UI.ViewManagement;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
	public class LiveVideoPlayerPageViewModel : HohoemaViewModelBase, IDisposable
	{
		
		private SynchronizationContextScheduler _PlayerWindowUIDispatcherScheduler;
		public SynchronizationContextScheduler PlayerWindowUIDispatcherScheduler
		{
			get
			{
				return _PlayerWindowUIDispatcherScheduler
					?? (_PlayerWindowUIDispatcherScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
			}
		}

		/// <summary>
		/// 生放送の再生時間をローカルで更新する頻度
		/// </summary>
		/// <remarks>コメント描画を120fpsで行えるように0.008秒で更新しています</remarks>
		public static TimeSpan LiveElapsedTimeUpdateInterval { get; private set; } 
			= TimeSpan.FromSeconds(0.008);




		public string LiveId { get; private set; }

		public NicoLiveVideo NicoLiveVideo { get; private set; }



		public ReactiveProperty<object> VideoStream { get; private set; }

		public ReadOnlyReactiveCollection<Views.Comment> LiveComments { get; private set; }



		private TimeSpan _LiveElapsedTime;
		public TimeSpan LiveElapsedTime
		{
			get { return _LiveElapsedTime; }
			set { SetProperty(ref _LiveElapsedTime, value); }
		}

		Util.AsyncLock _LiveElapsedTimeUpdateTimerLock = new Util.AsyncLock();
		Timer _LiveElapsedTimeUpdateTimer;


		private DateTimeOffset _BaseAt;

		// play
		public ReactiveProperty<bool> NowPlaying { get; private set; }

		public ReactiveProperty<uint> CommentCount { get; private set; }
		public ReactiveProperty<uint> WatchCount { get; private set; }

		// comment


		public ReactiveProperty<bool> IsVisibleComment { get; private set; }
		public ReactiveProperty<int> CommentRenderFPS { get; private set; }
		public ReactiveProperty<double> RequestCommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<bool> IsFullScreen { get; private set; }

		public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
		public ReactiveProperty<Color> CommentDefaultColor { get; private set; }


		// sound
		public ReactiveProperty<bool> IsMuted { get; private set; }
		public ReactiveProperty<double> SoundVolume { get; private set; }


		// ui
		public ReactiveProperty<bool> IsAutoHideEnable { get; private set; }


		public LiveVideoPlayerPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{

			VideoStream = new ReactiveProperty<object>();

			NowPlaying = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);

			IsVisibleComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, true).AddTo(_CompositeDisposable);
			CommentRenderFPS = new ReactiveProperty<int>(PlayerWindowUIDispatcherScheduler, 60).AddTo(_CompositeDisposable);
			RequestCommentDisplayDuration = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 5.0).AddTo(_CompositeDisposable);
			CommentFontScale = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 1.0).AddTo(_CompositeDisposable);

			CommentCanvasHeight = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0.0).AddTo(_CompositeDisposable);
			CommentDefaultColor = new ReactiveProperty<Color>(PlayerWindowUIDispatcherScheduler, Colors.White).AddTo(_CompositeDisposable);

			IsMuted = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false).AddTo(_CompositeDisposable);
			SoundVolume = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0.5).AddTo(_CompositeDisposable);

			IsFullScreen = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false).AddTo(_CompositeDisposable);
			IsFullScreen
				.Subscribe(isFullScreen =>
				{
					var appView = ApplicationView.GetForCurrentView();
					if (isFullScreen)
					{
						if (!appView.TryEnterFullScreenMode())
						{
							IsFullScreen.Value = false;
						}
					}
					else
					{
						appView.ExitFullScreenMode();
					}
				})
			.AddTo(_CompositeDisposable);

			IsAutoHideEnable =
				Observable.CombineLatest(
					NowPlaying
					//, NowCommentWriting.Select(x => !x)
					)
				.Select(x => x.All(y => y))
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
		}


		#region Command


		private DelegateCommand _UpdateCommand;
		public DelegateCommand UpdateCommand
		{
			get
			{
				return _UpdateCommand
					?? (_UpdateCommand = new DelegateCommand(async () =>
					{
						await TryStartViewing();
					}));
			}
		}




		private DelegateCommand _ToggleMuteCommand;
		public DelegateCommand ToggleMuteCommand
		{
			get
			{
				return _ToggleMuteCommand
					?? (_ToggleMuteCommand = new DelegateCommand(() =>
					{
						IsMuted.Value = !IsMuted.Value;
					}));
			}
		}


		private DelegateCommand _VolumeUpCommand;
		public DelegateCommand VolumeUpCommand
		{
			get
			{
				return _VolumeUpCommand
					?? (_VolumeUpCommand = new DelegateCommand(() =>
					{
						var amount = HohoemaApp.UserSettings.PlayerSettings.ScrollVolumeFrequency;
						SoundVolume.Value = Math.Min(1.0, SoundVolume.Value + amount);
					}));
			}
		}

		private DelegateCommand _VolumeDownCommand;
		public DelegateCommand VolumeDownCommand
		{
			get
			{
				return _VolumeDownCommand
					?? (_VolumeDownCommand = new DelegateCommand(() =>
					{
						var amount = HohoemaApp.UserSettings.PlayerSettings.ScrollVolumeFrequency;
						SoundVolume.Value = Math.Max(0.0, SoundVolume.Value - amount);
					}));
			}
		}


		#endregion



		public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				LiveId = e.Parameter as string;
			}
			
			if (LiveId != null)
			{
				NicoLiveVideo = new NicoLiveVideo(LiveId, HohoemaApp);
				NicoLiveVideo.ObserveProperty(x => x.VideoStreamSource)
					.Subscribe(x =>
					{
						VideoStream.Value = x;
					})
					.AddTo(_NavigatingCompositeDisposable);
				LiveComments = NicoLiveVideo.LiveComments.ToReadOnlyReactiveCollection(x =>
				{
					var comment = new Views.Comment();

					comment.CommentText = x.Text;
					comment.CommentId = x.No != null ? x.GetCommentNo() : 0;
					comment.IsAnonimity = x.GetAnonymity();
					comment.VideoPosition = Math.Max(0,  x.GetVpos());
					comment.EndPosition = comment.VideoPosition + 1000; // コメントレンダラが再計算するが、仮置きしないと表示対象として処理されない

					comment.ApplyCommands(x.GetCommandTypes());

					return comment;
				});

				OnPropertyChanged(nameof(LiveComments));

				CommentCount = NicoLiveVideo.ObserveProperty(x => x.CommentCount)
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
					.AddTo(_NavigatingCompositeDisposable);
				OnPropertyChanged(nameof(CommentCount));

				WatchCount = NicoLiveVideo.ObserveProperty(x => x.WatchCount)
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
					.AddTo(_NavigatingCompositeDisposable);
				OnPropertyChanged(nameof(WatchCount));

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
			NicoLiveVideo.Dispose();
			NicoLiveVideo = null;

			StopLiveElapsedTimer().ConfigureAwait(false);

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}


		
		private async Task TryStartViewing()
		{
			if (NicoLiveVideo == null) { return; }

			try
			{
				NowPlaying.Value = false;



				var success = await NicoLiveVideo.SetupLive();

				if (success)
				{
					_BaseAt = NicoLiveVideo.PlayerStatusResponse.Program.BaseAt;

					await StartLiveElapsedTimer();

					OnPropertyChanged(nameof(NicoLiveVideo));

					NowPlaying.Value = true;
				}
				else
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


		private async Task StartLiveElapsedTimer()
		{
			await StopLiveElapsedTimer();

			using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
			{
				_LiveElapsedTimeUpdateTimer = new Timer(UpdateLiveElapsedTime
				, null,
				TimeSpan.Zero,
				LiveElapsedTimeUpdateInterval
				);
			}

			Debug.WriteLine("live elapsed timer started.");
		}

		private async Task StopLiveElapsedTimer()
		{
			using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
			{
				if (_LiveElapsedTimeUpdateTimer != null)
				{
					_LiveElapsedTimeUpdateTimer?.Dispose();
					_LiveElapsedTimeUpdateTimer = null;

					Debug.WriteLine("live elapsed timer stoped.");

					await Task.Delay(500);
				}
			}
		}


		/// <summary>
		/// 放送開始からの経過時間を更新します
		/// </summary>
		/// <param name="state">Timerオブジェクトのコールバックとして登録できるようにするためのダミー</param>
		async void UpdateLiveElapsedTime(object state = null)
		{
			using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
			{
				await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
				{
					// ローカルの現在時刻から放送開始のベース時間を引いて
					// 放送経過時間の絶対値を求める
					LiveElapsedTime = DateTime.Now - _BaseAt;
				});
			}
		}
		

		



		

		

	}
}

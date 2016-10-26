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
using NicoPlayerHohoema.ViewModels.LiveVideoInfoContent;
using NicoPlayerHohoema.Views.Service;

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




		public TextInputDialogService TextInputDialogService { get; private set; }



		public string LiveId { get; private set; }


		private string _LiveTitle;
		public string LiveTitle
		{
			get { return _LiveTitle; }
			set { SetProperty(ref _LiveTitle, value); }
		}

		private string _CommunityId;
		public string CommunityId
		{
			get { return _CommunityId; }
			set { SetProperty(ref _CommunityId, value); }
		}

		private string _CommunityName;
		public string CommunityName
		{
			get { return _CommunityName; }
			set { SetProperty(ref _CommunityName, value); }
		}

		private string _SeetType;
		public string RoomName
		{
			get { return _SeetType; }
			set { SetProperty(ref _SeetType, value); }
		}

		private uint _SeetNumber;
		public uint SeetId
		{
			get { return _SeetNumber; }
			set { SetProperty(ref _SeetNumber, value); }
		}


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


		private DateTimeOffset _StartAt;
		private DateTimeOffset _EndAt;

		// play
		public ReactiveProperty<bool> NowPlaying { get; private set; }
		public ReactiveProperty<bool> NowUpdating { get; private set; }
		public ReactiveProperty<bool> NowConnecting { get; private set; }

		public ReactiveProperty<uint> CommentCount { get; private set; }
		public ReactiveProperty<uint> WatchCount { get; private set; }

		// comment

		
		public ReactiveProperty<bool> IsVisibleComment { get; private set; }
		public ReactiveProperty<int> CommentRenderFPS { get; private set; }
		public ReactiveProperty<TimeSpan> RequestCommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<bool> IsFullScreen { get; private set; }

		public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
		public ReactiveProperty<Color> CommentDefaultColor { get; private set; }

		// post comment
		public ReactiveProperty<string> WritingComment { get; private set; }
		public ReactiveProperty<bool> NowCommentWriting { get; private set; }
		public ReactiveProperty<bool> NowSubmittingComment { get; private set; }

		public CommentCommandEditerViewModel CommandEditerVM { get; private set; }
		public ReactiveProperty<string> CommandString { get; private set; }

		public ReactiveCommand CommentSubmitCommand { get; private set; }

		// operation command

		/// <summary>
		/// 運営コマンドで発行される時間経過で非表示にならないテキスト
		/// 放送のお知らせなどに利用される
		/// </summary>
		public ReactiveProperty<string> PermanentDisplayText { get; private set; }



		// sound
		public ReactiveProperty<bool> IsMuted { get; private set; }
		public ReactiveProperty<double> SoundVolume { get; private set; }


		// ui
		public ReactiveProperty<bool> IsAutoHideEnable { get; private set; }
		public ReactiveProperty<TimeSpan> AutoHideDelayTime { get; private set; }



		// pane content
		private Dictionary<LiveVideoPaneContentType, LiveInfoContentViewModelBase> _PaneContentCache;

		public static List<LiveVideoPaneContentType> PaneContentTypes { get; private set; } = new[] {
				LiveVideoPaneContentType.Summary,
				LiveVideoPaneContentType.Comment,
				LiveVideoPaneContentType.Shere
			}.ToList();

			
		public ReactiveProperty<LiveVideoPaneContentType> SelectedPaneContent { get; private set; }
		public ReactiveProperty<LiveInfoContentViewModelBase> PaneContent { get; private set; }


		// suggestion
		public ReactiveProperty<LiveSuggestion> Suggestion { get; private set; }
		public ReactiveProperty<bool> HasSuggestion { get; private set; }



		public LiveVideoPlayerPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, TextInputDialogService textInputDialogService) 
			: base(hohoemaApp, pageManager, isRequireSignIn:true)
		{
			TextInputDialogService = textInputDialogService;


			VideoStream = new ReactiveProperty<object>();

			NowPlaying = VideoStream.Select(x => x != null)
				.ToReactiveProperty();

			NowUpdating = new ReactiveProperty<bool>(false);

			IsVisibleComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, true).AddTo(_CompositeDisposable);

			CommentCanvasHeight = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0.0).AddTo(_CompositeDisposable);
			CommentDefaultColor = new ReactiveProperty<Color>(PlayerWindowUIDispatcherScheduler, Colors.White).AddTo(_CompositeDisposable);

			// post comment
			WritingComment = new ReactiveProperty<string>(PlayerWindowUIDispatcherScheduler, "").AddTo(_CompositeDisposable);
			NowCommentWriting = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler).AddTo(_CompositeDisposable);
			NowSubmittingComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler).AddTo(_CompositeDisposable);

			// TODO: ニコ生での匿名コメント設定
			CommandString = new ReactiveProperty<string>(PlayerWindowUIDispatcherScheduler, "").AddTo(_CompositeDisposable);
			CommandEditerVM = new CommentCommandEditerViewModel(true /* isDefaultAnnonymous */);
			CommandEditerVM.OnCommandChanged += CommandEditerVM_OnCommandChanged;
			CommandEditerVM.ChangeEnableAnonymity(true);
			CommandEditerVM.IsAnonymousComment.Value = true;

			CommandEditerVM_OnCommandChanged();


			CommentSubmitCommand = Observable.CombineLatest(
				WritingComment.Select(x => !string.IsNullOrEmpty(x)),
				NowSubmittingComment.Select(x => !x)
				)
				.Select(x => x.All(y => y))
				.ToReactiveCommand(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			CommentSubmitCommand.Subscribe(async x =>
			{
				if (NicoLiveVideo != null)
				{
					NowSubmittingComment.Value = true;
					await NicoLiveVideo.PostComment(WritingComment.Value, CommandString.Value, LiveElapsedTime);
				}
			});


			// operation command
			PermanentDisplayText = new ReactiveProperty<string>(PlayerWindowUIDispatcherScheduler, "").AddTo(_CompositeDisposable);


			// sound
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
					, NowCommentWriting.Select(x => !x)
					)
				.Select(x => x.All(y => y))
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			_PaneContentCache = new Dictionary<LiveVideoPaneContentType, LiveInfoContentViewModelBase>();
			SelectedPaneContent = new ReactiveProperty<LiveVideoPaneContentType>(PaneContentTypes.First(), mode:ReactivePropertyMode.DistinctUntilChanged);
			PaneContent = SelectedPaneContent
				.Select(x =>
				{
					LiveInfoContentViewModelBase vm = null;
					if (!_PaneContentCache.ContainsKey(x))
					{
						vm = CreateLiveVideoPaneContent(x);
						_PaneContentCache.Add(x, vm);
					}
					else
					{
						vm = _PaneContentCache[x];
					}

					if (vm != null)
					{
						var oldVm = PaneContent?.Value;
						if (oldVm != null)
						{
							oldVm.OnLeave();
						}
					}

					return vm;
				})
				.ToReactiveProperty();

			Suggestion = new ReactiveProperty<LiveSuggestion>();
			HasSuggestion = Suggestion.Select(x => x != null)
				.ToReactiveProperty();


			NowPlaying.Subscribe(x => 
			{
				if (x)
				{
					DisplayRequestHelper.RequestKeepDisplay();
				}
				else
				{
					DisplayRequestHelper.StopKeepDisplay();
				}
			});

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
						if (await TryUpdateLiveStatus())
						{
							await NicoLiveVideo.RetryRtmpConnection();

							// 配信終了１分前であれば次枠検出をスタートさせる
							if (DateTime.Now > _EndAt - TimeSpan.FromMinutes(1))
							{
								await NicoLiveVideo.StartNextLiveSubscribe(NicoLiveVideo.DefaultNextLiveSubscribeDuration);
							}
						}
						else
						{
							// 配信時間内に別の枠を取り直していた場合に対応する
							await NicoLiveVideo.StartNextLiveSubscribe(NicoLiveVideo.DefaultNextLiveSubscribeDuration);
						}
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
				var json = e.Parameter as string;
				var payload = LiveVidePagePayload.FromParameterString(json);

				LiveId = payload.LiveId;
				LiveTitle = payload.LiveTitle;
				CommunityId = payload.CommunityId;
				CommunityName = payload.CommunityName;
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

				NowConnecting = Observable.CombineLatest(
					NicoLiveVideo.ObserveProperty(x => x.VideoStreamSource).Select(x => x == null),
					NicoLiveVideo.ObserveProperty(x => x.LiveStatusType).Select(x => x == null)
					)
					.Select(x => x.All(y => y))
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
					.AddTo(_NavigatingCompositeDisposable);
				OnPropertyChanged(nameof(NowConnecting));

				LiveComments = NicoLiveVideo.LiveComments.ToReadOnlyReactiveCollection(x =>
				{
					var comment = new Views.Comment();

					comment.CommentText = x.Text;
					comment.CommentId = !string.IsNullOrEmpty(x.No) ? x.GetCommentNo() : 0;
					comment.IsAnonimity = !string.IsNullOrEmpty(x.Anonymity) ? x.GetAnonymity() : false;
					comment.UserId = x.User_id;
					comment.IsOwnerComment = x.User_id == NicoLiveVideo?.BroadcasterId;
					try
					{
						comment.IsLoginUserComment = !comment.IsAnonimity ? uint.Parse(x.User_id) == HohoemaApp.LoginUserId : false;
					}
					catch { }

					//x.GetVposでサーバー上のコメント位置が取れるが、
					// 受け取った順で表示したいのでローカルの放送時間からコメント位置を割り当てる
					comment.VideoPosition = (uint)(LiveElapsedTime.TotalMilliseconds * 0.1);
					// EndPositionはコメントレンダラが再計算するが、仮置きしないと表示対象として処理されない
					comment.EndPosition = comment.VideoPosition + 1000;

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

				CommunityId = NicoLiveVideo.BroadcasterCommunityId;


				// post comment 
				NicoLiveVideo.PostCommentResult += NicoLiveVideo_PostCommentResult;

				// operation command
				PermanentDisplayText = NicoLiveVideo.ObserveProperty(x => x.PermanentDisplayText)
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
					.AddTo(_NavigatingCompositeDisposable);
				OnPropertyChanged(nameof(PermanentDisplayText));


				// next live
				NicoLiveVideo.NextLive += NicoLiveVideo_NextLive;
			}

			base.OnNavigatedTo(e, viewModelState);
		}


		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			await TryStartViewing();


			if (CommunityName == null)
			{
				try
				{
					var communityDetail = await HohoemaApp.ContentFinder.GetCommunityInfo(CommunityId);
					if (communityDetail.IsStatusOK)
					{
						CommunityName = communityDetail.Community.Name;
					}
				}
				catch { }
			}

			var vm = CreateLiveVideoPaneContent(LiveVideoPaneContentType.Summary);
			await vm.OnEnter();
			_PaneContentCache.Add(LiveVideoPaneContentType.Summary, vm);

			SelectedPaneContent.ForceNotify();

			await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}



		protected override void OnSignIn(ICollection<IDisposable> userSessionDisposer)
		{
			AutoHideDelayTime = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.AutoHidePlayerControlUIPreventTime, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(AutoHideDelayTime));



			IsMuted = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.IsMute, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(IsMuted));

			SoundVolume = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.SoundVolume, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(SoundVolume));

			CommentRenderFPS = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
				.Select(x => (int)x)
				.ToReactiveProperty()
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(CommentRenderFPS));

			RequestCommentDisplayDuration = HohoemaApp.UserSettings.PlayerSettings
				.ObserveProperty(x => x.CommentDisplayDuration)
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(RequestCommentDisplayDuration));

			CommentFontScale = HohoemaApp.UserSettings.PlayerSettings
				.ObserveProperty(x => x.DefaultCommentFontScale)
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(CommentFontScale));

			base.OnSignIn(userSessionDisposer);
		}


		public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			NicoLiveVideo.Dispose();
			NicoLiveVideo = null;

			VideoStream.Value = null;

			DisplayRequestHelper.StopKeepDisplay();

			IsFullScreen.Value = false;

			StopLiveElapsedTimer().ConfigureAwait(false);

			base.OnNavigatingFrom(e, viewModelState, suspending);
		}


		/// <summary>
		/// 生放送情報を取得してライブストリームの受信を開始します。<br />
		/// 配信受信処理のハードリセット動作として機能します。
		/// </summary>
		/// <returns></returns>
		private async Task TryStartViewing()
		{
			if (NicoLiveVideo == null) { return; }

			try
			{
				NowUpdating.Value = true;

				var liveStatus = await NicoLiveVideo.SetupLive();

				if (liveStatus == null)
				{
					_StartAt = NicoLiveVideo.PlayerStatusResponse.Program.StartedAt;
					_EndAt = NicoLiveVideo.PlayerStatusResponse.Program.EndedAt;

					await StartLiveElapsedTimer();

					LiveTitle = NicoLiveVideo.LiveTitle;
					CommunityId = NicoLiveVideo.BroadcasterCommunityId;

					// seet
					RoomName = NicoLiveVideo.PlayerStatusResponse.Room.Name;
					SeetId = NicoLiveVideo.PlayerStatusResponse.Room.SeatId;

					OnPropertyChanged(nameof(NicoLiveVideo));
				}
				else
				{
					Debug.WriteLine("生放送情報の取得失敗しました "  + LiveId);
					VideoStream.Value = null;
				}

				ResetSuggestion(liveStatus);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
				NowUpdating.Value = false;
			}

			// コメント送信中にコメントクライアント切断した場合に対応
			NowSubmittingComment.Value = false;
		}


		/// <summary>
		/// 生放送情報だけを更新し、配信ストリームの更新は行いません。
		/// </summary>
		/// <returns></returns>
		private async Task<bool> TryUpdateLiveStatus()
		{
			if (NicoLiveVideo == null) { return false; }

			LiveStatusType? liveStatus = null;
			try
			{
				NowUpdating.Value = true;


				liveStatus = await NicoLiveVideo.UpdateLiveStatus();

				if (liveStatus == null)
				{
					_StartAt = NicoLiveVideo.PlayerStatusResponse.Program.StartedAt;
					_EndAt = NicoLiveVideo.PlayerStatusResponse.Program.EndedAt;
				}
				else
				{
					VideoStream.Value = null;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
				NowUpdating.Value = false;
			}

			ResetSuggestion(liveStatus);

			NowSubmittingComment.Value = false;

			return liveStatus == null;
		}



	

		private async Task StartLiveElapsedTimer()
		{
			await StopLiveElapsedTimer();

			using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
			{
				_LiveElapsedTimeUpdateTimer = new Timer(UpdateLiveElapsedTime,
					null,
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

		bool _IsEndMarked;
		bool _IsNextLiveSubscribeStarted;
		
		/// <summary>
		/// 放送開始からの経過時間を更新します
		/// </summary>
		/// <param name="state">Timerオブジェクトのコールバックとして登録できるようにするためのダミー</param>
		async void UpdateLiveElapsedTime(object state = null)
		{
			using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
			{
				await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => 
				{
					// ローカルの現在時刻から放送開始のベース時間を引いて
					// 放送経過時間の絶対値を求める
					LiveElapsedTime = DateTime.Now - _StartAt;

					// 終了時刻を過ぎたら生放送情報を更新する
					if (!_IsEndMarked && DateTime.Now > _EndAt)
					{
						_IsEndMarked = true;

						await Task.Delay(TimeSpan.FromSeconds(3));
						if (await TryUpdateLiveStatus())
						{
							// 放送が延長されていた場合は継続
							// _EndAtもTryUpdateLiveStatus内で更新されているはず
							_IsEndMarked = false;
						}
					}

					// 終了時刻の３０秒前から
					if (!_IsNextLiveSubscribeStarted && DateTime.Now > _EndAt - TimeSpan.FromSeconds(10))
					{
						_IsNextLiveSubscribeStarted = true;

						await NicoLiveVideo.StartNextLiveSubscribe(NicoLiveVideo.DefaultNextLiveSubscribeDuration);
					}
				});
			}
		}




		/// <summary>
		/// サイドペインに表示するコンテンツVMを作成します
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		private LiveInfoContentViewModelBase CreateLiveVideoPaneContent(LiveVideoPaneContentType type)
		{
			LiveInfoContentViewModelBase vm = null;
			switch (type)
			{
				case LiveVideoPaneContentType.Summary:
					vm = new SummaryLiveInfoContentViewModel(CommunityName, NicoLiveVideo, PageManager);
					break;
				case LiveVideoPaneContentType.Comment:
					vm = new CommentLiveInfoContentViewModel(NicoLiveVideo, LiveComments);
					break;
				case LiveVideoPaneContentType.Shere:
					vm = new ShereLiveInfoContentViewModel(NicoLiveVideo, TextInputDialogService);
					break;
				case LiveVideoPaneContentType.Settings:
					vm = new SettingsLiveInfoContentViewModel(NicoLiveVideo, HohoemaApp);
					break;
				default:
					Debug.WriteLine("CreateLiveVideoPaneContent not support type: " + type.ToString());
					break;
			}

			return vm;
		}


		/// <summary>
		/// 生放送終了後などに表示するユーザーアクションの候補を再設定します。
		/// </summary>
		/// <param name="liveStatus"></param>
		private void ResetSuggestion(LiveStatusType? liveStatus)
		{
			if (liveStatus == null)
			{
				Suggestion.Value = null;
			}
			else
			{
				LiveSuggestion suggestion = null;

				suggestion = liveStatus.Value.Make(NicoLiveVideo, PageManager);

				if (suggestion == null)
				{
					Debug.WriteLine("live suggestion not support : " + liveStatus.Value.ToString());
				}

				Suggestion.Value = suggestion;
			}
		}



		#region Event Handling


		// コメントコマンドの変更を受け取る
		private void CommandEditerVM_OnCommandChanged()
		{
			var commandString = CommandEditerVM.MakeCommandsString();
			CommandString.Value = string.IsNullOrEmpty(commandString) ? "コマンド" : commandString;
		}



		// コメント投稿の結果を受け取る
		private void NicoLiveVideo_PostCommentResult(NicoLiveVideo sender, bool postSuccess)
		{
			NowSubmittingComment.Value = false;

			if (postSuccess)
			{
				WritingComment.Value = "";
			}
		}



		// 配信の次枠を自動で開く
		private async void NicoLiveVideo_NextLive(NicoLiveVideo sender, string liveId)
		{
			var livePagePayload = new LiveVidePagePayload(liveId)
			{
				LiveTitle = this.LiveTitle,
				CommunityId = this.CommunityId,
				CommunityName = this.CommunityName
			};

			await Task.Delay(TimeSpan.FromSeconds(3));

			PageManager.OpenPage(
				HohoemaPageType.LiveVideoPlayer,
				livePagePayload.ToParameterString()
				);
		}



		#endregion

	}



	public enum LiveVideoPaneContentType
	{
		Summary,
		Comment,
		Shere,
		Settings,
	}

	
}

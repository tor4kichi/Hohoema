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
using Windows.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Playback;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using NicoPlayerHohoema.ViewModels.PlayerSidePaneContent;

namespace NicoPlayerHohoema.ViewModels
{
	public class LiveVideoPlayerControlViewModel : HohoemaViewModelBase, IDisposable
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
			= TimeSpan.FromSeconds(1);




		public TextInputDialogService _TextInputDialogService { get; private set; }
        private ToastNotificationService _ToastNotificationService;


        public MediaPlayer MediaPlayer { get; private set; }

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
        public ReactiveProperty<MediaElementState> CurrentState { get; private set; }
        public ReactiveProperty<bool> NowPlaying { get; private set; }
		public ReactiveProperty<bool> NowUpdating { get; private set; }
		public ReactiveProperty<bool> NowConnecting { get; private set; }

		public ReactiveProperty<uint> CommentCount { get; private set; }
		public ReactiveProperty<uint> WatchCount { get; private set; }

        public ReactiveProperty<LivePlayerType?> LivePlayerType { get; private set; }


        public ReactiveProperty<bool> CanChangeQuality { get; private set; }
        public ReactiveProperty<string> RequestQuality { get; private set; }

        public ReactiveProperty<string> CurrentQuality { get; private set; }

        public ReactiveProperty<bool> IsAvailableSuperLowQuality { get; }
        public ReactiveProperty<bool> IsAvailableLowQuality { get; }
        public ReactiveProperty<bool> IsAvailableNormalQuality { get; }
        public ReactiveProperty<bool> IsAvailableHighQuality { get; }

        public DelegateCommand<string> ChangeQualityCommand { get; }

        // comment


        public ReactiveProperty<bool> IsVisibleComment { get; private set; }
        public ReactiveProperty<bool> IsCommentDisplayEnable { get; private set; }
        public ReactiveProperty<TimeSpan> CommentUpdateInterval { get; private set; }
		public ReactiveProperty<TimeSpan> RequestCommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }

		public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
		public ReactiveProperty<Color> CommentDefaultColor { get; private set; }

        public ReadOnlyReactiveProperty<double> CommentOpacity { get; private set; }

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
        public ReactiveProperty<bool> IsDisplayControlUI { get; private set; }
        public ReactiveProperty<bool> IsMouseCursolAutoHideEnable { get; private set; }
        public ReactiveProperty<bool> IsFullScreen { get; private set; }
        public ReactiveProperty<bool> IsCompactOverlay { get; private set; }
        public ReactiveProperty<bool> IsForceLandscape { get; private set; }
        public ReactiveProperty<bool> IsSmallWindowModeEnable { get; private set; }


		// suggestion
		public ReactiveProperty<LiveSuggestion> Suggestion { get; private set; }
		public ReactiveProperty<bool> HasSuggestion { get; private set; }


        // Side Pane Content


		public LiveVideoPlayerControlViewModel(
            HohoemaApp hohoemaApp, 
            PageManager pageManager, 
            TextInputDialogService textInputDialogService,
            ToastNotificationService toast
            )
            : base(hohoemaApp, pageManager)
		{
			_TextInputDialogService = textInputDialogService;
            _ToastNotificationService = toast;

            MediaPlayer = HohoemaApp.MediaPlayer;

            // play
            CurrentState = new ReactiveProperty<MediaElementState>();
            NowPlaying = CurrentState.Select(x => x == MediaElementState.Playing)
                .ToReactiveProperty();


            NowUpdating = new ReactiveProperty<bool>(false);
            LivePlayerType = new ReactiveProperty<Models.Live.LivePlayerType?>();

            CanChangeQuality = new ReactiveProperty<bool>(false);
            RequestQuality = new ReactiveProperty<string>();
            CurrentQuality = new ReactiveProperty<string>();

            IsAvailableSuperLowQuality = new ReactiveProperty<bool>(false);
            IsAvailableLowQuality = new ReactiveProperty<bool>(false);
            IsAvailableNormalQuality = new ReactiveProperty<bool>(false);
            IsAvailableHighQuality = new ReactiveProperty<bool>(false);

            ChangeQualityCommand = new DelegateCommand<string>(
                (quality) => 
                {
                    NicoLiveVideo.ChangeQualityRequest(quality).ConfigureAwait(false);
                    HohoemaApp.UserSettings.PlayerSettings.DefaultLiveQuality = quality;
                }, 
                (quality) => NicoLiveVideo.Qualities.Any(x => x == quality)
            );

            IsCommentDisplayEnable = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsCommentDisplay_Live, PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);
            IsVisibleComment =
                Observable.CombineLatest(
                    HohoemaApp.Playlist.ObserveProperty(x => x.IsPlayerFloatingModeEnable).Select(x => !x),
                    IsCommentDisplayEnable
                    )
                    .Select(x => x.All(y => y))
                    .ToReactiveProperty(PlayerWindowUIDispatcherScheduler);

            CommentCanvasHeight = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0.0).AddTo(_CompositeDisposable);
			CommentDefaultColor = new ReactiveProperty<Color>(PlayerWindowUIDispatcherScheduler, Colors.White).AddTo(_CompositeDisposable);

            CommentOpacity = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentOpacity)
                .Select(x => x.ToOpacity())
                .ToReadOnlyReactiveProperty(eventScheduler: PlayerWindowUIDispatcherScheduler);


            // post comment
            WritingComment = new ReactiveProperty<string>(PlayerWindowUIDispatcherScheduler, "").AddTo(_CompositeDisposable);
			NowCommentWriting = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler).AddTo(_CompositeDisposable);
			NowSubmittingComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler).AddTo(_CompositeDisposable);

			// TODO: ニコ生での匿名コメント設定
			CommandString = new ReactiveProperty<string>(PlayerWindowUIDispatcherScheduler, "").AddTo(_CompositeDisposable);
			CommandEditerVM = new CommentCommandEditerViewModel();
			CommandEditerVM.OnCommandChanged += CommandEditerVM_OnCommandChanged;
			CommandEditerVM.ChangeEnableAnonymity(true);
            CommandEditerVM.IsAnonymousDefault = true;
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

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                // プレイヤーを閉じた際のコンパクトオーバーレイの解除はPlayerWithPageContainerViewModel側で行う
                IsCompactOverlay = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler,
                    ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay,
                    ReactivePropertyMode.DistinctUntilChanged);
                IsCompactOverlay
                    .Subscribe(async isCompactOverlay =>
                    {
                        var appView = ApplicationView.GetForCurrentView();
                        if (appView.IsViewModeSupported(ApplicationViewMode.CompactOverlay))
                        {
                            if (isCompactOverlay)
                            {
                                ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                                compactOptions.CustomSize = new Windows.Foundation.Size(500, 280);

                                var result = await appView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, compactOptions);
                                if (result)
                                {
                                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                                    appView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                                    appView.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                                    IsDisplayControlUI.Value = false;
                                }
                            }
                            else
                            {
                                var result = await appView.TryEnterViewModeAsync(ApplicationViewMode.Default);
                                if (result)
                                {
                                    CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
                                }
                            }
                        }
                    })
                    .AddTo(_CompositeDisposable);
            }
            else
            {
                IsCompactOverlay = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
            }
                

            IsSmallWindowModeEnable = HohoemaApp.Playlist
                .ToReactivePropertyAsSynchronized(x => x.IsPlayerFloatingModeEnable);

            
            Suggestion = new ReactiveProperty<LiveSuggestion>();
			HasSuggestion = Suggestion.Select(x => x != null)
				.ToReactiveProperty();

            IsDisplayControlUI = HohoemaApp.Playlist.ToReactivePropertyAsSynchronized(x => x.IsDisplayPlayerControlUI);

            if (Util.InputCapabilityHelper.IsMouseCapable && !IsForceTVModeEnable.Value)
            {
                IsAutoHideEnable = Observable.CombineLatest(
                    NowPlaying,
                    NowCommentWriting.Select(x => !x)
                    )
                .Select(x => x.All(y => y))
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

                IsMouseCursolAutoHideEnable = Observable.CombineLatest(
                    IsDisplayControlUI.Select(x => !x),
                    IsSmallWindowModeEnable.Select(x => !x)
                    )
                    .Select(x => x.All(y => y))
                    .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                    .AddTo(_CompositeDisposable);
            }
            else
            {
                IsAutoHideEnable = new ReactiveProperty<bool>(false);
                IsMouseCursolAutoHideEnable = new ReactiveProperty<bool>(false);
            }

            AutoHideDelayTime = new ReactiveProperty<TimeSpan>(TimeSpan.FromSeconds(3));



            IsMuted = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsMute, PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);
            IsMuted.Subscribe(isMuted => 
            {
                MediaPlayer.IsMuted = isMuted;
            })
            .AddTo(_CompositeDisposable);

            SoundVolume = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.SoundVolume, PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);
            SoundVolume.Subscribe(volume =>
            {
                MediaPlayer.Volume = volume;
            })
            .AddTo(_CompositeDisposable);

            CommentUpdateInterval = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
                .Select(x => TimeSpan.FromSeconds(1.0 / x))
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);

            RequestCommentDisplayDuration = HohoemaApp.UserSettings.PlayerSettings
                .ObserveProperty(x => x.CommentDisplayDuration)
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            CommentFontScale = HohoemaApp.UserSettings.PlayerSettings
                .ObserveProperty(x => x.DefaultCommentFontScale)
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);


            IsForceLandscape = HohoemaApp.UserSettings.PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsForceLandscape);



            // Side Pane

            CurrentSidePaneContentType = new ReactiveProperty<PlayerSidePaneContentType?>(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);
            CurrentSidePaneContent = CurrentSidePaneContentType
                .Select(GetSidePaneContent)
                .ToReadOnlyReactiveProperty()
                .AddTo(_CompositeDisposable);


            CurrentSidePaneContent.Subscribe(async content =>
            {
                if (_PrevSidePaneContent != null)
                {
                    _PrevSidePaneContent.OnLeave();
                }

                if (content != null)
                {
                    await content.OnEnter();
                    _PrevSidePaneContent = content;
                }
            });


            HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.DefaultLiveQuality)
                .Subscribe(x => 
                {
                    if (NicoLiveVideo != null)
                    {
                        NicoLiveVideo.ChangeQualityRequest(x).ConfigureAwait(false);
                    }
                })
                .AddTo(_CompositeDisposable);
        }







        #region Command

        private DelegateCommand _ClosePlayerCommand;
        public DelegateCommand ClosePlayerCommand
        {
            get
            {
                return _ClosePlayerCommand
                    ?? (_ClosePlayerCommand = new DelegateCommand(() =>
                    {
                        HohoemaApp.Playlist.IsDisplayPlayer = false;
                    }
                    ));
            }
        }

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
							await NicoLiveVideo.Refresh();

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
                        MediaPlayer.IsMuted = IsMuted.Value;
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


        private DelegateCommand _ToggleDisplayCommentCommand;
        public DelegateCommand ToggleDisplayCommentCommand
        {
            get
            {
                return _ToggleDisplayCommentCommand
                    ?? (_ToggleDisplayCommentCommand = new DelegateCommand(() =>
                    {
                        IsCommentDisplayEnable.Value = !IsCommentDisplayEnable.Value;
                    }));
            }
        }


        private DelegateCommand _ToggleFullScreenCommand;
        public DelegateCommand ToggleFullScreenCommand
        {
            get
            {
                return _ToggleFullScreenCommand
                    ?? (_ToggleFullScreenCommand = new DelegateCommand(() =>
                    {
                        IsFullScreen.Value = !IsFullScreen.Value;
                    }
                    ));
            }
        }


        private DelegateCommand _ToggleCompactOverlayCommand;
        public DelegateCommand ToggleCompactOverlayCommand
        {
            get
            {
                return _ToggleCompactOverlayCommand
                    ?? (_ToggleCompactOverlayCommand = new DelegateCommand(() =>
                    {
                        IsCompactOverlay.Value = !IsCompactOverlay.Value;
                    }
                    , () => ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4)
                    ));
            }
        }


        private DelegateCommand _PlayerSmallWindowDisplayCommand;
        public DelegateCommand PlayerSmallWindowDisplayCommand
        {
            get
            {
                return _PlayerSmallWindowDisplayCommand
                    ?? (_PlayerSmallWindowDisplayCommand = new DelegateCommand(() =>
                    {
                        HohoemaApp.Playlist.IsPlayerFloatingModeEnable = true;
                    }
                    ));
            }
        }


        private DelegateCommand _ShareCommand;
        public DelegateCommand ShareCommand
        {
            get
            {
                return _ShareCommand
                    ?? (_ShareCommand = new DelegateCommand(() =>
                    {
                        ShareHelper.Share(NicoLiveVideo);
                    }
                    ));
            }
        }

        private DelegateCommand _ShereWithTwitterCommand;
        public DelegateCommand ShereWithTwitterCommand
        {
            get
            {
                return _ShereWithTwitterCommand
                    ?? (_ShereWithTwitterCommand = new DelegateCommand(async () =>
                    {
                        await ShareHelper.ShareToTwitter(NicoLiveVideo);
                    }
                    ));
            }
        }

        private DelegateCommand _ShareWithClipboardCommand;
        public DelegateCommand ShareWithClipboardCommand
        {
            get
            {
                return _ShareWithClipboardCommand
                    ?? (_ShareWithClipboardCommand = new DelegateCommand(() =>
                    {
                        ShareHelper.CopyToClipboard(NicoLiveVideo);
                    }
                    ));
            }
        }



        private DelegateCommand _OpenBroadcastCommunityCommand;
        public DelegateCommand OpenBroadcastCommunityCommand
        {
            get
            {
                return _OpenBroadcastCommunityCommand
                    ?? (_OpenBroadcastCommunityCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.Community, CommunityId);
                        HohoemaApp.Playlist.IsPlayerFloatingModeEnable = true;
                    }
                    ));
            }
        }


        #endregion




        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			if (e.Parameter is string)
			{
				var json = e.Parameter as string;
				var payload = LiveVideoPagePayload.FromParameterString(json);

				LiveId = payload.LiveId;
				LiveTitle = payload.LiveTitle;
				CommunityId = payload.CommunityId;
				CommunityName = payload.CommunityName;
			}
			
			if (LiveId != null)
			{
				NicoLiveVideo = new NicoLiveVideo(LiveId, HohoemaApp);

				NowConnecting = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
					.AddTo(_NavigatingCompositeDisposable);
				RaisePropertyChanged(nameof(NowConnecting));

				LiveComments = NicoLiveVideo.LiveComments.ToReadOnlyReactiveCollection(x =>
				{
					var comment = new Views.Comment(HohoemaApp.UserSettings.NGSettings);
                    //x.GetVposでサーバー上のコメント位置が取れるが、
                    // 受け取った順で表示したいのでローカルの放送時間からコメント位置を割り当てる
                    comment.VideoPosition = (uint)(MediaPlayer.PlaybackSession.Position.TotalMilliseconds * 0.1) + 50;

                    // EndPositionはコメントレンダラが再計算するが、仮置きしないと表示対象として処理されない
                    comment.EndPosition = comment.VideoPosition + 500;

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
                    
					comment.ApplyCommands(x.ParseCommandTypes());

					return comment;
				});

				RaisePropertyChanged(nameof(LiveComments));

				CommentCount = NicoLiveVideo.ObserveProperty(x => x.CommentCount)
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
					.AddTo(_NavigatingCompositeDisposable);
				RaisePropertyChanged(nameof(CommentCount));

				WatchCount = NicoLiveVideo.ObserveProperty(x => x.WatchCount)
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
					.AddTo(_NavigatingCompositeDisposable);
				RaisePropertyChanged(nameof(WatchCount));

				CommunityId = NicoLiveVideo.BroadcasterCommunityId;


				// post comment 
				NicoLiveVideo.PostCommentResult += NicoLiveVideo_PostCommentResult;

				// operation command
				PermanentDisplayText = NicoLiveVideo.ObserveProperty(x => x.PermanentDisplayText)
					.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
					.AddTo(_NavigatingCompositeDisposable);
				RaisePropertyChanged(nameof(PermanentDisplayText));


				// next live
				NicoLiveVideo.NextLive += NicoLiveVideo_NextLive;

                NicoLiveVideo.OpenLive += NicoLiveVideo_OpenLive;
                NicoLiveVideo.CloseLive += NicoLiveVideo_CloseLive;
                NicoLiveVideo.FailedOpenLive += NicoLiveVideo_FailedOpenLive1; ;

            }

			base.OnNavigatedTo(e, viewModelState);
		}

        private void NicoLiveVideo_FailedOpenLive1(NicoLiveVideo sender, OpenLiveFailedReason reason)
        {
            NowConnecting.Value = false;

            this.ResetSuggestion(reason);
        }

        private void NicoLiveVideo_CloseLive(NicoLiveVideo sender)
        {
            NowConnecting.Value = false;
        }

        private void NicoLiveVideo_OpenLive(NicoLiveVideo sender)
        {
            NowConnecting.Value = false;
        }

        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.LoggedIn);
			
			await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}

       

        protected override async Task OnSignIn(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
        {
            try
            {
                await TryStartViewing();

                cancelToken.ThrowIfCancellationRequested();
            }
            catch
            {
                NicoLiveVideo?.Dispose();
                NicoLiveVideo = null;

                await StopLiveElapsedTimer().ConfigureAwait(false);

                throw;
            }
            
//			base.OnSignIn(userSessionDisposer);
		}


		protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			if (!suspending)
			{
				NicoLiveVideo.Dispose();
				NicoLiveVideo = null;
			}

            MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

			IsFullScreen.Value = false;
			StopLiveElapsedTimer().ConfigureAwait(false);

			base.OnHohoemaNavigatingFrom(e, viewModelState, suspending);
		}

		protected override async Task OnResumed()
		{
			await TryStartViewing();

//			return base.OnResumed();
		}

		/// <summary>
		/// 生放送情報を取得してライブストリームの受信を開始します。<br />
		/// 配信受信処理のハードリセット動作として機能します。
		/// </summary>
		/// <returns></returns>
		private async Task TryStartViewing()
		{
			if (NicoLiveVideo == null) { return; }

            NowConnecting.Value = true;

            try
			{
                MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

				NowUpdating.Value = true;

				var liveStatus = await NicoLiveVideo.SetupLive();

				if (liveStatus == null)
				{
                    LivePlayerType.Value = NicoLiveVideo.LivePlayerType;

                    RaisePropertyChanged(nameof(MediaPlayer));
					_StartAt = NicoLiveVideo.PlayerStatusResponse.Program.StartedAt;
					_EndAt = NicoLiveVideo.PlayerStatusResponse.Program.EndedAt;

					await StartLiveElapsedTimer();

					LiveTitle = NicoLiveVideo.LiveTitle;
                    Title = LiveTitle;
                    CommunityId = NicoLiveVideo.BroadcasterCommunityId;

					// seet
					RoomName = NicoLiveVideo.PlayerStatusResponse.Room.Name;
					SeetId = NicoLiveVideo.PlayerStatusResponse.Room.SeatId;

					RaisePropertyChanged(nameof(NicoLiveVideo));

                    if (CommunityName == null)
                    {
                        if (CommunityId == null)
                        {
                            CommunityId = NicoLiveVideo.BroadcasterCommunityId;
                        }

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

                    if (NicoLiveVideo.LivePlayerType == Models.Live.LivePlayerType.Leo)
                    {
                        CanChangeQuality.Value = true;

                        NicoLiveVideo.ObserveProperty(x => x.RequestQuality)
                            .Subscribe(q => 
                            {
                                RequestQuality.Value = q;
                            });

                        NicoLiveVideo.ObserveProperty(x => x.CurrentQuality)
                            .Subscribe(x => 
                            {
                                CurrentQuality.Value = x;
                            });

                        NicoLiveVideo.ObserveProperty(x => x.Qualities)
                            .Subscribe(types => 
                            {
                                IsAvailableSuperLowQuality.Value    = types?.Any(x => x == "super_low") ?? false;
                                IsAvailableLowQuality.Value         = types?.Any(x => x == "low") ?? false;
                                IsAvailableNormalQuality.Value      = types?.Any(x => x == "normal") ?? false;
                                IsAvailableHighQuality.Value        = types?.Any(x => x == "high") ?? false;

                                var sidePaneContent = _SidePaneContentCache.ContainsKey(PlayerSidePaneContentType.Setting) ? _SidePaneContentCache[PlayerSidePaneContentType.Setting] : null;
                                if (sidePaneContent != null && NicoLiveVideo != null)
                                {
                                    (sidePaneContent as SettingsSidePaneContentViewModel).SetupAvairableLiveQualities(
                                        NicoLiveVideo.Qualities
                                        );
                                    (sidePaneContent as SettingsSidePaneContentViewModel).IsLeoPlayerLive = true;
                                }

                                ChangeQualityCommand.RaiseCanExecuteChanged();
                            });
                    }

                    if (!Util.InputCapabilityHelper.IsMouseCapable)
                    {
                        IsDisplayControlUI.Value = false;
                    }
                }
                else
				{
					Debug.WriteLine("生放送情報の取得失敗しました "  + LiveId);
                    NowConnecting.Value = false;
                }

				ResetSuggestion(liveStatus);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
                NowConnecting.Value = false;
            }
            finally
			{
				NowUpdating.Value = false;
			}

			// コメント送信中にコメントクライアント切断した場合に対応
			NowSubmittingComment.Value = false;
		}

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            switch (sender.PlaybackState)
            {
                case MediaPlaybackState.None:
                    CurrentState.Value = MediaElementState.Closed;
                    break;
                case MediaPlaybackState.Opening:
                    CurrentState.Value = MediaElementState.Opening;
                    break;
                case MediaPlaybackState.Buffering:
                    CurrentState.Value = MediaElementState.Buffering;
                    break;
                case MediaPlaybackState.Playing:
                    CurrentState.Value = MediaElementState.Playing;
                    break;
                case MediaPlaybackState.Paused:
                    CurrentState.Value = MediaElementState.Paused;
                    break;
                default:
                    break;
            }
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
			await HohoemaApp.UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, async () => 
			{
                using (var releaser = await _LiveElapsedTimeUpdateTimerLock.LockAsync())
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

                }
            });
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

        private void ResetSuggestion(OpenLiveFailedReason reason)
        {
            LiveSuggestion suggestion = null;

            if (reason == OpenLiveFailedReason.VideoQuoteIsNotSupported)
            {
                // ブラウザ視聴を案内
                suggestion = new LiveSuggestion("動画引用放送は対応していません", new[] { new SuggestAction("ブラウザで視聴", async () =>
                {
                    var livePageUrl = new Uri($"http://live.nicovideo.jp/watch/" + LiveId);
                    await Launcher.LaunchUriAsync(livePageUrl );
                })});
            }
            else if (reason == OpenLiveFailedReason.TimeOver)
            {
                suggestion = new LiveSuggestion("放送受信に失敗しました", new[] { new SuggestAction("再試行", async () => 
                {
                    await TryStartViewing();
                })});
            }

            if (suggestion == null)
            {
                Debug.WriteLine("live suggestion not support : " + reason.ToString());
            }

            Suggestion.Value = suggestion;
            
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
			await Task.Delay(TimeSpan.FromSeconds(3));

            HohoemaApp.Playlist.PlayLiveVideo(liveId, LiveTitle);
        }



        #endregion



        #region Side Pane Content


        private Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase> _SidePaneContentCache = new Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase>();

        public ReactiveProperty<PlayerSidePaneContentType?> CurrentSidePaneContentType { get; }
        public ReadOnlyReactiveProperty<SidePaneContentViewModelBase> CurrentSidePaneContent { get; }

        SidePaneContentViewModelBase _PrevSidePaneContent;

        private DelegateCommand<object> _SelectSidePaneContentCommand;
        public DelegateCommand<object> SelectSidePaneContentCommand
        {
            get
            {
                return _SelectSidePaneContentCommand
                    ?? (_SelectSidePaneContentCommand = new DelegateCommand<object>((type) =>
                    {
                        if (type is PlayerSidePaneContentType)
                        {
                            CurrentSidePaneContentType.Value = (PlayerSidePaneContentType)type;
                        }
                        else if (type is string && Enum.TryParse<PlayerSidePaneContentType>(type as String, out var parsed))
                        {
                            CurrentSidePaneContentType.Value = parsed;
                        }
                    }));
            }
        }


        private SidePaneContentViewModelBase GetSidePaneContent(PlayerSidePaneContentType? maybeType)
        {
            PlayerSidePaneContentType type = PlayerSidePaneContentType.None;
            if (maybeType.HasValue)
            {
                type = maybeType.Value;
            }

            if (_SidePaneContentCache.ContainsKey(type))
            {
                return _SidePaneContentCache[type];
            }
            else
            {
                SidePaneContentViewModelBase sidePaneContent = null;
                switch (type)
                {
                    case PlayerSidePaneContentType.Playlist:
                        sidePaneContent = new PlayerSidePaneContent.PlaylistSidePaneContentViewModel(HohoemaApp.MediaPlayer, HohoemaApp.Playlist, HohoemaApp.UserSettings.PlaylistSettings, PageManager);
                        break;
                    case PlayerSidePaneContentType.Comment:
                        throw new NotImplementedException();
//                        sidePaneContent = new PlayerSidePaneContent.CommentSidePaneContentViewModel(HohoemaApp.UserSettings, LiveComments);
//                        break;
                    case PlayerSidePaneContentType.Setting:
                        sidePaneContent = new PlayerSidePaneContent.SettingsSidePaneContentViewModel(HohoemaApp.UserSettings);
                        if (NicoLiveVideo != null)
                        {
                            (sidePaneContent as SettingsSidePaneContentViewModel).SetupAvairableLiveQualities(
                                NicoLiveVideo.Qualities
                                );
                            (sidePaneContent as SettingsSidePaneContentViewModel).IsLeoPlayerLive = NicoLiveVideo.LivePlayerType == Models.Live.LivePlayerType.Leo;
                        }
                        break;
                    default:
                        sidePaneContent = new PlayerSidePaneContent.EmptySidePaneContentViewModel();
                        break;
                }

                _SidePaneContentCache.Add(type, sidePaneContent);
                return sidePaneContent;
            }
        }



        #endregion
    }	
}

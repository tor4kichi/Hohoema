using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Models.LocalMylist;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Models.Niconico.Video;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.ViewModels.PlayerSidePaneContent;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Metadata;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.ViewModels
{

    public class VideoPlayerPageViewModel : HohoemaViewModelBase, Interfaces.IVideoContent, INavigatedAwareAsync
	{
        // TODO: HohoemaViewModelBaseとの依存性を排除（ViewModelBaseとの関係性は維持）

        public VideoPlayerPageViewModel(
            IScheduler scheduler,
            IEventAggregator eventAggregator,
            NicoVideoStreamingSessionProvider nicoVideo,
            VideoCacheManager videoCacheManager,
            UserMylistManager userMylistManager,
            LocalMylistManager localMylistManager,
            Models.Subscription.SubscriptionManager subscriptionManager,
            Models.NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider,
            ChannelProvider channelProvider,
            MylistProvider mylistProvider,
            PlayerSettings playerSettings,
            PlaylistSettings playlistSettings,
            CacheSettings cacheSettings,
            NGSettings ngSettings,
            AppearanceSettings appearanceSettings,
            Services.HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            PlayerViewManager playerViewManager,
            NotificationService notificationService,
            DialogService dialogService,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand,
            Commands.Mylist.CreateLocalMylistCommand createLocalMylistCommand,
            Commands.Mylist.CreateMylistCommand createMylistCommand
            )
        {
            Scheduler = scheduler;
            EventAggregator = eventAggregator;
            NicoVideo = nicoVideo;
            VideoCacheManager = videoCacheManager;
            UserMylistManager = userMylistManager;
            LocalMylistManager = localMylistManager;
            SubscriptionManager = subscriptionManager;
            NiconicoSession = niconicoSession;
            NicoVideoProvider = nicoVideoProvider;
            ChannelProvider = channelProvider;
            MylistProvider = mylistProvider;
            PlayerSettings = playerSettings;
            PlaylistSettings = playlistSettings;
            CacheSettings = cacheSettings;
            NgSettings = ngSettings;
            AppearanceSettings = appearanceSettings;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            PlayerViewManager = playerViewManager;
            _NotificationService = notificationService;
            _HohoemaDialogService = dialogService;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
            CreateLocalMylistCommand = createLocalMylistCommand;
            CreateMylistCommand = createMylistCommand;
            MediaPlayer = playerViewManager.GetCurrentWindowMediaPlayer();

            NicoScript_Default_Enabled = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.NicoScript_Default_Enabled, raiseEventScheduler: Scheduler)
                .AddTo(_CompositeDisposable);
            NicoScript_DisallowSeek_Enabled = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.NicoScript_DisallowSeek_Enabled, raiseEventScheduler: Scheduler)
                .AddTo(_CompositeDisposable);
            NicoScript_Jump_Enabled = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.NicoScript_Jump_Enabled, raiseEventScheduler: Scheduler)
                .AddTo(_CompositeDisposable);
            NicoScript_Replace_Enabled = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.NicoScript_Replace_Enabled, raiseEventScheduler: Scheduler)
                .AddTo(_CompositeDisposable);

            NicoScript_Default_Enabled.Subscribe(async x =>
            {
                if (_DefaultCommandNicoScriptList.Any())
                {
                    await UpdateComments();
                }
            });

            ThumbnailUri = new ReactiveProperty<string>(Scheduler);

            CurrentVideoPosition = new ReactiveProperty<TimeSpan>(Scheduler, TimeSpan.Zero)
                .AddTo(_CompositeDisposable);
            ReadVideoPosition = new ReactiveProperty<TimeSpan>(Scheduler, TimeSpan.Zero);
            //				.AddTo(_CompositeDisposable);
            CommentVideoPosition = new ReactiveProperty<TimeSpan>(Scheduler, TimeSpan.Zero)
                .AddTo(_CompositeDisposable);
            NowSubmittingComment = new ReactiveProperty<bool>(Scheduler)
                .AddTo(_CompositeDisposable);
            SliderVideoPosition = new ReactiveProperty<double>(Scheduler, 0, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);
            VideoLength = new ReactiveProperty<double>(Scheduler, 0)
                .AddTo(_CompositeDisposable);
            CurrentState = new ReactiveProperty<MediaPlaybackState>(Scheduler)
                .AddTo(_CompositeDisposable);
            NowBuffering = CurrentState.Select(x => x == MediaPlaybackState.Buffering || x == MediaPlaybackState.Opening)
                .ToReadOnlyReactiveProperty(eventScheduler: Scheduler)
                .AddTo(_CompositeDisposable);


            IsSeekDisabledFromNicoScript = new ReactiveProperty<bool>(Scheduler, false);
            IsCommentDisabledFromNicoScript = new ReactiveProperty<bool>(Scheduler, false);

            IsPlayWithCache = new ReactiveProperty<bool>(Scheduler, false)
                .AddTo(_CompositeDisposable);

            IsNeedResumeExitWrittingComment = new ReactiveProperty<bool>(Scheduler);


            NowQualityChanging = new ReactiveProperty<bool>(Scheduler, false);
            Comments = new ObservableCollection<Comment>();

            CanSubmitComment = new ReactiveProperty<bool>(Scheduler, false);
            NowCommentWriting = new ReactiveProperty<bool>(Scheduler, false)
                .AddTo(_CompositeDisposable);
            NowSoundChanging = new ReactiveProperty<bool>(Scheduler, false)
                .AddTo(_CompositeDisposable);
            IsCommentDisplayEnable = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsCommentDisplay_Video, Scheduler)
                .AddTo(_CompositeDisposable);

            IsEnableRepeat = new ReactiveProperty<bool>(Scheduler, false)
                .AddTo(_CompositeDisposable);



            WritingComment = new ReactiveProperty<string>(Scheduler, "")
                .AddTo(_CompositeDisposable);


            NowCanSeek = Observable.CombineLatest(
                NowQualityChanging.Select(x => !x),
                Observable.CombineLatest(
                    NicoScript_DisallowSeek_Enabled,
                    IsSeekDisabledFromNicoScript.Select(x => !x)
                    )
                    .Select(x => x[0] ? x[1] : true)
                )
                .Select(x => x.All(y => y))
                .ToReadOnlyReactiveProperty(eventScheduler: Scheduler);

            SeekVideoCommand = NowCanSeek.ToReactiveCommand<TimeSpan?>(scheduler: Scheduler);
            SeekVideoCommand.Subscribe(time =>
            {
                if (!time.HasValue) { return; }
                var session = MediaPlayer.PlaybackSession;
                session.Position += time.Value;
            });

            NowCanSubmitComment = Observable.CombineLatest(
                NowSubmittingComment.Select(x => !x),
                CanSubmitComment,
                IsCommentDisabledFromNicoScript.Select(x => PlayerSettings.NicoScript_DisallowComment_Enabled ? !x : true),
                WritingComment.Select(x => !string.IsNullOrWhiteSpace(x))
                )
                .Select(x => x.All(y => y))
                .ToReadOnlyReactiveProperty(eventScheduler: Scheduler);

            CommentSubmitCommand = NowCanSubmitComment
                .ToReactiveCommand(Scheduler)
                .AddTo(_CompositeDisposable);

            CommentSubmitCommand.Subscribe(async x => await SubmitComment())
                .AddTo(_CompositeDisposable);

            NowCommentWriting.Subscribe(x => Debug.WriteLine("NowCommentWriting:" + NowCommentWriting.Value))
                .AddTo(_CompositeDisposable);


            NowCommentWriting
                .Subscribe(isWritting =>
                {
                    if (IsPauseWithCommentWriting?.Value ?? false)
                    {
                        if (isWritting)
                        {
                            MediaPlayer.Pause();
                            IsNeedResumeExitWrittingComment.Value = NowPlaying.Value;
                        }
                        else
                        {
                            if (IsNeedResumeExitWrittingComment.Value)
                            {
                                MediaPlayer.Play();
                                IsNeedResumeExitWrittingComment.Value = false;
                            }

                        }
                    }
                })
            .AddTo(_CompositeDisposable);

            CommandString = new ReactiveProperty<string>(Scheduler, "")
                .AddTo(_CompositeDisposable);

            IsPauseWithCommentWriting = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting, Scheduler)
                .AddTo(_CompositeDisposable);

            CommentCanvasHeight = new ReactiveProperty<double>(Scheduler, 0);
            CommentCanvasWidth = new ReactiveProperty<double>(Scheduler, 0);

            CommentOpacity = PlayerSettings.ObserveProperty(x => x.CommentOpacity)
                .ToReadOnlyReactiveProperty(eventScheduler: Scheduler);
            





            CurrentVideoQuality = new ReactiveProperty<NicoVideoQuality?>(Scheduler, null, ReactivePropertyMode.None)
                .AddTo(_CompositeDisposable);
            RequestVideoQuality = new ReactiveProperty<NicoVideoQuality>(Scheduler, PlayerSettings.DefaultQuality, ReactivePropertyMode.None)
                .AddTo(_CompositeDisposable);

            IsCacheLegacyOriginalQuality = new ReactiveProperty<bool>(Scheduler, false, mode: ReactivePropertyMode.None);
            IsCacheLegacyLowQuality = new ReactiveProperty<bool>(Scheduler, false, mode: ReactivePropertyMode.None);

            CanToggleCacheRequestLegacyOriginalQuality = new ReactiveProperty<bool>(Scheduler, false);
            CanToggleCacheRequestLegacyLowQuality = new ReactiveProperty<bool>(Scheduler, false);



            SliderVideoPosition.Subscribe(x =>
            {
                _NowControlSlider = true;
                if (x > VideoLength.Value)
                {
                    x = VideoLength.Value;
                }

                if (!_NowReadingVideoPosition)
                {
                    CurrentVideoPosition.Value = TimeSpan.FromSeconds(x);
                    MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(x);
                }

                _NowControlSlider = false;
            })
            .AddTo(_CompositeDisposable);

            ReadVideoPosition.Subscribe(x =>
            {
                if (CurrentState.Value == MediaPlaybackState.Playing)
                {
                    PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;
                }

                if (_NowControlSlider) { return; }

                _NowReadingVideoPosition = true;

                SliderVideoPosition.Value = x.TotalSeconds;

                _NowReadingVideoPosition = false;
            })
            .AddTo(_CompositeDisposable);

            NowPlaying = CurrentState
                .Select(x =>
                {
                    return
                        //						x == MediaPlaybackState.Opening ||
                        x == MediaPlaybackState.Buffering ||
                        x == MediaPlaybackState.Playing;
                })
                .ToReactiveProperty(Scheduler)
                .AddTo(_CompositeDisposable);

            CurrentState
                .Subscribe(async x =>
                {
                    if (x == MediaPlaybackState.Opening)
                    {
                    }
                    else if (x == MediaPlaybackState.Playing && NowQualityChanging.Value)
                    {
                        NowQualityChanging.Value = false;
                        //					SliderVideoPosition.Value = PreviousVideoPosition;
                        CurrentVideoPosition.Value = TimeSpan.FromSeconds(PreviousVideoPosition);
                    }
                    else if (x == MediaPlaybackState.None)
                    {
                        if (NicoVideo != null && !_IsVideoPlayed)
                        {
                            Debug.WriteLine("再生中に動画がClosedになったため、強制的に再初期化を実行しました。これは非常措置です。");

                            this._PreviosPlayingVideoPosition = TimeSpan.FromSeconds(PreviousVideoPosition);

                            //await this.PlayingQualityChangeAction();
                        }
                    }

                    Scheduler.Schedule(() =>
                    {
                        SetKeepDisplayWithCurrentState();
                    });

                    Debug.WriteLine("player state :" + x.ToString());
                })
            .AddTo(_CompositeDisposable);


            // 再生速度
            PlaybackRate = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PlaybackRate, Scheduler)
                .AddTo(_CompositeDisposable);
            PlaybackRate.Subscribe(x =>
            {
                MediaPlayer.PlaybackSession.PlaybackRate = x;
            })
            .AddTo(_CompositeDisposable);

            SetPlaybackRateCommand = new DelegateCommand<double?>(
                (rate) => PlaybackRate.Value = rate.HasValue ? rate.Value : 1.0
                , (rate) => rate.HasValue ? rate.Value != PlaybackRate.Value : true
            );




            DownloadCompleted = new ReactiveProperty<bool>(Scheduler, false);
            ProgressPercent = new ReactiveProperty<double>(Scheduler, 0.0);
            IsFullScreen = new ReactiveProperty<bool>(Scheduler, false, ReactivePropertyMode.DistinctUntilChanged);
            IsFullScreen
                .Subscribe(isFullScreen =>
                {
                    var appView = ApplicationView.GetForCurrentView();

                    IsCompactOverlay.Value = false;

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


            // プレイヤーを閉じた際のコンパクトオーバーレイの解除はPlayerWithPageContainerViewModel側で行う


            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                IsCompactOverlay = new ReactiveProperty<bool>(Scheduler,
                    ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay
                    );

                // This device supports all APIs in UniversalApiContract version 2.0
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
                                appView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                                appView.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                                IsDisplayControlUI.Value = false;
                            }
                        }
                        else
                        {
                            var result = await appView.TryEnterViewModeAsync(ApplicationViewMode.Default);
                        }
                    }
                })
                .AddTo(_CompositeDisposable);
            }
            else
            {
                IsCompactOverlay = new ReactiveProperty<bool>(Scheduler, false);
            }



            IsSmallWindowModeEnable = PlayerViewManager
                .ObserveProperty(x => x.IsPlayerSmallWindowModeEnabled)
                .ToReadOnlyReactiveProperty(eventScheduler: Scheduler)
                .AddTo(_CompositeDisposable);


            // playlist
            CurrentPlaylistName = new ReactiveProperty<string>(Scheduler, HohoemaPlaylist.CurrentPlaylist?.Label)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, Scheduler)
                .AddTo(_CompositeDisposable);



            IsTrackRepeatModeEnable = PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .ToReactiveProperty(Scheduler)
                .AddTo(_CompositeDisposable);
            IsListRepeatModeEnable = PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.List)
                .ToReactiveProperty(Scheduler)
                .AddTo(_CompositeDisposable);


            IsTrackRepeatModeEnable.Subscribe(x =>
            {
                MediaPlayer.IsLoopingEnabled = x;
            })
                .AddTo(_CompositeDisposable);

            IsDisplayControlUI = new ReactiveProperty<bool>(Scheduler, true);

            PlaylistCanGoBack = HohoemaPlaylist.Player.ObserveProperty(x => x.CanGoBack).ToReactiveProperty(Scheduler);
            PlaylistCanGoNext = HohoemaPlaylist.Player.ObserveProperty(x => x.CanGoNext).ToReactiveProperty(Scheduler);


            CurrentSidePaneContentType = new ReactiveProperty<PlayerSidePaneContentType?>(Scheduler, initialValue: _PrevSidePaneContentType)
                .AddTo(_CompositeDisposable);
            CurrentSidePaneContent = CurrentSidePaneContentType
                .Select(GetSidePaneContent)
                .ToReadOnlyReactiveProperty(eventScheduler: Scheduler)
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

            if (Services.Helpers.InputCapabilityHelper.IsMouseCapable && !AppearanceSettings.IsForceTVModeEnable)
            {
                IsAutoHideEnable = Observable.CombineLatest(
                    NowPlaying,
                    NowSoundChanging.Select(x => !x),
                    NowCommentWriting.Select(x => !x)
                    )
                .Select(x => x.All(y => y))
                .ToReactiveProperty(Scheduler)
                .AddTo(_CompositeDisposable);

                IsMouseCursolAutoHideEnable = Observable.CombineLatest(
                    IsDisplayControlUI.Select(x => !x),
                    IsSmallWindowModeEnable.Select(x => !x)
                    )
                    .Select(x => x.All(y => y))
                    .ToReactiveProperty(Scheduler)
                    .AddTo(_CompositeDisposable);
            }
            else
            {
                IsAutoHideEnable = new ReactiveProperty<bool>(Scheduler, false);
                IsMouseCursolAutoHideEnable = new ReactiveProperty<bool>(Scheduler, false);
            }

            AutoHideDelayTime = new ReactiveProperty<TimeSpan>(Scheduler, TimeSpan.FromSeconds(3));

            IsMuted = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsMute, Scheduler)
                .AddTo(_CompositeDisposable);
            MediaPlayer.IsMuted = IsMuted.Value;

            SoundVolume = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.SoundVolume, Scheduler)
                .AddTo(_CompositeDisposable);

            CommentDefaultColor = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.CommentColor, Scheduler)
                .AddTo(_CompositeDisposable);

            SoundVolume.Subscribe(volume =>
            {
                MediaPlayer.Volume = volume;
            });


            RequestUpdateInterval = PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
                .Select(x => TimeSpan.FromSeconds(1.0 / x))
                .ToReactiveProperty(Scheduler)
                .AddTo(_CompositeDisposable);

            RequestCommentDisplayDuration = PlayerSettings
                .ObserveProperty(x => x.CommentDisplayDuration)
                .ToReactiveProperty(Scheduler)
                .AddTo(_CompositeDisposable);

            CommentFontScale = PlayerSettings
                .ObserveProperty(x => x.DefaultCommentFontScale)
                .ToReactiveProperty(Scheduler)
                .AddTo(_CompositeDisposable);



            IsForceLandscape = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsForceLandscape, Scheduler);
            RaisePropertyChanged(nameof(IsForceLandscape));

        }


        const uint default_DisplayTime = 400; // 1 = 10ms, 400 = 4000ms = 4.0 Seconds

        public bool IsXbox => Services.Helpers.DeviceTypeHelper.IsXbox;

        public bool IsTVModeEnabled => AppearanceSettings.IsForceTVModeEnable || Services.Helpers.DeviceTypeHelper.IsXbox;


        ICommentSession CommentSession;

        IVideoStreamingSession _CurrentPlayingVideoSession;
        Database.NicoVideo _VideoInfo;


        public Models.Subscription.SubscriptionSource? SubscriptionSource => this._VideoInfo?.Owner != null ? (new Models.Subscription.SubscriptionSource(_VideoInfo.Owner.ScreenName, _VideoInfo.Owner.UserType == Mntone.Nico2.Videos.Thumbnail.UserType.User ? Models.Subscription.SubscriptionSourceType.User : Models.Subscription.SubscriptionSourceType.Channel, _VideoInfo.Owner.OwnerId)) : default(Models.Subscription.SubscriptionSource);
        


        




		private void SetKeepDisplayWithCurrentState()
		{
			var x = CurrentState.Value;
			if (x == MediaPlaybackState.Paused || x == MediaPlaybackState.None)
			{
				ExitKeepDisplay();
			}
			else
			{
				SetKeepDisplayIfEnable();
			}
		}

		bool _NowControlSlider = false;
		bool _NowReadingVideoPosition = false;


		private double PreviousVideoPosition;


		private void UpdadeProgress(float videoSize, float progressSize)
		{
			//ProgressFragments

			DownloadCompleted.Value = progressSize == videoSize;
			if (DownloadCompleted.Value)
			{
				ProgressPercent.Value = 100;
			}
			else
			{
				ProgressPercent.Value = Math.Round((double)progressSize / videoSize * 100, 1);
			}

		}

		



		
        /*
        protected override async Task OnResumed()
        {
            if (NowSignIn)
            {
                InitializeBufferingMonitor();
            }

            if (_IsNeddPlayInResumed)
            {
                await PlayingQualityChangeAction();
                _IsNeddPlayInResumed = false;
            }

        }
        */



        /// <summary>
        /// 再生処理
        /// </summary>
        /// <returns></returns>
        private async Task PlayingQualityChangeAction(NicoVideoQuality requestQuality = NicoVideoQuality.Unknown)
        {
            // TODO: 再生画質変更中のロックを導入する
            // 画質変更中にDisposeが掛かっても正常に破棄できるようにする

            if (requestQuality.IsLegacy() || requestQuality == NicoVideoQuality.Unknown)
            {
                if (CurrentVideoQuality.Value != NicoVideoQuality.Unknown)
                {
                    requestQuality = CurrentVideoQuality.Value.Value;
                }
                else
                {
                    requestQuality = PlayerSettings.DefaultQuality;
                }
            }
            
            if (NicoVideo == null) { return; }

            if (VideoId == null) { return; }

            NowQualityChanging.Value = true;


            // 古い再生セッションを破棄
            _CurrentPlayingVideoSession?.Dispose();

            // サポートされたメディアの再生
            CurrentState.Value = MediaPlaybackState.Opening;

            try
            {
                _CurrentPlayingVideoSession = (IVideoStreamingSession)await VideoCacheManager.CreateStreamingSessionAsync(VideoId);
                if (_CurrentPlayingVideoSession == null)
                {
                    _CurrentPlayingVideoSession = (IVideoStreamingSession)await NicoVideo.CreateStreamingSessionAsync(VideoId, requestQuality);
                }

                if (_CurrentPlayingVideoSession == null)
                {
                    throw new NotSupportedException("不明なエラーにより再生できません");
                }

                await _CurrentPlayingVideoSession.StartPlayback(MediaPlayer);

                CurrentVideoQuality.Value = _CurrentPlayingVideoSession.Quality;

                NowPlayingWithDmcVideo = false;
                CurrentDmcQualityVideoContent = null;
                if (_CurrentPlayingVideoSession is DmcVideoStreamingSession)
                {
                    var dmcSession = _CurrentPlayingVideoSession as DmcVideoStreamingSession;
                    var content = dmcSession.VideoContent;
                    if (content != null)
                    {
                        NowPlayingWithDmcVideo = true;
                        VideoWidth = content.Resolution.Width;
                        VideoHeight = content.Resolution.Height;
                        VideoBitrate = content.Bitrate;
                    }

                    VideoQualities.Clear();
                    if (dmcSession.DmcWatchResponse?.Video?.DmcInfo?.Quality != null)
                    {
                        foreach (var videoContent in dmcSession.DmcWatchResponse.Video.DmcInfo.Quality.Videos)
                        {
                            VideoQualities.Add(videoContent);
                        }
                    }

                    CurrentDmcQualityVideoContent = content;
                }

                MediaPlayer.PlaybackSession.Position = this._PreviosPlayingVideoPosition;
            }
            catch (NotSupportedException ex)
            {
                _CurrentPlayingVideoSession?.Dispose();
                _CurrentPlayingVideoSession = null;

                IsNotSupportVideoType = true;
                CannotPlayReason = ex.Message;
                CurrentState.Value = MediaPlaybackState.None;

                VideoPlayed(canPlayNext: true);

                return;
            }



            if (!IsNotSupportVideoType)
            {
                if (_CurrentPlayingVideoSession is LocalVideoStreamingSession)
                {
                    // キャッシュ再生
                    IsPlayWithCache.Value = true;
                }
                else
                {
                    // オンライン再生
                    IsPlayWithCache.Value = false;
                }
                /*
                    var isCurrentQualityCacheDownloadCompleted = false;
                    switch (CurrentVideoQuality.Value)
                    {
                        case NicoVideoQuality.Smile_Original:
                            IsSaveRequestedCurrentQualityCache.Value = Video.OriginalQuality.IsCacheRequested;
                            isCurrentQualityCacheDownloadCompleted = Video.OriginalQuality.IsCached;
                            break;
                        case NicoVideoQuality.Smile_Low:
                            IsSaveRequestedCurrentQualityCache.Value = Video.LowQuality.IsCacheRequested;
                            isCurrentQualityCacheDownloadCompleted = Video.LowQuality.IsCached;
                            break;
                        default:
                            IsSaveRequestedCurrentQualityCache.Value = false;
                            break;
                    }
                    */



                MediaPlayer.PlaybackSession.PlaybackRate =
                    PlayerSettings.PlaybackRate;

                // リクエストどおりの画質が再生された場合、画質をデフォルトとして設定する
                if (RequestVideoQuality.Value == CurrentVideoQuality.Value)
                {
                    if (CurrentVideoQuality.Value.HasValue)
                    {
                        PlayerSettings.DefaultQuality = CurrentVideoQuality.Value.Value;
                    }
                }

                if (MediaPlayer.AutoPlay)
                {
                    IsDisplayControlUI.Value = false;
                }
            }
        }


        

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
			Debug.WriteLine("VideoPlayer OnNavigatedToAsync start.");

            IsDisplayControlUI.Value = true;


            VideoId = parameters.GetValue<string>("id");

            if (parameters.TryGetValue("quality", out NicoVideoQuality quality))
            {
                RequestVideoQuality.Value = quality;
            }
            else if (parameters.TryGetValue("quality", out string qualityString))
            {
                if (Enum.TryParse(qualityString, out quality))
                {
                    RequestVideoQuality.Value = quality;
                }
            }
            else
            {
                RequestVideoQuality.Value = PlayerSettings.DefaultQuality;
            }
            

            // 先にプレイリストのセットアップをしないと
            // 再生に失敗した時のスキップ処理がうまく動かない
            CurrentPlaylist = HohoemaPlaylist.CurrentPlaylist;
            CurrentPlaylistName.Value = CurrentPlaylist.Label;
            PlaylistItems = CurrentPlaylist.ToReadOnlyReactiveCollection(
                Observable.Empty<CollectionChanged<string>>(),
                videoId =>
                {
                    var video = Database.NicoVideoDb.Get(videoId);
                    return new PlaylistItem()
                    {
                        ContentId = video.RawVideoId,
                        Title = video.Title,
                        Type = PlaylistItemType.Video,
                        Owner = CurrentPlaylist
                    };
                },
                Scheduler
                )
                .AddTo(_NavigatingCompositeDisposable);

            RaisePropertyChanged(nameof(PlaylistItems));

            // 削除状態をチェック（再生準備より先に行う）
            _VideoInfo = Database.NicoVideoDb.Get(VideoId);
            await CheckDeleted(_VideoInfo);

            MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            MediaPlayer.AutoPlay = true;
            await this.PlayingQualityChangeAction(PlayerSettings.DefaultQuality);

            
            // そのあとで表示情報を取得
            _VideoInfo = await NicoVideoProvider.GetNicoVideoInfo(VideoId)
                ?? Database.NicoVideoDb.Get(VideoId);

            // 改めて削除状態をチェック（動画リスト経由してない場合の削除チェック）
            await CheckDeleted(_VideoInfo);

            VideoTitle = _VideoInfo.Title;
            ThumbnailUri.Value = _VideoInfo.ThumbnailUrl;

            VideoLength.Value = _VideoInfo.Length.TotalSeconds;

            
            // 削除された動画の場合、自動でスキップさせる
            if (_VideoInfo.IsDeleted)
            {
                VideoPlayed(canPlayNext: true);
            }

            // コメントやキャッシュ状況の表示を更新
            if (IsNotSupportVideoType)
            {
                // コメント入力不可
                NowSubmittingComment.Value = true;
                CanSubmitComment.Value = false;
            }
            else
            {
                CommentSession = await NicoVideo.CreateCommentSessionAsync(VideoId);

                // コメントの更新
                await UpdateComments();

                // コメント送信を有効に
                CanSubmitComment.Value = true;

                // コメントのコマンドエディタを初期化
                CommandEditerVM = new CommentCommandEditerViewModel()
                    .AddTo(_CompositeDisposable);

                RaisePropertyChanged(nameof(CommandEditerVM));

                CommandEditerVM.OnCommandChanged += () => UpdateCommandString();
                CommandEditerVM.IsPremiumUser = NiconicoSession.IsPremiumAccount;

                CommandEditerVM.IsAnonymousDefault = PlayerSettings.IsDefaultCommentWithAnonymous;
                CommandEditerVM.IsAnonymousComment.Value = PlayerSettings.IsDefaultCommentWithAnonymous;

                // コミュニティやチャンネルの動画では匿名コメントは利用できない
                //CommandEditerVM.ChangeEnableAnonymity(CommentClient.IsAllowAnnonimityComment);

                UpdateCommandString();

                // キャッシュ可能か
                var isAcceptedCache = CacheSettings?.IsUserAcceptedCache ?? false;
                var isEnabledCache = (CacheSettings?.IsEnableCache ?? false);

                CanDownload = isAcceptedCache && isEnabledCache;

                
                // 再生履歴に反映
                //VideoPlayHistoryDb.VideoPlayed(Video.RawVideoId);

               
                var smtc = SystemMediaTransportControls.GetForCurrentView();
                //            smtc.AutoRepeatModeChangeRequested += Smtc_AutoRepeatModeChangeRequested;
                MediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
                MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

                
//                ResetMediaPlayerCommand();

                smtc.DisplayUpdater.ClearAll();
                smtc.IsEnabled = true;
                smtc.IsPlayEnabled = true;
                smtc.IsPauseEnabled = true;
                smtc.DisplayUpdater.Type = MediaPlaybackType.Video;
                smtc.DisplayUpdater.VideoProperties.Title = _VideoInfo.Title;
                smtc.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(_VideoInfo.ThumbnailUrl));
                smtc.DisplayUpdater.Update();


                PlayerSettings.ObserveProperty(x => x.IsKeepDisplayInPlayback)
                    .Subscribe(isKeepDisplay =>
                    {
                        SetKeepDisplayWithCurrentState();
                    })
                    .AddTo(_NavigatingCompositeDisposable);

            }



            if (HohoemaPlaylist.CurrentPlaylist == null)
            {
                throw new Exception();
            }

            
            Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");

            App.Current.Resuming += Current_Resuming;
            App.Current.Suspending += Current_Suspending;



            UpdateCache();

            ToggleCacheRequestCommand.RaiseCanExecuteChanged();

            RaisePropertyChanged(nameof(VideoOwnerId));
        }

        private async Task CheckDeleted(Database.NicoVideo videoInfo)
        {
            try
            {
                // 動画が削除されていた場合
                if (videoInfo.IsDeleted)
                {
                    Debug.WriteLine($"cant playback{VideoId}. due to denied access to watch page, or connection offline.");

                    IsNotSupportVideoType = true;
                    CannotPlayReason = $"この動画は {_VideoInfo.PrivateReasonType.ToCulturelizeString()} のため視聴できません";
                    CurrentState.Value = MediaPlaybackState.None;

                    Scheduler.ScheduleAsync(async (scheduler, cancelToken) =>
                    {
                        await Task.Delay(100);

                        string toastContent = "";
                        if (!String.IsNullOrEmpty(videoInfo.Title))
                        {
                            toastContent = $"\"{videoInfo.Title}\" は削除された動画です";
                        }
                        else
                        {
                            toastContent = $"削除された動画です";
                        }
                        _NotificationService.ShowToast($"動画 {VideoId} は再生できません", toastContent);
                    });

                    // ローカルプレイリストの場合は勝手に消しておく
                    if (HohoemaPlaylist.CurrentPlaylist is LegacyLocalMylist)
                    {
                        if (HohoemaPlaylist.CurrentPlaylist != HohoemaPlaylist.DefaultPlaylist)
                        {
                            var item = HohoemaPlaylist.CurrentPlaylist.FirstOrDefault(x => x == VideoId);
                            if (item != null)
                            {
                                await (HohoemaPlaylist.CurrentPlaylist as Interfaces.ILocalMylist).RemoveMylistItem(item);
                            }
                        }
                    }

                    VideoPlayed(canPlayNext: true);

                    return;
                }
            }
            catch (Exception exception)
            {
                // 動画情報の取得に失敗
                System.Diagnostics.Debug.Write(exception.Message);
                return;
            }
        }


        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            Debug.WriteLine("VideoPlayer OnNavigatingFromAsync start.");

            CancelPlayNextVideo();

            //			PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

            _CurrentPlayingVideoSession?.Dispose();

            var mediaPlayer = MediaPlayer;
            MediaPlayer = null;
            RaisePropertyChanged(nameof(MediaPlayer));
            MediaPlayer = mediaPlayer;


            App.Current.Suspending -= Current_Suspending;
            MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
            MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;

            MediaPlayer.CommandManager.NextReceived -= CommandManager_NextReceived;
            MediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;

            Comments.Clear();

            var smtc = SystemMediaTransportControls.GetForCurrentView();
            smtc.DisplayUpdater.ClearAll();
            smtc.DisplayUpdater.Update();


            // プレイリストへ再生完了を通知
            VideoPlayed();

            ExitKeepDisplay();

            // サイドペインの片付け
            // 関連動画を選択した場合に表示エラーが起きないように回避
            if (CurrentSidePaneContentType.Value != PlayerSidePaneContentType.RelatedVideos)
            {
                _PrevSidePaneContentType = CurrentSidePaneContentType.Value;
            }

            if (_SidePaneContentCache.ContainsKey(PlayerSidePaneContentType.Comment))
            {
                try
                {
                    var commentSidePaneContent = _SidePaneContentCache[PlayerSidePaneContentType.Comment];
                    _SidePaneContentCache.Remove(PlayerSidePaneContentType.Comment);
                }
                catch { Debug.WriteLine("failed dispose PlayerSidePaneContentType.Comment"); }

                CurrentSidePaneContentType.Value = null;
            }

            if (_SidePaneContentCache.ContainsKey(PlayerSidePaneContentType.Setting))
            {
                (_SidePaneContentCache[PlayerSidePaneContentType.Setting] as SettingsSidePaneContentViewModel).VideoQualityChanged -= VideoPlayerPageViewModel_VideoQualityChanged;
            }

            App.Current.Resuming -= Current_Resuming;
            App.Current.Suspending -= Current_Suspending;

            var sidePaneContents = _SidePaneContentCache.Values.ToArray();
            _SidePaneContentCache.Clear();
            foreach (var sidePaneContent in sidePaneContents)
            {
                sidePaneContent.Dispose();
            }

            Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");

            base.OnNavigatedFrom(parameters);
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var defferal = e.SuspendingOperation.GetDeferral();
            try
            {
                //            PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

                _PreviosPlayingVideoPosition = TimeSpan.FromSeconds(SliderVideoPosition.Value);

                _IsNeddPlayInResumed = this.CurrentState.Value != MediaPlaybackState.Paused
                    && this.CurrentState.Value != MediaPlaybackState.None;

                IsFullScreen.Value = false;

                _CurrentPlayingVideoSession?.Dispose();
                _CurrentPlayingVideoSession = null;

                if (MediaPlayer.Source != null)
                {
                    MediaPlayer.Pause();
                    // サスペンド時にメモリ使用可能量が変更された場合に対応する
                    MediaPlayer.Source = null;
                }
            }
            finally
            {
                defferal.Complete();
            }
        }

        private void Current_Resuming(object sender, object e)
        {
        }

        TimeSpan _PrevPlaybackPosition;
        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (IsDisposed) { return; }

            if (sender.PlaybackState == MediaPlaybackState.Playing)
            {
                ReadVideoPosition.Value = sender.Position;
            }

            foreach (var script in _NicoScriptList)
            {
                if (_PrevPlaybackPosition <= script.BeginTime && sender.Position > script.BeginTime)
                {
                    if (script.EndTime < sender.Position)
                    {
                        Debug.WriteLine("nicoscript Enabling Skiped :" + script.Type);
                        continue;
                    }

                    Debug.WriteLine("nicoscript Enabling :" + script.Type);
                    script.ScriptEnabling?.Invoke();
                }
                else if (script.EndTime.HasValue)
                {
                    if (_PrevPlaybackPosition <= script.BeginTime)
                    {
                        Debug.WriteLine("nicoscript Disabling Skiped :" + script.Type);
                        continue;
                    }

                    if (_PrevPlaybackPosition < script.EndTime && sender.Position > script.EndTime)
                    {
                        Debug.WriteLine("nicoscript Disabling :" + script.Type);
                        script.ScriptDisabling?.Invoke();
                    }
                }
            }

            _PrevPlaybackPosition = sender.Position;
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            Debug.WriteLine(sender.PlaybackState);

            Scheduler.Schedule(() =>
            {
                if (IsDisposed) { return; }

                CurrentState.Value = sender.PlaybackState;
            });

            // 最後まで到達していた場合
            if (sender.PlaybackState == MediaPlaybackState.Paused
                && sender.Position >= (_VideoInfo.Length - TimeSpan.FromSeconds(1))
                )
            {
                VideoPlayed(canPlayNext: true);
            }
        }




        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                HohoemaPlaylist.PlayDone(CurrentPlayingItem, canPlayNext:false);

                if (HohoemaPlaylist.Player.CanGoBack)
                {
                    HohoemaPlaylist.Player.GoBack();
                }
            }
        }

        private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                HohoemaPlaylist.PlayDone(CurrentPlayingItem, canPlayNext: true);

                /*
                if (HohoemaPlaylist.Player.CanGoBack)
                {
                    HohoemaPlaylist.Player.GoBack();
                }
                */
            }
        }


        private void ResetMediaPlayerCommand()
        {
            if (MediaPlayer == null) { return; }

            var isEnableNextButton = this.PlaylistCanGoNext.Value;
            if (isEnableNextButton)
            {
                MediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            }
            else
            {
                MediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Never;
            }

            var isEnableBackButton = this.PlaylistCanGoBack.Value;
            if (isEnableBackButton)
            {
                MediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            }
            else
            {
                MediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Never;
            }
        }

        bool _IsVideoPlayed = false;
        DispatcherTimer _NextPlayVideoProgressTimer = new DispatcherTimer() { Interval = TimeSpan.FromMilliseconds(32) };

        DateTime PlayEndTime;
        private double _NextPlayVideoProgressTime = 0.0;
        public double NextPlayVideoProgressTime
        {
            get { return _NextPlayVideoProgressTime; }
            set { SetProperty(ref _NextPlayVideoProgressTime, value); }
        }

        private bool _IsCanceledPlayNextVideo = true;
        public bool IsCanceledPlayNextVideo
        {
            get { return _IsCanceledPlayNextVideo; }
            set { SetProperty(ref _IsCanceledPlayNextVideo, value); }
        }


        public bool IsEnableAutoPlayNextVideo => PlaylistSettings.AutoMoveNextVideoOnPlaylistEmpty;

        private void VideoPlayed(bool canPlayNext = false)
        {
            
            // Note: 次の再生用VMの作成を現在ウィンドウのUIDsipatcher上で行わないと同期コンテキストが拾えず再生に失敗する
            // VideoPlayedはMediaPlayerが動作しているコンテキスト上から呼ばれる可能性がある
            Scheduler.Schedule(async () => 
            {
                IsDisplayControlUI.Value = true;

                if (!_IsVideoPlayed == false && CurrentPlayingItem != null)
                {
                    HohoemaPlaylist.PlayDone(CurrentPlayingItem, canPlayNext);

                    Database.VideoPlayedHistoryDb.VideoPlayed(CurrentPlayingItem.ContentId);
                }

                if (!HohoemaPlaylist.CurrentPlaylist.Any())
                {
                    if (!IsPlayWithCache.Value)
                    {
                        if (canPlayNext)
                        {
                            EndPlayRecommendAction = GetSidePaneContent(PlayerSidePaneContentType.RelatedVideos) as RelatedVideosSidePaneContentViewModel;

                            IsCanceledPlayNextVideo = !IsEnableAutoPlayNextVideo; // 次動画へ自動で進まない場合はキャンセル操作が不要になる
                            CancelAutoPlayNextVideoCommand.RaiseCanExecuteChanged();

                            // 自動で次動画へ移動する機能
                            var sidePaneContent = GetSidePaneContent(PlayerSidePaneContentType.RelatedVideos) as RelatedVideosSidePaneContentViewModel;
                            await sidePaneContent.InitializeRelatedVideos();

                            if (sidePaneContent.NextVideo != null && PlaylistSettings.AutoMoveNextVideoOnPlaylistEmpty)
                            {
                                // 再生終了後アクションがプレイヤー表示に変更がない場合に自動次動画検出を開始する
                                if (PlaylistSettings.PlaylistEndAction == PlaylistEndAction.NothingDo 
                                || PlayerViewManager.IsPlayerShowWithSecondaryView)
                                {
                                    PlayEndTime = DateTime.Now;

                                    NextPlayVideoProgressTime = 0.0;
                                    _NextPlayVideoProgressTimer.Tick += _NextPlayVideoProgressTimer_Tick;
                                    _NextPlayVideoProgressTimer.Start();

                                    await Task.Delay(TimeSpan.FromSeconds(10));

                                    _NextPlayVideoProgressTimer.Stop();
                                    _NextPlayVideoProgressTimer.Tick -= _NextPlayVideoProgressTimer_Tick;

                                    if (!IsCanceledPlayNextVideo)
                                    {
                                        HohoemaPlaylist.PlayVideo(sidePaneContent.NextVideo.RawVideoId, sidePaneContent.NextVideo.Label);
                                    }
                                }
                            }
                                    
                        }
                            
                    }
                }
            });

            _IsVideoPlayed = true;
        }

        private void _NextPlayVideoProgressTimer_Tick(object sender, object e)
        {
            NextPlayVideoProgressTime = (DateTime.Now - PlayEndTime).TotalMilliseconds;
        }

        private void CancelPlayNextVideo()
        {
            IsCanceledPlayNextVideo = true;
            _NextPlayVideoProgressTimer.Stop();

        }

        private DelegateCommand _CancelAutoPlayNextVideoCommand;
        public DelegateCommand CancelAutoPlayNextVideoCommand
        {
            get
            {
                return _CancelAutoPlayNextVideoCommand
                    ?? (_CancelAutoPlayNextVideoCommand = new DelegateCommand(() =>
                    {
                        CancelPlayNextVideo();
                    }
                    , () => !IsCanceledPlayNextVideo));
            }
        }

        private DelegateCommand _ManualPlayNextVideoCommand;
        public DelegateCommand ManualPlayNextVideoCommand
        {
            get
            {
                return _ManualPlayNextVideoCommand
                    ?? (_ManualPlayNextVideoCommand = new DelegateCommand(() =>
                    {
                        CancelPlayNextVideo();

                        var sidePaneContent = GetSidePaneContent(PlayerSidePaneContentType.RelatedVideos) as RelatedVideosSidePaneContentViewModel;
                        sidePaneContent.InitializeRelatedVideos()
                            .ContinueWith(prevTask =>
                            {
                                if (sidePaneContent.NextVideo != null)
                                {
                                    HohoemaPlaylist.PlayVideo(sidePaneContent.NextVideo.RawVideoId, sidePaneContent.NextVideo.Label);
                                }
                            });
                    }
                    ));
            }
        }


        bool IsDisposed = false;
        public override void Destroy()
        {
            IsDisposed = true;

            _CurrentPlayingVideoSession?.Dispose();

            var sidePaneContents = _SidePaneContentCache.Values.ToArray();
            _SidePaneContentCache.Clear();
            foreach (var sidePaneContent in sidePaneContents)
            {
                if (sidePaneContent is SettingsSidePaneContentViewModel)
                {
                    (sidePaneContent as SettingsSidePaneContentViewModel).VideoQualityChanged -= VideoPlayerPageViewModel_VideoQualityChanged;
                }

                sidePaneContent.Dispose();
            }


            base.Destroy();
        }

        private async Task UpdateComments()
        {
            Comments.Clear();


            var comments = await CommentSession.GetInitialComments();
            
            // ニコスクリプトの状態を初期化
            ClearNicoScriptState();

            // 投コメからニコスクリプトをセットアップしていく
            var ownerComments = comments.Where(x => x.UserId == null);
            foreach (var ownerComment in ownerComments)
            {
                if (ownerComment.DeletedFlag > 0) { continue; }
                TryAddNicoScript(ownerComment);
            }

            _NicoScriptList.Sort((x, y) => (int)(x.BeginTime.Ticks - y.BeginTime.Ticks));

            // 投コメのニコスクリプトをスキップして
            // コメントをコメントリストに追加する（通常の投コメも含めて）
            foreach (var comment in comments)
            {
                if (comment.DeletedFlag > 0) { continue; }
                if (IsNicoScriptComment(comment.UserId, comment.CommentText)) { continue; }
                if (comment != null)
                {
                    Comments.Add(comment);
                }
            }


            System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");
        }



        List<NicoScript> _NicoScriptList = new List<NicoScript>();
        List<ReplaceNicoScript> _ReplaceNicoScirptList = new List<ReplaceNicoScript>();
        List<DefaultCommandNicoScript> _DefaultCommandNicoScriptList = new List<DefaultCommandNicoScript>();

        private static bool IsNicoScriptComment(string userId, string content)
        {
            return userId == null && (content.StartsWith("＠") || content.StartsWith("@") || content.StartsWith("/"));
        }



        private bool TryAddNicoScript(Comment chat)
        {
            const bool IS_ENABLE_Default   = true; // Default comment Command
            const bool IS_ENABLE_Replace         = false; // Replace comment text
            const bool IS_ENABLE_Jump     = true; // seek video position or redirect to another content
            const bool IS_ENABLE_DisallowSeek   = true; // disable seek
            const bool IS_ENABLE_DisallowComment = true; // disable comment

            if (!IsNicoScriptComment(chat.UserId, chat.CommentText)) { return false; }

            var nicoScriptContents = chat.CommentText.Remove(0, 1).Split(' ', '　');

            if (nicoScriptContents.Length == 0) { return false; }

            var nicoScriptType = nicoScriptContents[0];
            var beginTime = TimeSpan.FromMilliseconds(chat.VideoPosition * 10);
            switch (nicoScriptType)
            {
                case "デフォルト":
                    if (IS_ENABLE_Default)
                    {
                        TimeSpan? endTime = null;
                        var commands = chat.Mail.Split(' ');
                        var timeCommand = commands.FirstOrDefault(x => x.StartsWith("@"));
                        if (timeCommand != null)
                        {
                            endTime = beginTime + TimeSpan.FromSeconds(int.Parse(timeCommand.Remove(0, 1)));
                        }

                        _DefaultCommandNicoScriptList.Add(new DefaultCommandNicoScript(nicoScriptType)
                        {
                            BeginTime = beginTime,
                            EndTime = endTime,
                            Command = commands.Where(x => !x.StartsWith("@")).ToArray()
                        });
                    }


                    break;
                case "置換":
                    if (IS_ENABLE_Replace)
                    {
                        var commands = chat.Mail.Split(' ');
                        List<string> commandItems = new List<string>();
                        TimeSpan duration = TimeSpan.FromSeconds(30);
                        foreach (var command in commands)
                        {
                            if (command.StartsWith("@"))
                            {
                                duration = TimeSpan.FromSeconds(int.Parse(command.Remove(0, 1)));
                            }
                            else
                            {
                                commandItems.Add(command);
                            }
                        }
                        /*
                         * ※1 オプション自体にスペースを含めたい場合は、コマンドをダブルクォート(")、シングルクォート(')、または全角かぎかっこ（「」）で囲んでください。
                            その際、ダブルクォート(")とシングルクォート(')内ではバックスラッシュ（\）がエスケープ文字として扱われますが、全角かぎかっこ（「」）内では文字列として扱われます。
  
                         */

                        _ReplaceNicoScirptList.Add(new ReplaceNicoScript(nicoScriptType)
                        {
                            Commands = string.Join(" ", commandItems),
                            BeginTime = beginTime,
                            EndTime = beginTime + duration,
                           
                        });

                        Debug.WriteLine($"置換を設定");
                    }
                    break;
                case "ジャンプ":
                    if (IS_ENABLE_Jump)
                    {  
                        var condition = nicoScriptContents[1];
                        if (condition.StartsWith("#"))
                        {
                            TimeSpan? endTime = null;
                            if (chat.Mail?.StartsWith("@") ?? false)
                            {
                                endTime = beginTime + TimeSpan.FromSeconds(int.Parse(chat.Mail.Remove(0, 1)));
                            }
                            _NicoScriptList.Add(new NicoScript(nicoScriptType)
                            {
                                BeginTime = beginTime,
                                EndTime = endTime,
                                ScriptEnabling = () => 
                                {
                                    if (!PlayerSettings.NicoScript_Jump_Enabled)
                                    {
                                        return;
                                    }

                                    // #00:00
                                    // #00:00.00
                                    // #00
                                    // #00.00
                                    TimeSpan time = TimeSpan.Zero;
                                    var timeTexts = condition.Remove(0, 1).Split(':');
                                    if (timeTexts.Length == 2)
                                    {
                                        time += TimeSpan.FromMinutes(int.Parse(timeTexts[0]));
                                    }

                                    var secAndMillSec = timeTexts.Last().Split('.');

                                    var sec = secAndMillSec.First();
                                    time += TimeSpan.FromSeconds(int.Parse(sec));
                                    if (secAndMillSec.Length == 2)
                                    {
                                        time += TimeSpan.FromMilliseconds(int.Parse(secAndMillSec.Last()));
                                    }

                                    // シーク位置を直接設定する
                                    MediaPlayer.PlaybackSession.Position = time;
                                }
                            });

                            Debug.WriteLine($"{beginTime.ToString()} に {condition} へのジャンプを設定");
                        }
                        else if (NiconicoRegex.IsVideoId(condition))
                        {
                            var message = nicoScriptContents.ElementAtOrDefault(2);
                            
                            // ニコスクリプトによる別動画へのジャンプを実装する
                            _JumpVideoId = condition;
                            Debug.WriteLine($"{beginTime.ToString()} に {condition} へのジャンプを設定");
                        }
                    }
                    break;
                case "シーク禁止":
                    if (IS_ENABLE_DisallowSeek)
                    {
                        TimeSpan? endTime = null;
                        if (chat.Mail?.StartsWith("@") ?? false)
                        {
                            endTime = beginTime + TimeSpan.FromSeconds(int.Parse(chat.Mail.Remove(0, 1)));
                        }
                        _NicoScriptList.Add(new NicoScript(nicoScriptType)
                        {
                            BeginTime = beginTime,
                            EndTime = endTime,
                            ScriptEnabling = () => IsSeekDisabledFromNicoScript.Value = true,
                            ScriptDisabling = () => IsSeekDisabledFromNicoScript.Value = false
                        });

                        if (endTime.HasValue)
                        {
                            Debug.WriteLine($"{beginTime.ToString()} ～ {endTime.ToString()} までシーク禁止を設定");
                        }
                        else
                        {
                            Debug.WriteLine($"{beginTime.ToString()} から動画終了までシーク禁止を設定");
                        }
                    }
                    break;
                case "コメント禁止":
                    if (IS_ENABLE_DisallowComment)
                    {
                        TimeSpan? endTime = null;
                        if (chat.Mail?.StartsWith("@") ?? false)
                        {
                            endTime = beginTime + TimeSpan.FromSeconds(int.Parse(chat.Mail.Remove(0, 1)));
                        }
                        _NicoScriptList.Add(new NicoScript(nicoScriptType)
                        {
                            BeginTime = beginTime,
                            EndTime = endTime,
                            ScriptEnabling = () => IsCommentDisabledFromNicoScript.Value = true,
                            ScriptDisabling = () => IsCommentDisabledFromNicoScript.Value = false
                        });

                        if (endTime.HasValue)
                        {
                            Debug.WriteLine($"{beginTime.ToString()} ～ {endTime.ToString()} までコメント禁止を設定");
                        }
                        else
                        {
                            Debug.WriteLine($"{beginTime.ToString()} から動画終了までコメント禁止を設定");
                        }
                    }
                    break;
                default:
                    Debug.WriteLine($"Not support nico script type : {nicoScriptType}");
                    break;
            }

            return true;
        }

        // コメントの再ロード時などにニコスクリプトを再評価する
        private void ClearNicoScriptState()
        {
            // デフォルトのコメントコマンドをクリア
            _DefaultCommandNicoScriptList.Clear();

            // 置換設定をクリア
            _ReplaceNicoScirptList.Clear();

            // ジャンプスクリプト
            // シーク禁止とコメント禁止
            _NicoScriptList.Clear();
        }

        private async Task SubmitComment()
		{
            if (CommentSession == null) { return; }

            Debug.WriteLine($"try comment submit:{WritingComment.Value}");
            
			NowSubmittingComment.Value = true;
			try
			{
				var vpos = (uint)(ReadVideoPosition.Value.TotalMilliseconds / 10);
				var commands = CommandString.Value;
				var res = await CommentSession.PostComment(WritingComment.Value, ReadVideoPosition.Value, CommandEditerVM.MakeCommands());

				if (res.Status == ChatResult.Success)
				{
					Debug.WriteLine("コメントの投稿に成功: " + res.CommentNo);

					var commentVM = new Comment()
					{
						CommentId = (uint)res.CommentNo,
						VideoPosition = vpos,
						UserId = NiconicoSession.UserIdString,
						CommentText = WritingComment.Value,
					};

					if (CommandEditerVM.IsPickedColor.Value)
					{
						var color = CommandEditerVM.FreePickedColor.Value;
						commentVM.Color = color;
					}

					Comments.Add(commentVM);

					WritingComment.Value = "";
				}
				else
				{
                    _NotificationService.ShowToast("コメント投稿", $"{VideoId} へのコメント投稿に失敗 （error code : {res.StatusCode}" , duration: Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Short);

                    Debug.WriteLine("コメントの投稿に失敗: " + res.Status.ToString());
				}

			}
            catch (NotSupportedException ex)
            {
                Debug.WriteLine(ex.ToString());
            }
			finally
			{
				NowSubmittingComment.Value = false;
			}
		}


		private void UpdateCommandString()
		{
			var str = CommandEditerVM.MakeCommandsString();
			if (String.IsNullOrEmpty(str))
			{
				CommandString.Value = "";
			}
			else
			{
				CommandString.Value = str;
			}
		}




        private void UpdateCache()
        {
            /*
            IsCacheLegacyOriginalQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Smile_Original).IsCacheRequested;
            IsCacheLegacyLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Smile_Low).IsCacheRequested;

            CanToggleCacheRequestLegacyOriginalQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Smile_Original).CanRequestCache || IsCacheLegacyOriginalQuality.Value;
            CanToggleCacheRequestLegacyLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Smile_Low).CanRequestCache || IsCacheLegacyLowQuality.Value;
            */
        }

		#region Command	

		private DelegateCommand<object> _CurrentStateChangedCommand;
		public DelegateCommand<object> CurrentStateChangedCommand
		{
			get
			{
				return _CurrentStateChangedCommand
					?? (_CurrentStateChangedCommand = new DelegateCommand<object>((arg) =>
					{
						var e = (RoutedEventArgs)arg;
						
					}
					));
			}
		}

        
        private DelegateCommand _TogglePlayPauseCommand;
        public DelegateCommand TogglePlayPauseCommand
        {
            get
            {
                return _TogglePlayPauseCommand
                    ?? (_TogglePlayPauseCommand = new DelegateCommand(async () =>
                    {
                        CancelPlayNextVideo();
                        EndPlayRecommendAction = null;

                        var session = MediaPlayer.PlaybackSession;
                        if (session.PlaybackState == MediaPlaybackState.None)
                        {
                            PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

                            await this.PlayingQualityChangeAction();
                        }
                        if (session.PlaybackState == MediaPlaybackState.Playing)
                        {
                            MediaPlayer.Pause();
                        }
                        else if (session.PlaybackState == MediaPlaybackState.Paused)
                        {
                            MediaPlayer.Play();
                        }
                    }));
            }
        }

        
        public ReactiveCommand<TimeSpan?> SeekVideoCommand { get; private set; }
        

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
						var amount = PlayerSettings.SoundVolumeChangeFrequency;
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
						var amount = PlayerSettings.SoundVolumeChangeFrequency;
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



        private DelegateCommand _ToggleRepeatCommand;
		public DelegateCommand ToggleRepeatCommand
		{
			get
			{
				return _ToggleRepeatCommand
					?? (_ToggleRepeatCommand = new DelegateCommand(() =>
					{
						IsEnableRepeat.Value = !IsEnableRepeat.Value;
					}));
			}
		}

		public ReactiveCommand CommentSubmitCommand { get; private set; }


        private DelegateCommand<object> _ChangePlayQualityCommand;
        public DelegateCommand<object> ChangePlayQualityCommand
        {
            get
            {
                return _ChangePlayQualityCommand
                    ?? (_ChangePlayQualityCommand = new DelegateCommand<object>(async (parameter) =>
                    {
                        if (parameter is VideoContent content && _CurrentPlayingVideoSession is DmcVideoStreamingSession dmcSession)
                        {
                            var videos = dmcSession.DmcWatchResponse.Video.DmcInfo.Quality.Videos;

                            var index = videos.Reverse().ToList().IndexOf(content);
                            var dmcQuality = (NicoVideoQuality)(6 - index); // 2~6の範囲に収める必要あり
                            if (dmcQuality.IsDmc())
                            {
                                RequestVideoQuality.Value = dmcQuality;

                                _PreviosPlayingVideoPosition = ReadVideoPosition.Value;

                                await PlayingQualityChangeAction(RequestVideoQuality.Value);
                            }
                        }
                    }
                    ));
            }
        }

        private VideoContent _CurrentDmcQualityVideoContent;
        public VideoContent CurrentDmcQualityVideoContent
        {
            get { return _CurrentDmcQualityVideoContent; }
            set { SetProperty(ref _CurrentDmcQualityVideoContent, value); }
        }


        public ObservableCollection<VideoContent> VideoQualities { get; } = new ObservableCollection<VideoContent>();


        private DelegateCommand _OpenVideoPageWithBrowser;
		public DelegateCommand OpenVideoPageWithBrowser
		{
			get
			{
				return _OpenVideoPageWithBrowser
					?? (_OpenVideoPageWithBrowser = new DelegateCommand(async () =>
					{
						var watchPageUri = Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl + VideoId;
						await Windows.System.Launcher.LaunchUriAsync(new Uri(watchPageUri));
					}
					));
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
                        PlayerViewManager.IsPlayerSmallWindowModeEnabled = true;
                    }
                    ));
            }
        }

        private DelegateCommand _PlayerDisplayWithMainViewCommand;
        public DelegateCommand PlayerDisplayWithMainViewCommand
        {
            get
            {
                return _PlayerDisplayWithMainViewCommand
                    ?? (_PlayerDisplayWithMainViewCommand = new DelegateCommand(() =>
                    {
                        _ = PlayerViewManager.ChangePlayerViewModeAsync(PlayerViewMode.PrimaryView);
                    }
                    ));
            }
        }

        private DelegateCommand _PlayerDisplayWithSecondaryViewCommand;
        public DelegateCommand PlayerDisplayWithSecondaryViewCommand
        {
            get
            {
                return _PlayerDisplayWithSecondaryViewCommand
                    ?? (_PlayerDisplayWithSecondaryViewCommand = new DelegateCommand(() =>
                    {
                        _ = PlayerViewManager.ChangePlayerViewModeAsync(PlayerViewMode.SecondaryView);
                    }
                    ));
            }
        }



        private DelegateCommand _OpenVideoInfoCommand;
        public DelegateCommand OpenVideoInfoCommand
        {
            get
            {
                return _OpenVideoInfoCommand
                    ?? (_OpenVideoInfoCommand = new DelegateCommand(() =>
                    {
                        CancelPlayNextVideo();

                        PageManager.OpenPageWithId(HohoemaPageType.VideoInfomation, VideoId);
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
                        CancelPlayNextVideo();

                        ShareHelper.Share(_VideoInfo);
                    }
                    , () => DataTransferManager.IsSupported()
                    ));
            }
        }

        private DelegateCommand _VideoInfoCopyToClipboardCommand;
        public DelegateCommand VideoInfoCopyToClipboardCommand
        {
            get
            {
                return _VideoInfoCopyToClipboardCommand
                    ?? (_VideoInfoCopyToClipboardCommand = new DelegateCommand(() =>
                    {
                        Services.Helpers.ClipboardHelper.CopyToClipboard(_VideoInfo);
                    }
                    ));
            }
        }


        private DelegateCommand _AddMylistCommand;
        public DelegateCommand AddMylistCommand
        {
            get
            {
                return _AddMylistCommand
                    ?? (_AddMylistCommand = new DelegateCommand(async () =>
                    {
                        var targetMylist = await _HohoemaDialogService.ChoiceMylist();
                        if (targetMylist != null)
                        {
                            var result = await targetMylist.AddMylistItem(_VideoInfo.RawVideoId);
                            _NotificationService.ShowInAppNotification(
                                InAppNotificationPayload.CreateRegistrationResultNotification(
                                    result ? ContentManageResult.Success : ContentManageResult.Failed,
                                    "マイリスト",
                                    targetMylist.Label,
                                    _VideoInfo.Title
                                    ));
                        }
                    }
                    ));
            }
        }


        private DelegateCommand<string> _ToggleCacheRequestCommand;
        public DelegateCommand<string> ToggleCacheRequestCommand
        {
            get
            {
                return _ToggleCacheRequestCommand
                    ?? (_ToggleCacheRequestCommand = new DelegateCommand<string>(async (qualityName) =>
                    {
                        if (Enum.TryParse<NicoVideoQuality>(qualityName, out var quality))
                        {
                            var cacheInfo = VideoCacheManager.GetCacheInfo(VideoId, quality);
                            if (cacheInfo != null)
                            {

                                var qualityText = quality.ToCulturelizeString();

                                var dialog = new MessageDialog(
                                    $"{VideoTitle} の キャッシュデータ（{qualityText}画質）を削除します。この操作は元に戻せません。",
                                    "キャッシュ削除の確認"
                                    );
                                dialog.Commands.Add(new UICommand("キャッシュを削除") { Id = "delete" });
                                dialog.Commands.Add(new UICommand("キャンセル"));

                                dialog.CancelCommandIndex = 1;
                                dialog.DefaultCommandIndex = 1;
                                var result = await dialog.ShowAsync();

                                if ((result.Id as string) == "delete")
                                {
                                    if (CurrentVideoQuality.Value == quality)
                                    {
                                        MediaPlayer.Pause();
                                        VideoPlayed(true);
                                        await Task.Delay(TimeSpan.FromSeconds(2));
                                    }

                                    Scheduler.ScheduleAsync(async (s, cancelToken) =>
                                    {
                                        await VideoCacheManager.CancelCacheRequest(VideoId, quality);
                                    });
                                }

                                UpdateCache();
                            }
                            else
                            {
                                Scheduler.ScheduleAsync(async (s, cancelToken) =>
                                {
                                    await VideoCacheManager.RequestCache(VideoId, quality);
                                });
                            }

                        }
                    }, (qualityName) =>
                    {
                        if (Enum.TryParse<NicoVideoQuality>(qualityName, out var quality))
                        {
                            var cacheInfo = VideoCacheManager.GetCacheInfo(VideoId, quality);

                            if (cacheInfo == null)
                            {
                                // TODO: キャッシュDLが利用可能な画質かを確認する
                                return NiconicoSession.IsLoggedIn;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    ));
            }
        }


        // Playlist

        private DelegateCommand _OpenPreviousPlaylistItemCommand;
        public DelegateCommand OpenPreviousPlaylistItemCommand
        {
            get
            {
                return _OpenPreviousPlaylistItemCommand
                    ?? (_OpenPreviousPlaylistItemCommand = new DelegateCommand(() =>
                    {
                        var player = HohoemaPlaylist.Player;
                        if (player != null)
                        {
                            // 先に_IsVideoPlayed をtrueにしておくことで
                            // NavigatingFromで再生完了が呼ばれた時にPlaylist.PlayDoneが多重呼び出しされないようにする
                            _IsVideoPlayed = true;

                            HohoemaPlaylist.PlayDone(CurrentPlayingItem);

                            if (player.CanGoBack)
                            {
                                player.GoBack();
                            }
                        }
                    }
//                    , () => HohoemaPlaylist.Player?.CanGoBack ?? false
                    ));
            }
        }

        private DelegateCommand _OpenNextPlaylistItemCommand;
        public DelegateCommand OpenNextPlaylistItemCommand
        {
            get
            {
                return _OpenNextPlaylistItemCommand
                    ?? (_OpenNextPlaylistItemCommand = new DelegateCommand(() =>
                    {
                        var player = HohoemaPlaylist.Player;
                        if (player != null)
                        {
                            // 先に_IsVideoPlayed をtrueにしておくことで
                            // NavigatingFromで再生完了が呼ばれた時にPlaylist.PlayDoneが多重呼び出しされないようにする
                            _IsVideoPlayed = true;

                            HohoemaPlaylist.PlayDone(CurrentPlayingItem, canPlayNext:true);
                        }
                    }
//                    , () => HohoemaPlaylist.Player?.CanGoNext ?? false
                    ));
            }
        }

        private DelegateCommand<PlaylistItem> _OpenPlaylistItemCommand;
        public DelegateCommand<PlaylistItem> OpenPlaylistItemCommand
        {
            get
            {
                return _OpenPlaylistItemCommand
                    ?? (_OpenPlaylistItemCommand = new DelegateCommand<PlaylistItem>((item) =>
                    {
                        if (item != CurrentPlayingItem)
                        {
                            HohoemaPlaylist.Play(item);
                        }
                    }
                    ));
            }
        }

        private DelegateCommand _OpenCurrentPlaylistPageCommand;
        public DelegateCommand OpenCurrentPlaylistPageCommand
        {
            get
            {
                return _OpenCurrentPlaylistPageCommand
                    ?? (_OpenCurrentPlaylistPageCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.Mylist, $"id={HohoemaPlaylist.CurrentPlaylist.Id}&origin={HohoemaPlaylist.CurrentPlaylist.ToMylistOrigin().ToString()}");
                    }
                    ));
            }
        }


        private DelegateCommand _ToggleRepeatModeCommand;
        public DelegateCommand ToggleRepeatModeCommand
        {
            get
            {
                return _ToggleRepeatModeCommand
                    ?? (_ToggleRepeatModeCommand = new DelegateCommand(() =>
                    {
                        switch (PlaylistSettings.RepeatMode)
                        {
                            case MediaPlaybackAutoRepeatMode.None:
                                PlaylistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.Track;
                                break;
                            case MediaPlaybackAutoRepeatMode.Track:
                                PlaylistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.List;
                                break;
                            case MediaPlaybackAutoRepeatMode.List:
                                PlaylistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.None;
                                break;
                            default:
                                break;
                        }
                    }
                    ));
            }
        }

        private DelegateCommand _ToggleShuffleCommand;
        public DelegateCommand ToggleShuffleCommand
        {
            get
            {
                return _ToggleShuffleCommand
                    ?? (_ToggleShuffleCommand = new DelegateCommand(() =>
                    {
                        PlaylistSettings.IsShuffleEnable = !PlaylistSettings.IsShuffleEnable;
                    }
                    ));
            }
        }


        #endregion




        #region player settings method


        void SetKeepDisplayIfEnable()
		{
			ExitKeepDisplay();

//			if (PlayerSettings.IsKeepDisplayInPlayback)
			{
				DisplayRequestHelper.RequestKeepDisplay();
			}
		}

		void ExitKeepDisplay()
		{
			DisplayRequestHelper.StopKeepDisplay();
		}

        public IScheduler Scheduler { get; }







        #endregion

        public IEventAggregator EventAggregator { get; }

        public NicoVideoStreamingSessionProvider NicoVideo { get; }

		private string _VideoId;
		public string VideoId
		{
			get { return _VideoId; }
			set { SetProperty(ref _VideoId, value); }
		}

        private NicoVideoQuality? _Quality;
        public NicoVideoQuality? Quality
        {
            get { return _Quality; }
            set { SetProperty(ref _Quality, value); }
        }

        private PlaylistItem _CurrentPlayingItem;
        public PlaylistItem CurrentPlayingItem => _CurrentPlayingItem ?? (_CurrentPlayingItem = PlaylistItems.FirstOrDefault(x => x.ContentId == this.VideoId));

        private string _VideoTitle;
        public string VideoTitle
        {
            get { return _VideoTitle; }
            set { SetProperty(ref _VideoTitle, value); }
        }


        public string VideoOwnerId => _VideoInfo?.Owner?.OwnerId;

        private bool _CanDownload;
		public bool CanDownload
		{
			get { return _CanDownload; }
			set { SetProperty(ref _CanDownload, value); }
		}

        public ReactiveProperty<string> ThumbnailUri { get; private set; }




        // Note: 新しいReactivePropertyを追加したときの注意点
        // ReactivePorpertyの初期化にPlayerWindowUIDispatcherSchedulerを使うこと


        public MediaPlayer MediaPlayer { get; private set; }


        public ReactiveProperty<NicoVideoQuality?> CurrentVideoQuality { get; private set; }
        public ReactiveProperty<NicoVideoQuality> RequestVideoQuality { get; private set; }

        public ReactiveProperty<bool> IsCacheLegacyOriginalQuality { get; private set; }
        public ReactiveProperty<bool> IsCacheLegacyLowQuality { get; private set; }

        public ReactiveProperty<bool> CanToggleCacheRequestLegacyOriginalQuality { get; private set; }
        public ReactiveProperty<bool> CanToggleCacheRequestLegacyLowQuality { get; private set; }

        public ReactiveProperty<bool> IsPlayWithCache { get; private set; }
		public ReactiveProperty<bool> DownloadCompleted { get; private set; }
		public ReactiveProperty<double> ProgressPercent { get; private set; }


		public ReactiveProperty<TimeSpan> CurrentVideoPosition { get; private set; }
		public ReactiveProperty<TimeSpan> ReadVideoPosition { get; private set; }
		public ReactiveProperty<TimeSpan> CommentVideoPosition { get; private set; }

        public ReadOnlyReactiveProperty<bool> NowCanSeek { get; private set; }
        public ReactiveProperty<bool> IsSeekDisabledFromNicoScript { get; private set; }

		public ReactiveProperty<double> SliderVideoPosition { get; private set; }
		public ReactiveProperty<double> VideoLength { get; private set; }
		public ReactiveProperty<MediaPlaybackState> CurrentState { get; private set; }
        public IReadOnlyReactiveProperty<bool> NowBuffering { get; private set; }
		public ReactiveProperty<bool> NowPlaying { get; private set; }
		public ReactiveProperty<bool> NowQualityChanging { get; private set; }
		public ReactiveProperty<bool> IsEnableRepeat { get; private set; }
        public ReactiveProperty<double> PlaybackRate { get; private set; }
        public DelegateCommand<double?> SetPlaybackRateCommand { get; private set; }

        public static List<double> PlaybackRateList { get; } = new List<double>
        {
            2.0,
            1.75,
            1.5,
            1.25,
            1.0,
            0.75,
            0.5,
            0.25,
            0.05
        };

        public ReactiveProperty<bool> IsAutoHideEnable { get; private set; }
        public ReactiveProperty<TimeSpan> AutoHideDelayTime { get; private set; }

        public ReactiveProperty<bool> IsMouseCursolAutoHideEnable { get; private set; }

        public ReactiveProperty<bool> IsDisplayControlUI { get; private set; }

        private TimeSpan _PreviosPlayingVideoPosition;

        private bool _IsNeddPlayInResumed;


        private bool _NowPlayingWithDmcVideo;
        public bool NowPlayingWithDmcVideo
        {
            get { return _NowPlayingWithDmcVideo; }
            set { SetProperty(ref _NowPlayingWithDmcVideo, value); }
        }

        private double _VideoBitrate;
        public double VideoBitrate
        {
            get { return _VideoBitrate; }
            set { SetProperty(ref _VideoBitrate, value); }
        }

        private double _VideoWidth;
        public double VideoWidth
        {
            get { return _VideoWidth; }
            set { SetProperty(ref _VideoWidth, value); }
        }

        private double _VideoHeight;
        public double VideoHeight
        {
            get { return _VideoHeight; }
            set { SetProperty(ref _VideoHeight, value); }
        }

        // Sound
        public ReactiveProperty<bool> NowSoundChanging { get; private set; }
		public ReactiveProperty<bool> IsMuted { get; private set; }
		public ReactiveProperty<double> SoundVolume { get; private set; }

		// Settings
		public ReactiveProperty<TimeSpan> RequestUpdateInterval { get; private set; }
		public ReactiveProperty<TimeSpan> RequestCommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<bool> IsFullScreen { get; private set; }
        public ReactiveProperty<bool> IsCompactOverlay { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsSmallWindowModeEnable { get; private set; }
        public ReactiveProperty<bool> IsForceLandscape { get; private set; }

        public ReactiveProperty<bool> NicoScript_Default_Enabled { get; private set; }
        public ReactiveProperty<bool> NicoScript_DisallowSeek_Enabled { get; private set; }
        public ReactiveProperty<bool> NicoScript_Jump_Enabled { get; private set; }
        public ReactiveProperty<bool> NicoScript_Replace_Enabled { get; private set; }



        public ReadOnlyReactiveProperty<bool> NowCanSubmitComment { get; private set; }
        public ReactiveProperty<bool> CanSubmitComment { get; private set; }
        public ReactiveProperty<bool> NowSubmittingComment { get; private set; }
		public ReactiveProperty<string> WritingComment { get; private set; }
        public ReactiveProperty<bool> IsCommentDisplayEnable { get; private set; }
        public ReactiveProperty<bool> NowCommentWriting { get; private set; }
		public ObservableCollection<Comment> Comments { get; private set; }
		public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }
		public ReactiveProperty<bool> IsNeedResumeExitWrittingComment { get; private set; }
		public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
		public ReactiveProperty<double> CommentCanvasWidth { get; private set; }
		public ReactiveProperty<Color> CommentDefaultColor { get; private set; }
        public ReadOnlyReactiveProperty<double> CommentOpacity { get; private set; }
        public ReactiveProperty<bool> IsCommentDisabledFromNicoScript { get; private set; }



        public CommentCommandEditerViewModel CommandEditerVM { get; private set; }
		public ReactiveProperty<string> CommandString { get; private set; }

        // 再生できない場合の補助

        private bool _IsCannotPlay;
		public bool IsNotSupportVideoType
		{
			get { return _IsCannotPlay; }
			set { SetProperty(ref _IsCannotPlay, value); }
		}

		private string _CannotPlayReason;
		public string CannotPlayReason
		{
			get { return _CannotPlayReason; }
			set { SetProperty(ref _CannotPlayReason, value); }
		}

        // 再生終了後のリコメンドアクション
        private RelatedVideosSidePaneContentViewModel _EndPlayRecommendAction;
        public RelatedVideosSidePaneContentViewModel EndPlayRecommendAction
        {
            get { return _EndPlayRecommendAction; }
            set { SetProperty(ref _EndPlayRecommendAction, value); }
        }

        // プレイリスト
        public Interfaces.IMylist CurrentPlaylist { get; private set; }
        public ReactiveProperty<string> CurrentPlaylistName { get; private set; }
        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistItem> PlaylistItems { get; private set; }

        private Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase> _SidePaneContentCache = new Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase>();

        public ReactiveProperty<PlayerSidePaneContentType?> CurrentSidePaneContentType { get; }
        public ReadOnlyReactiveProperty<SidePaneContentViewModelBase> CurrentSidePaneContent { get; }

        string _JumpVideoId;

        static PlayerSidePaneContentType? _PrevSidePaneContentType;
        SidePaneContentViewModelBase _PrevSidePaneContent;

        private DelegateCommand<object> _SelectSidePaneContentCommand;
        public DelegateCommand<object> SelectSidePaneContentCommand
        {
            get
            {
                return _SelectSidePaneContentCommand
                    ?? (_SelectSidePaneContentCommand = new DelegateCommand<object>((type) => 
                    {
                        // 再生終了後アクションとして関連動画を選択した場合には
                        // 次動画自動再生をキャンセルする
                        CancelPlayNextVideo();

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
            if (maybeType.HasValue && _SidePaneContentCache.ContainsKey(maybeType.Value))
            {
                return _SidePaneContentCache[maybeType.Value];
            }
            else if (!maybeType.HasValue)
            {
                return EmptySidePaneContent;
            }
            else
            {
                SidePaneContentViewModelBase sidePaneContent = null;
                switch (maybeType.Value)
                {
                    case PlayerSidePaneContentType.Playlist:
                        sidePaneContent = new PlayerSidePaneContent.PlaylistSidePaneContentViewModel(MediaPlayer, HohoemaPlaylist, PlaylistSettings, PageManager);
                        break;
                    case PlayerSidePaneContentType.Comment:
                        throw new NotImplementedException();
                    //                        sidePaneContent = new PlayerSidePaneContent.CommentSidePaneContentViewModel(HohoemaApp.UserSettings, LiveComments);
                    //                        break;
                    case PlayerSidePaneContentType.Setting:
                        sidePaneContent = new PlayerSidePaneContent.SettingsSidePaneContentViewModel(NgSettings, PlayerSettings, PlaylistSettings);
                        (sidePaneContent as SettingsSidePaneContentViewModel).VideoQualityChanged += VideoPlayerPageViewModel_VideoQualityChanged;
                        break;
                    case PlayerSidePaneContentType.RelatedVideos:
                        if (NicoVideo != null)
                        {
                            sidePaneContent = new PlayerSidePaneContent.RelatedVideosSidePaneContentViewModel(VideoId, NicoVideo, _JumpVideoId, NicoVideoProvider, ChannelProvider, MylistProvider,HohoemaPlaylist, PageManager);
                        }
                        else
                        {
                            return EmptySidePaneContent;
                        }
                        break;
                    default:
                        sidePaneContent = EmptySidePaneContent;
                        break;
                }

                _SidePaneContentCache.Add(maybeType.Value, sidePaneContent);
                return sidePaneContent;
            }
        }

        private async void VideoPlayerPageViewModel_VideoQualityChanged(object sender, NicoVideoQuality e)
        {
            if (IsDisposed) { return; }
            if (IsNotSupportVideoType) { return; }

            _PreviosPlayingVideoPosition = ReadVideoPosition.Value;

            RequestVideoQuality.Value = e;

            await PlayingQualityChangeAction(e);
        }

        public static EmptySidePaneContentViewModel EmptySidePaneContent { get; } = EmptySidePaneContentViewModel.Default;
        public VideoCacheManager VideoCacheManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public ChannelProvider ChannelProvider { get; }
        public MylistProvider MylistProvider { get; }
        public PlayerSettings PlayerSettings { get; }
        public PlaylistSettings PlaylistSettings { get; }
        public CacheSettings CacheSettings { get; }
        public NGSettings NgSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public PlayerViewManager PlayerViewManager { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }
        public Commands.Mylist.CreateLocalMylistCommand CreateLocalMylistCommand { get; }
        public Commands.Mylist.CreateMylistCommand CreateMylistCommand { get; }

        string IVideoContent.ProviderId => VideoOwnerId;

        string IVideoContent.ProviderName => _VideoInfo.Owner?.ScreenName;

        UserType IVideoContent.ProviderType => _VideoInfo.Owner.UserType;

        string INiconicoObject.Id => _VideoInfo.RawVideoId;

        string INiconicoObject.Label => _VideoInfo.Title;

        IMylist IVideoContent.OnwerPlaylist => CurrentPlaylist;

        NotificationService _NotificationService;
        DialogService _HohoemaDialogService;


        // TODO: コメントのNGユーザー登録
        internal Task AddNgUser(Comment commentViewModel)
		{
			if (commentViewModel.UserId == null) { return Task.CompletedTask; }

			string userName = "";
			try
			{
//				var commentUser = await _HohoemaApp.ContentFinder.GetUserInfo(commentViewModel.UserId);
//				userName = commentUser.Nickname;
			}
			catch { }

		    NgSettings.NGCommentUserIds.Add(new UserIdInfo()
			{
				UserId = commentViewModel.UserId,
				Description = userName
			});

			return Task.CompletedTask;
		}

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = null;
            return false;
        }
    }


	


	

	public class TagViewModel : Interfaces.ITag
    {
		public string Tag { get; private set; }
		public bool IsCategoryTag { get; internal set; }
		public bool IsLocked { get; internal set; }

        public TagViewModel(string tag)
		{
            Tag = tag;
			IsCategoryTag = false;
			IsLocked = false;
		}
	}


	public enum PlayerSidePaneContentType
	{
        Playlist,
		Comment,
		Setting,
        RelatedVideos,
    }










}

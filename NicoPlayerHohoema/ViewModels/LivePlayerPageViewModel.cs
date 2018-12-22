using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using Prism.Windows.Navigation;
using System.Threading;
using NicoPlayerHohoema.Models.Helpers;
using System.Diagnostics;
using NicoPlayerHohoema.Models.Live;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Windows.UI;
using Prism.Commands;
using System.Reactive.Concurrency;
using Windows.UI.ViewManagement;
using System.Reactive.Linq;
using NicoPlayerHohoema.Services;
using Windows.UI.Xaml.Media;
using Windows.Media.Playback;
using System.Collections.ObjectModel;
using Windows.System;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using NicoPlayerHohoema.ViewModels.PlayerSidePaneContent;
using Windows.UI.Core;
using System.Collections.Concurrent;
using Windows.UI.Xaml;
using NicoPlayerHohoema.Models.Provider;
using Microsoft.Practices.Unity;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Interfaces;

namespace NicoPlayerHohoema.ViewModels
{
    public class LiveOperationCommand
    {
        public VerticalAlignment? DisplayPosition;
        public string Header { get; set; }
        public string Content { get; set; }
        public Uri Hyperlink { get; set; }
        public Color Color { get; set; }
        private DelegateCommand _OpenHyperlinkCommand;
        public DelegateCommand OpenHyperlinkCommand
        {
            get
            {
                return _OpenHyperlinkCommand
                    ?? (_OpenHyperlinkCommand = new DelegateCommand(async () => 
                    {
                        var result = await Launcher.LaunchUriAsync(Hyperlink);
                    }));
            }
        }
    }


	public class LivePlayerPageViewModel : HohoemaViewModelBase, IDisposable, Interfaces.ILiveContent
	{

        public LivePlayerPageViewModel(
            IScheduler scheduler,
            NGSettings ngSettings,
            PlayerSettings playerSettings,
            PlaylistSettings playlistSettings,
            NicoLiveProvider nicoLiveProvider,
            AppearanceSettings appearanceSettings,
            LoginUserLiveReservationProvider loginUserLiveReservationProvider,
            NiconicoSession niconicoSession,
            UserProvider userProvider,
            CommunityProvider communityProvider,
            Services.HohoemaPlaylist hohoemaPlaylist,
            PlayerViewManager playerViewManager,
            Services.DialogService dialogService,
            PageManager pageManager,
            NotificationService notificationService,
            ExternalAccessService externalAccessService
            )
            : base(pageManager)
        {
            Scheduler = scheduler;
            NGSettings = ngSettings;
            PlayerSettings = playerSettings;
            PlaylistSettings = playlistSettings;
            NicoLiveProvider = nicoLiveProvider;
            AppearanceSettings = appearanceSettings;
            LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
            NiconicoSession = niconicoSession;
            UserProvider = userProvider;
            CommunityProvider = communityProvider;
            HohoemaPlaylist = hohoemaPlaylist;
            PlayerViewManager = playerViewManager;

            _HohoemaDialogService = dialogService;
            _NotificationService = notificationService;
            ExternalAccessService = externalAccessService;
            MediaPlayer = PlayerViewManager.GetCurrentWindowMediaPlayer();

            LiveComments = new ReadOnlyObservableCollection<Views.Comment>(_LiveComments);
            FilterdComments.Source = LiveComments;

            // play
            WatchStartLiveElapsedTime = new ReactiveProperty<TimeSpan>(raiseEventScheduler: CurrentWindowContextScheduler, initialValue: TimeSpan.Zero);
            CurrentState = new ReactiveProperty<MediaElementState>(MediaElementState.Closed);
            NowPlaying = CurrentState.Select(x => x == MediaElementState.Playing)
                .ToReactiveProperty(CurrentWindowContextScheduler);

            NowConnecting = Observable.CombineLatest(
                CurrentState.Select(x => x == MediaElementState.Opening || x == MediaElementState.Buffering)
                )
                .Select(x => x.Any(y => y))
                .ToReadOnlyReactiveProperty(eventScheduler: CurrentWindowContextScheduler)
                .AddTo(_NavigatingCompositeDisposable);

            LivePlayerType = new ReactiveProperty<Models.Live.LivePlayerType?>(CurrentWindowContextScheduler);
            LiveStatusType = new ReactiveProperty<Models.Live.LiveStatusType?>(CurrentWindowContextScheduler);

            CanChangeQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            RequestQuality = new ReactiveProperty<string>(CurrentWindowContextScheduler);
            CurrentQuality = new ReactiveProperty<string>(CurrentWindowContextScheduler);

            IsLowLatency = PlayerSettings.ObserveProperty(x => x.LiveWatchWithLowLatency)
                .ToReadOnlyReactiveProperty(eventScheduler: CurrentWindowContextScheduler);

            IsAvailableSuperLowQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            IsAvailableLowQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            IsAvailableNormalQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            IsAvailableHighQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);

            ChangeQualityCommand = new DelegateCommand<string>(
                (quality) =>
                {
                    //                    NicoLiveVideo.ChangeQualityRequest(quality, PlayerSettings.LiveWatchWithLowLatency).ConfigureAwait(false);
                    PlayerSettings.DefaultLiveQuality = quality;
                },
                (quality) => NicoLiveVideo.Qualities.Any(x => x == quality)
            );

            IsCommentDisplayEnable = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsCommentDisplay_Live, PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            CommentCanvasHeight = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0.0).AddTo(_CompositeDisposable);
            CommentDefaultColor = new ReactiveProperty<Color>(PlayerWindowUIDispatcherScheduler, Colors.White).AddTo(_CompositeDisposable);

            CommentOpacity = PlayerSettings.ObserveProperty(x => x.CommentOpacity)
                .Select(x => x.ToOpacity())
                .ToReadOnlyReactiveProperty(eventScheduler: PlayerWindowUIDispatcherScheduler);

            FilterdComments.SortDescriptions.Add(new Microsoft.Toolkit.Uwp.UI.SortDescription(nameof(Views.Comment.VideoPosition), Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending));
            FilterdComments.Filter = (x) => !(NGSettings.IsLiveNGComment((x as Views.Comment)?.UserId));
            NGSettings.NGLiveCommentUserIds.CollectionChangedAsObservable()
                .Subscribe(x =>
                {
                    FilterdComments.RefreshFilter();
                })
                .AddTo(_CompositeDisposable);


            IsWatchWithTimeshift = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
                .AddTo(_CompositeDisposable);

            SeekVideoCommand = IsWatchWithTimeshift.ToReactiveCommand<TimeSpan?>(scheduler: CurrentWindowContextScheduler)
                    .AddTo(_CompositeDisposable);
            SeekVideoCommand.Subscribe(async time =>
            {
                if (!time.HasValue) { return; }
                var session = MediaPlayer.PlaybackSession;

                NicoLiveVideo.TimeshiftPosition = (NicoLiveVideo.TimeshiftPosition ?? TimeSpan.Zero) + session.Position + time;
                await NicoLiveVideo.Refresh();
            })
            .AddTo(_CompositeDisposable);

            SeekBarTimeshiftPosition = new ReactiveProperty<double>(CurrentWindowContextScheduler, 0.0, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);
            _MaxSeekablePosition = new ReactiveProperty<double>(CurrentWindowContextScheduler, 0.0)
                .AddTo(_CompositeDisposable);

            SeekBarTimeshiftPosition
                .Where(_ => !_NowSeekBarPositionChanging)
                .Throttle(TimeSpan.FromSeconds(0.25))
                .Subscribe(async x =>
                {
                    var time = TimeSpan.FromSeconds(x);

                    var session = MediaPlayer.PlaybackSession;
                    NicoLiveVideo.TimeshiftPosition = time;
                    WatchStartLiveElapsedTime.Value = time;
                    await NicoLiveVideo.Refresh();
                })
                .AddTo(_CompositeDisposable);

            // post comment
            WritingComment = new ReactiveProperty<string>(PlayerWindowUIDispatcherScheduler, "").AddTo(_CompositeDisposable);
            NowCommentWriting = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler).AddTo(_CompositeDisposable);
            NowSubmittingComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler).AddTo(_CompositeDisposable);

            CommandString = new ReactiveProperty<string>(PlayerWindowUIDispatcherScheduler, "").AddTo(_CompositeDisposable);
            CommandEditerVM = new CommentCommandEditerViewModel();
            CommandEditerVM.OnCommandChanged += CommandEditerVM_OnCommandChanged;
            CommandEditerVM.ChangeEnableAnonymity(true);
            CommandEditerVM.IsAnonymousDefault = PlayerSettings.IsDefaultCommentWithAnonymous;
            CommandEditerVM.IsAnonymousComment.Value = PlayerSettings.IsDefaultCommentWithAnonymous;

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
            })
            .AddTo(_CompositeDisposable);


            // operation command
            BroadcasterLiveOperationCommand = new ReactiveProperty<LiveOperationCommand>(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            OperaterLiveOperationCommand = new ReactiveProperty<LiveOperationCommand>(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            PressLiveOperationCommand = new ReactiveProperty<LiveOperationCommand>(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);


            // sound
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                // プレイヤーを閉じた際のコンパクトオーバーレイの解除はPlayerWithPageContainerViewModel側で行う
                IsCompactOverlay = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler,
                    ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay
                    );
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
                IsCompactOverlay = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
            }

            IsFullScreen = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false).AddTo(_CompositeDisposable);
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


            IsSmallWindowModeEnable = PlayerViewManager
                .ObserveProperty(x => x.IsPlayerSmallWindowModeEnabled)
                .ToReadOnlyReactiveProperty(eventScheduler: PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);


            Suggestion = new ReactiveProperty<LiveSuggestion>(PlayerWindowUIDispatcherScheduler);
            HasSuggestion = Suggestion.Select(x => x != null)
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler);

            IsDisplayControlUI = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, true);

            if (Services.Helpers.InputCapabilityHelper.IsMouseCapable && !AppearanceSettings.IsForceTVModeEnable)
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
                IsAutoHideEnable = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
                IsMouseCursolAutoHideEnable = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
            }

            AutoHideDelayTime = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.FromSeconds(3));



            IsMuted = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsMute, PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);


            SoundVolume = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.SoundVolume, PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);


            CommentUpdateInterval = PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
                .Select(x => TimeSpan.FromSeconds(1.0 / x))
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            RequestCommentDisplayDuration = PlayerSettings
                .ObserveProperty(x => x.CommentDisplayDuration)
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            CommentFontScale = PlayerSettings
                .ObserveProperty(x => x.DefaultCommentFontScale)
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);


            IsForceLandscape = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsForceLandscape, PlayerWindowUIDispatcherScheduler);



            // Side Pane

            CurrentSidePaneContentType = new ReactiveProperty<PlayerSidePaneContentType?>(PlayerWindowUIDispatcherScheduler, _PrevPrevSidePaneContentType)
                .AddTo(_CompositeDisposable);
            CurrentSidePaneContent = CurrentSidePaneContentType
                .Select(GetSidePaneContent)
                .ToReadOnlyReactiveProperty(eventScheduler: PlayerWindowUIDispatcherScheduler)
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

            Observable.Merge(
                PlayerSettings.ObserveProperty(x => x.LiveWatchWithLowLatency).ToUnit(),
                PlayerSettings.ObserveProperty(x => x.DefaultLiveQuality).ToUnit()
                )
                .Subscribe(async x =>
                {
                    if (NicoLiveVideo != null)
                    {
                        await NicoLiveVideo.ChangeQualityRequest(
                            PlayerSettings.DefaultLiveQuality,
                            PlayerSettings.LiveWatchWithLowLatency
                            );
                    }
                })
                .AddTo(_CompositeDisposable);

            // Noneの時
            // OnAirかCommingSoonの時

            CanRefresh = Observable.CombineLatest(
                this.CurrentState.Select(x => x == MediaElementState.Closed || x == MediaElementState.Paused),
                this.LiveStatusType.Select(x => x == Models.Live.LiveStatusType.OnAir || x == Models.Live.LiveStatusType.ComingSoon)
                )
                .Select(x => x.All(y => y))
                .ToReadOnlyReactiveProperty(eventScheduler: PlayerWindowUIDispatcherScheduler);

            RefreshCommand = CanRefresh
                .ToReactiveCommand(scheduler: PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            RefreshCommand.Subscribe(async _ =>
            {
                if (IsWatchWithTimeshift.Value)
                {
                    MediaPlayer.Play();
                }
                else
                {
                    if (await TryUpdateLiveStatus())
                    {
                        await NicoLiveVideo.Refresh();

                        // MediaPlayer.PositionはSourceを再設定するたびに0にリセットされる
                        // ソース更新後のコメント表示再生位置のズレを補正する
                        if (!IsWatchWithTimeshift.Value)
                        {
                            WatchStartLiveElapsedTime.Value = (DateTimeOffset.Now.ToOffset(NicoLiveVideo.JapanTimeZoneOffset) - _OpenAt);
                        }
                    }
                }
            })
            .AddTo(_CompositeDisposable);

            TogglePlayPauseCommand = new ReactiveCommand(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);

            TogglePlayPauseCommand.Subscribe(_ =>
            {
                if (IsWatchWithTimeshift.Value)
                {
                    if (MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                    {
                        MediaPlayer.Pause();
                    }
                    else
                    {
                        MediaPlayer.Play();
                    }
                }
                else
                {
                    MediaPlayer.Source = null;
                }
            })
            .AddTo(_CompositeDisposable);


            CurrentState.Where(x => x == MediaElementState.Playing)
                .SubscribeOnUIDispatcher()
                .Subscribe(_ =>
                {
                    IsDisplayControlUI.Value = false;
                })
                .AddTo(_CompositeDisposable);


            CanSeek = Observable.CombineLatest(IsWatchWithTimeshift, CurrentState.Select(x => x == MediaElementState.Paused || x == MediaElementState.Playing || x == MediaElementState.Stopped))
                .Select(x => x.All(y => y))
                .ToReadOnlyReactiveProperty(eventScheduler: PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);
        }




        private SynchronizationContextScheduler _PlayerWindowUIDispatcherScheduler;
		public SynchronizationContextScheduler PlayerWindowUIDispatcherScheduler
		{
			get
			{
				return _PlayerWindowUIDispatcherScheduler
					?? (_PlayerWindowUIDispatcherScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
			}
		}

        public IScheduler Scheduler { get; }

        public NGSettings NGSettings { get; }
        public PlayerSettings PlayerSettings { get; }
        public PlaylistSettings PlaylistSettings { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserProvider UserProvider { get; }
        public CommunityProvider CommunityProvider { get; }
        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public DialogService _HohoemaDialogService { get; }
        public ExternalAccessService ExternalAccessService { get; }

        private NotificationService _NotificationService;
        public PlayerViewManager PlayerViewManager { get; }


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

		private uint? _SeetNumber;
		public uint? SeetId
		{
			get { return _SeetNumber; }
			set { SetProperty(ref _SeetNumber, value); }
		}


		public NicoLiveVideo NicoLiveVideo { get; private set; }


        private ObservableCollection<Views.Comment> _LiveComments { get; } = new ObservableCollection<Views.Comment>();
        public ReadOnlyObservableCollection<Views.Comment> LiveComments { get; private set; } 

        public ObservableCollection<LiveOperationCommand> LiveOperationCommands { get; private set; } = new ObservableCollection<LiveOperationCommand>();
        public ReactiveProperty<LiveOperationCommand> BroadcasterLiveOperationCommand { get; }
        public ReactiveProperty<LiveOperationCommand> OperaterLiveOperationCommand { get; }
        public ReactiveProperty<LiveOperationCommand> PressLiveOperationCommand { get; }

        private TimeSpan _LiveElapsedTime;
		public TimeSpan LiveElapsedTime
		{
			get { return _LiveElapsedTime; }
			set { SetProperty(ref _LiveElapsedTime, value); }
		}


        
        private DateTimeOffset _OpenAt;
        private DateTimeOffset _StartAt;
        private DateTimeOffset _EndAt;

        public ReactiveProperty<bool> IsWatchWithTimeshift { get; private set; }

        public ReactiveProperty<TimeSpan> WatchStartLiveElapsedTime { get; private set; }
        // play
        public ReactiveProperty<MediaElementState> CurrentState { get; private set; }
        public ReactiveProperty<bool> NowPlaying { get; private set; }
		public ReadOnlyReactiveProperty<bool> NowConnecting { get; private set; }

		public ReactiveProperty<uint> CommentCount { get; private set; }
		public ReactiveProperty<uint> WatchCount { get; private set; }

        public ReactiveProperty<LivePlayerType?> LivePlayerType { get; private set; }
        public ReactiveProperty<LiveStatusType?> LiveStatusType { get; private set; }

        public ReadOnlyReactiveProperty<bool> CanRefresh { get; private set; }

        public ReactiveProperty<bool> CanChangeQuality { get; private set; }
        public ReactiveProperty<string> RequestQuality { get; private set; }

        public ReactiveProperty<string> CurrentQuality { get; private set; }

        public ReadOnlyReactiveProperty<bool> IsLowLatency { get; }


        public ReactiveProperty<bool> IsAvailableSuperLowQuality { get; }
        public ReactiveProperty<bool> IsAvailableLowQuality { get; }
        public ReactiveProperty<bool> IsAvailableNormalQuality { get; }
        public ReactiveProperty<bool> IsAvailableHighQuality { get; }

        public DelegateCommand<string> ChangeQualityCommand { get; }

        public ReactiveCommand<TimeSpan?> SeekVideoCommand { get; private set; }

        public IReadOnlyReactiveProperty<bool> CanSeek { get; }
        public ReactiveProperty<double> SeekBarTimeshiftPosition { get; }
        private ReactiveProperty<double> _MaxSeekablePosition { get; }
        public IReadOnlyReactiveProperty<double> MaxSeekablePosition => _MaxSeekablePosition;
        private bool _NowSeekBarPositionChanging = false;


        // comment

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

        public Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView FilterdComments { get; } = new Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView();


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
        public ReadOnlyReactiveProperty<bool> IsSmallWindowModeEnable { get; private set; }


		// suggestion
		public ReactiveProperty<LiveSuggestion> Suggestion { get; private set; }
		public ReactiveProperty<bool> HasSuggestion { get; private set; }


        // Side Pane Content
        



        #region Command

        private DelegateCommand _ClosePlayerCommand;
        public DelegateCommand ClosePlayerCommand
        {
            get
            {
                return _ClosePlayerCommand
                    ?? (_ClosePlayerCommand = new DelegateCommand(() =>
                    {
                        PlayerViewManager.ClosePlayer();
                    }
                    ));
            }
        }



        public ReactiveCommand RefreshCommand { get; private set; }

        public ReactiveCommand TogglePlayPauseCommand { get; private set; }


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


        private DelegateCommand _ShareCommand;
        public DelegateCommand ShareCommand
        {
            get
            {
                return _ShareCommand
                    ?? (_ShareCommand = new DelegateCommand(() =>
                    {
                        Services.Helpers.ShareHelper.Share(NicoLiveVideo);
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
                        await Services.Helpers.ShareHelper.ShareToTwitter(NicoLiveVideo);
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
                        Services.Helpers.ClipboardHelper.CopyToClipboard(NicoLiveVideo);
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
                SoundVolume.Subscribe(volume =>
                {
                    MediaPlayer.Volume = volume;
                })
                .AddTo(_NavigatingCompositeDisposable);
                IsMuted.Subscribe(isMuted =>
                {
                    MediaPlayer.IsMuted = isMuted;
                })
                .AddTo(_NavigatingCompositeDisposable);

                NicoLiveVideo = new NicoLiveVideo(
                    LiveId, 
                    MediaPlayer,
                    NiconicoSession,
                    NicoLiveProvider,
                    LoginUserLiveReservationProvider,
                    PlayerSettings,
                    Scheduler,
                    CommunityId
                    );
                
                NicoLiveVideo.LiveComments.ObserveAddChanged()
                    .Subscribe(x =>
				{
					var comment = new Views.LiveComment(NGSettings);

                    comment.VideoPosition = x.Vpos;

                    // EndPositionはコメントレンダラが再計算するが、仮置きしないと表示対象として処理されない
                    comment.EndPosition = comment.VideoPosition + 500;

                    comment.CommentText = x.Content;
					comment.CommentId = (uint)x.No;
                    comment.IsAnonimity = x.IsAnonymity;
					comment.UserId = x.UserId;
					comment.IsOwnerComment = x.UserId == NicoLiveVideo?.BroadcasterId;
                    comment.IsOperationCommand = x.IsOperater && x.HasOperatorCommand;

                    if (!comment.IsAnonimity && TryResolveUserId(comment.UserId, out var owner))
                    {
                        comment.UserName = owner.ScreenName;
                        comment.IconUrl = owner.IconUrl;
                    }
                    else
                    {
                        UserIdToComments.AddOrUpdate(comment.UserId
                            , (key) => new List<Views.LiveComment>() { comment }
                            , (key, list) =>
                            {
                                list.Add(comment);
                                return list;
                            }
                        );
                    }

                    try
					{
						comment.IsLoginUserComment = !comment.IsAnonimity ? NiconicoSession.IsLoginUserId(x.UserId) : false;
					}
					catch { }
                    
                    if (!string.IsNullOrEmpty(x.Mail))
                    {
                        comment.ApplyCommands(x.Mail.Split(' '));
                    }

                    CurrentWindowContextScheduler.Schedule(() => 
                    {
                        _LiveComments.Add(comment as Views.Comment);
                    });
				}
                )
                .AddTo(_NavigatingCompositeDisposable);

                NicoLiveVideo.ObserveProperty(x => x.LiveStatusType)
                    .Subscribe(x =>
                    {
                        CurrentWindowContextScheduler.Schedule(() =>
                        {
                            LiveStatusType.Value = x;
                        });
                    })
                    .AddTo(_NavigatingCompositeDisposable);

                FilterdComments.Source = LiveComments;

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


                NicoLiveVideo.OpenLive += NicoLiveVideo_OpenLive;
                NicoLiveVideo.CloseLive += NicoLiveVideo_CloseLive;
                NicoLiveVideo.FailedOpenLive += NicoLiveVideo_FailedOpenLive1;
                NicoLiveVideo.OperationCommandRecieved += NicoLiveVideo_OperationCommandRecieved;
                NicoLiveVideo.LiveElapsed += NicoLiveVideo_LiveElapsed;
            }

			base.OnNavigatedTo(e, viewModelState);
		}

        private void NicoLiveVideo_LiveElapsed(object sender, TimeSpan e)
        {
            PlayerWindowUIDispatcherScheduler.Schedule(() => 
            {
                LiveElapsedTime = e;

                if (NicoLiveVideo?.IsWatchWithTimeshift == true)
                {
                    _NowSeekBarPositionChanging = true;
                    SeekBarTimeshiftPosition.Value = (NicoLiveVideo.LiveElapsedTimeFromOpen).TotalSeconds;
                    _NowSeekBarPositionChanging = false;
                }
            });
        }

        protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
            IsDisplayControlUI.Value = true;

            try
            {
                var liveInfo = await NiconicoSession.Context.Live.GetLiveVideoInfoAsync(LiveId);
                if (liveInfo != null && liveInfo.IsOK)
                {
                    LiveTitle = liveInfo.VideoInfo.Video.Title;
                    Title = LiveTitle;

                    _OpenAt = liveInfo.VideoInfo.Video.OpenTime.Value;
                    _StartAt = liveInfo.VideoInfo.Video.StartTime.Value;
                    _EndAt = liveInfo.VideoInfo.Video.EndTime.Value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            try
            {
                using (var defer = FilterdComments.DeferRefresh())
                {
                    await TryStartViewing();
                }

                RoomName = NicoLiveVideo.RoomName;

                cancelToken.ThrowIfCancellationRequested();
            }
            catch
            {
                NicoLiveVideo?.Dispose();
                NicoLiveVideo = null;

                throw;
            }

            await base.NavigatedToAsync(cancelToken, e, viewModelState);
		}



		protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			if (!suspending)
			{
                if (NicoLiveVideo != null)
                {
                    NicoLiveVideo.OpenLive -= NicoLiveVideo_OpenLive;
                    NicoLiveVideo.CloseLive -= NicoLiveVideo_CloseLive;
                    NicoLiveVideo.FailedOpenLive -= NicoLiveVideo_FailedOpenLive1;
                    NicoLiveVideo.OperationCommandRecieved -= NicoLiveVideo_OperationCommandRecieved;
                    
                    NicoLiveVideo.Dispose();
                    NicoLiveVideo = null;
                }
            }

            CancelUserInfoResolvingTask();

            MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;

			IsFullScreen.Value = false;

            _PrevPrevSidePaneContentType = CurrentSidePaneContentType.Value;
            CurrentSidePaneContentType.Value = null;

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

            CurrentState.Value = MediaElementState.Closed;

            try
            {
                
                MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;

                await NicoLiveVideo.StartLiveWatchingSessionAsync();

                IsWatchWithTimeshift.Value = NicoLiveVideo.IsWatchWithTimeshift;

                if (!IsWatchWithTimeshift.Value)
                {
                    WatchStartLiveElapsedTime.Value = (DateTimeOffset.Now.ToOffset(NicoLiveVideo.JapanTimeZoneOffset) - _OpenAt);
                    ResetSuggestion(NicoLiveVideo.LiveStatusType);
                }
                else
                {
                    WatchStartLiveElapsedTime.Value = NicoLiveVideo.TimeshiftPosition.Value;
                }

                Debug.WriteLine(this.NicoLiveVideo.LiveInfo.VideoInfo.Video.TsArchiveStartTime);
                Debug.WriteLine(this.NicoLiveVideo.LiveInfo.VideoInfo.Video.TsArchiveEndTime);

                _MaxSeekablePosition.Value = (_EndAt - _OpenAt).TotalSeconds;
            }
            catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
            }
            finally
			{
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

            Debug.WriteLine(sender.PlaybackState.ToString());
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
                await NicoLiveVideo.UpdateLiveStatus();
                
				if (NicoLiveVideo.PlayerStatusResponse != null)
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
			}

			ResetSuggestion(liveStatus);

			NowSubmittingComment.Value = false;

			return liveStatus == null;
		}
		
		/// <summary>
		/// 生放送終了後などに表示するユーザーアクションの候補を再設定します。
		/// </summary>
		/// <param name="liveStatus"></param>
		private void ResetSuggestion(LiveStatusType? liveStatus)
		{
			if (liveStatus == null || liveStatus == Models.Live.LiveStatusType.OnAir || IsWatchWithTimeshift.Value)
			{
				Suggestion.Value = null;
			}
			else
			{
				LiveSuggestion suggestion = null;

				suggestion = liveStatus.Value.Make(NicoLiveVideo, PageManager, NiconicoSession);

				if (suggestion == null)
				{
					Debug.WriteLine("live suggestion not support : " + liveStatus.Value.ToString());
				}

				Suggestion.Value = suggestion;
			}
		}

        private void ResetSuggestion(string message)
        {
            LiveSuggestion suggestion = null;

            // ブラウザ視聴を案内
            suggestion = new LiveSuggestion(message, new[] { new SuggestAction("ブラウザで視聴", async () =>
            {
                var livePageUrl = new Uri($"http://live.nicovideo.jp/watch/" + LiveId);
                await Launcher.LaunchUriAsync(livePageUrl );
            })});

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




        #endregion


        #region NicoLiveVideo Event Handling

        private void NicoLiveVideo_OperationCommandRecieved(object sender, OperationCommandRecievedEventArgs e)
        {
            LiveOperationCommand operationCommand = null;
            var vpos = TimeSpan.FromMilliseconds(e.Chat.Vpos * 10);
//            var relatedVpos = vpos - WatchStartLiveElapsedTime;
            bool isDisplayComment = (this.LiveElapsedTime - vpos) < TimeSpan.FromSeconds(10);
            switch (e.CommandType)
            {
                case "perm":
                    {
                        var content = e.CommandParameter.ElementAtOrDefault(0)?.Trim('"');

                        HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                        document.LoadHtml(content);

                        var node = document.DocumentNode;

                        var comment = node.InnerText;



                        string link = null;
                        try
                        {
                            link = node.Descendants("a")?.FirstOrDefault()?.Attributes["href"]?.Value;
                        }
                        catch { }


                        operationCommand = new LiveOperationCommand()
                        {
                            DisplayPosition = VerticalAlignment.Top,
                            Content = comment,
                        };
                        if (!string.IsNullOrEmpty(link))
                        {
                            operationCommand.Hyperlink = new Uri(link);
                        }


                        BroadcasterLiveOperationCommand.Value = operationCommand;
                    }
                    break;
                case "press":
                    {
                        if (!isDisplayComment) { return; }

                        var color = e.CommandParameter.ElementAtOrDefault(1);
                        var comment = e.CommandParameter.ElementAtOrDefault(2)?.Trim('"');
                        var screenName = e.CommandParameter.ElementAtOrDefault(3)?.Trim('"');
                        if (!string.IsNullOrWhiteSpace(comment))
                        {
                            // 表示
                            operationCommand = new LiveOperationCommand()
                            {
                                DisplayPosition = VerticalAlignment.Bottom,
                                Content = comment,
                                Header = screenName,
                            };
                        }

                        PressLiveOperationCommand.Value = operationCommand;
                    }
                    break;
                case "info":
                    {
                        if (!isDisplayComment) { return; }

                        var type = int.Parse(e.CommandParameter.ElementAtOrDefault(0));
                        var comment = e.CommandParameter.ElementAtOrDefault(1)?.Trim('"');

                        operationCommand = new LiveOperationCommand()
                        {
                            DisplayPosition = VerticalAlignment.Bottom,
                            Content = comment,
                        };

                        OperaterLiveOperationCommand.Value = operationCommand;
                    }
                    break;
                case "telop":
                    {
                        if (!isDisplayComment) { return; }

                        // プレイヤー下部に情報表示
                        var type = e.CommandParameter.ElementAtOrDefault(0);
                        var comment = e.CommandParameter.ElementAtOrDefault(1)?.Trim('"');

                        //on ニコ生クルーズ(リンク付き)/ニコニコ実況コメント
                        //show クルーズが到着/実況に接続
                        //show0 実際に流れているコメント
                        //perm ニコ生クルーズが去って行きました＜改行＞(降りた人の名前、人数)
                        //off (プレイヤー下部のテロップを消去)

                        if (type == "off")
                        {
                            OperaterLiveOperationCommand.Value = null;
                        }
                        else// if (type == "perm")
                        {
                            operationCommand = new LiveOperationCommand()
                            {
                                DisplayPosition = VerticalAlignment.Bottom,
                                Content = comment,
                            };

                            OperaterLiveOperationCommand.Value = operationCommand;
                        }
                    }
                    break;
                case "uadpoint":
                    {
                        if (!isDisplayComment) { return; }

                        var liveId_wo_lv = e.CommandParameter.ElementAtOrDefault(0);
                        var adpoint = e.CommandParameter.ElementAtOrDefault(1);

                        /*
                        operationCommand = new LiveOperationCommand()
                        {
                            DisplayPosition = VerticalAlignment.Bottom,
                            Content = $"広告総ポイント {adpoint}pt",
                        };

                        OperaterLiveOperationCommand.Value = operationCommand;
                        */
                    }
                    break;
                case "koukoku":
                    {
                        if (!isDisplayComment) { return; }

                        var content = e.CommandParameter.ElementAtOrDefault(0)?.Trim('"');

                        HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                        document.LoadHtml(content);

                        var node = document.DocumentNode;

                        var comment = node.InnerText;
                        string link = null;
                        try
                        {
                            link = node.Descendants("a")?.FirstOrDefault()?.Attributes["href"]?.Value;
                        }
                        catch { }

                        operationCommand = new LiveOperationCommand()
                        {
                            DisplayPosition = VerticalAlignment.Top,
                            Content = comment,
                        };
                        if (!string.IsNullOrEmpty(link))
                        {
                            operationCommand.Hyperlink = new Uri(link);
                        }

                    }
                    break;
                case "disconnect":
                    if (TogglePlayPauseCommand.CanExecute())
                    {
                        TogglePlayPauseCommand.Execute();
                    }

                    ResetSuggestion(LiveStatusType.Value = Models.Live.LiveStatusType.Closed);
                    break;
                case "clear":
                case "cls":
                    BroadcasterLiveOperationCommand.Value = null;
                    break;
                default:
                    Debug.WriteLine($"非対応な運コメ：{e.CommandType}");
                    break;
            }

            if (operationCommand != null)
            {
                LiveOperationCommands.Add(operationCommand);
            }
        }

        private void NicoLiveVideo_FailedOpenLive1(NicoLiveVideo sender, FailedOpenLiveEventArgs args)
        {
            this.ResetSuggestion(args.Message);
        }

        private void NicoLiveVideo_CloseLive(NicoLiveVideo sender)
        {
        }

        private async void NicoLiveVideo_OpenLive(NicoLiveVideo sender)
        {
            Debug.WriteLine("NicoLiveVideo_OpenLive");

            if (NicoLiveVideo.LiveStatusType == Models.Live.LiveStatusType.OnAir ||
                    NicoLiveVideo.LiveStatusType == Models.Live.LiveStatusType.ComingSoon ||
                    NicoLiveVideo.IsWatchWithTimeshift
                    )
            {
                CurrentState.Value = MediaElementState.Opening;

                LivePlayerType.Value = NicoLiveVideo.LivePlayerType;

                RaisePropertyChanged(nameof(MediaPlayer));

                CommunityId = NicoLiveVideo.BroadcasterCommunityId;


                RaisePropertyChanged(nameof(NicoLiveVideo));

                if (CommunityName == null)
                {
                    if (CommunityId == null)
                    {
                        CommunityId = NicoLiveVideo.BroadcasterCommunityId;
                    }

                    try
                    {
                        var communityDetail = await CommunityProvider.GetCommunityInfo(CommunityId);
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

                            // MediaPlayer.PositionはSourceを再設定するたびに0にリセットされる
                            // ソース更新後のコメント表示再生位置のズレを補正する
                            if (!IsWatchWithTimeshift.Value)
                            {
                                WatchStartLiveElapsedTime.Value = (DateTimeOffset.Now.ToOffset(NicoLiveVideo.JapanTimeZoneOffset) - _OpenAt);
                            }
                        });

                    NicoLiveVideo.ObserveProperty(x => x.Qualities)
                        .Subscribe(types =>
                        {
                            IsAvailableSuperLowQuality.Value = types?.Any(x => x == "super_low") ?? false;
                            IsAvailableLowQuality.Value = types?.Any(x => x == "low") ?? false;
                            IsAvailableNormalQuality.Value = types?.Any(x => x == "normal") ?? false;
                            IsAvailableHighQuality.Value = types?.Any(x => x == "high") ?? false;

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

                    // コメントのユーザー名解決
                    _ = CommentUserInfoResolvingAsync().ConfigureAwait(false);
                }
                else
                {
                    // seet
                    if (NicoLiveVideo.PlayerStatusResponse != null)
                    {
                        RoomName = NicoLiveVideo.PlayerStatusResponse.Room.Name;
                        SeetId = NicoLiveVideo.PlayerStatusResponse.Room.SeatId;
                    }
                }
            }
            else
            {
                ResetSuggestion(NicoLiveVideo.LiveStatusType);

                Debug.WriteLine("生放送情報の取得失敗しました " + LiveId);
            }

            
        }

        

        #endregion


        #region Side Pane Content


        private Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase> _SidePaneContentCache = new Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase>();

        public ReactiveProperty<PlayerSidePaneContentType?> CurrentSidePaneContentType { get; }
        public ReadOnlyReactiveProperty<SidePaneContentViewModelBase> CurrentSidePaneContent { get; }


        static PlayerSidePaneContentType? _PrevPrevSidePaneContentType;
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
            if (maybeType.HasValue && _SidePaneContentCache.ContainsKey(maybeType.Value))
            {
                return _SidePaneContentCache[maybeType.Value];
            }
            else if (!maybeType.HasValue)
            {
                return EmptySidePaneContentViewModel.Default;
            }
            else
            {
                SidePaneContentViewModelBase sidePaneContent = null;
                switch (maybeType.Value)
                {
                    case PlayerSidePaneContentType.Playlist:
                        sidePaneContent = App.Current.Container.Resolve<PlaylistSidePaneContentViewModel>();
                        break;
                    case PlayerSidePaneContentType.Comment:                        
                        sidePaneContent = App.Current.Container.Resolve<LiveCommentSidePaneContentViewModel>(new ParameterOverride("comments", FilterdComments));
                        break;
                    case PlayerSidePaneContentType.Setting:
                        sidePaneContent = App.Current.Container.Resolve<SettingsSidePaneContentViewModel>();
                        if (NicoLiveVideo != null)
                        {
                            if (LivePlayerType.Value == Models.Live.LivePlayerType.Leo)
                            {
                                (sidePaneContent as SettingsSidePaneContentViewModel).SetupAvairableLiveQualities(
                                    NicoLiveVideo.Qualities
                                    );
                                (sidePaneContent as SettingsSidePaneContentViewModel).IsLeoPlayerLive = NicoLiveVideo.LivePlayerType == Models.Live.LivePlayerType.Leo;
                            }
                        }
                        break;
                    default:
                        sidePaneContent = EmptySidePaneContentViewModel.Default;
                        break;
                }

                _SidePaneContentCache.Add(maybeType.Value, sidePaneContent);
                return sidePaneContent;
            }
        }



        #endregion



        #region CommentUserId Resolve

        private ConcurrentStack<string> UnresolvedUserId = new ConcurrentStack<string>();
        private ConcurrentDictionary<string, List<Views.LiveComment>> UserIdToComments = new ConcurrentDictionary<string, List<Views.LiveComment>>();

        private bool TryResolveUserId(string userId, out Database.NicoVideoOwner userInfo)
        {
            userInfo = Database.NicoVideoOwnerDb.Get(userId);
            if (userInfo == null)
            {
                UnresolvedUserId.Push(userId);
            }

            return userInfo != null;
        }

        private void CancelUserInfoResolvingTask()
        {
            _UserInfoResolvingTaskCancellationToken?.Cancel();
            _UserInfoResolvingTaskCancellationToken?.Dispose();
            _UserInfoResolvingTaskCancellationToken = null;
        }


        private void UpdateCommentUserName(Database.NicoVideoOwner user)
        {
            //using (var releaser = await )
            {
                if (UserIdToComments.TryGetValue(user.OwnerId, out var comments))
                {
                    CurrentWindowContextScheduler.Schedule(() =>
                    {
                        foreach (var comment in comments)
                        {
                            comment.UserName = user.ScreenName;
                            comment.IconUrl = user.IconUrl;
                        }
                    });
                }
            }
        }


        CancellationTokenSource _UserInfoResolvingTaskCancellationToken;
        Task CommentUserInfoResolvingAsync()
        {
            return Task.Run(async () =>
            {
                var token = _UserInfoResolvingTaskCancellationToken.Token;
                while (!token.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();

                    if (UnresolvedUserId.TryPop(out var id))
                    {
                        var owner = await UserProvider.GetUser(id);
                        if (owner != null)
                        {
                            UpdateCommentUserName(owner);
                        }
                    }
                    else
                    {
                        await Task.Delay(1000);
                    }
                }
            }, (_UserInfoResolvingTaskCancellationToken = new CancellationTokenSource()).Token);
        }

        private DelegateCommand _ToggleCommentListSidePaneContentCommand;
        public DelegateCommand ToggleCommentListSidePaneContentCommand
        {
            get
            {
                return _ToggleCommentListSidePaneContentCommand
                    ?? (_ToggleCommentListSidePaneContentCommand = new DelegateCommand(async () =>
                    {
                        if (CurrentSidePaneContentType.Value == PlayerSidePaneContentType.Comment)
                        {
                            CurrentSidePaneContentType.Value = null;
                        }
                        else
                        {
                            CurrentSidePaneContentType.Value = PlayerSidePaneContentType.Comment;

                            // すぐに閉じるとコメントリストUIの生成をタイミングが被って快適さが減るのでちょっと待つ
                            await Task.Delay(100);
                            IsDisplayControlUI.Value = false;
                        }
                    }));
            }
        }




        string ILiveContent.ProviderId => this.NicoLiveVideo.BroadcasterCommunityId;

        string ILiveContent.ProviderName => this.NicoLiveVideo.BroadcasterName;

        CommunityType ILiveContent.ProviderType => NicoLiveVideo.BroadcasterCommunityType.Value;

        string INiconicoObject.Id => NicoLiveVideo.LiveId;

        string INiconicoObject.Label => NicoLiveVideo.LiveTitle;


        #endregion
    }
}

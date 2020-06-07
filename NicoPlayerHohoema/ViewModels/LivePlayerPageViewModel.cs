using Mntone.Nico2.Live;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Live;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.Services.Player;
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.ViewModels.PlayerSidePaneContent;
using NicoPlayerHohoema.Views;
using Prism.Commands;
using Prism.Navigation;
using Prism.Unity;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity;
using Unity.Resolution;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

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


	public class LivePlayerPageViewModel : HohoemaViewModelBase, Interfaces.ILiveContent, INavigatedAwareAsync
	{

        public LivePlayerPageViewModel(
            IScheduler scheduler,
            PlayerSettings playerSettings,
            AppearanceSettings appearanceSettings,
            NicoLiveProvider nicoLiveProvider,
            ApplicationLayoutManager applicationLayoutManager,
            LoginUserLiveReservationProvider loginUserLiveReservationProvider,
            NiconicoSession niconicoSession,
            UserProvider userProvider,
            CommunityProvider communityProvider,
            HohoemaPlaylist hohoemaPlaylist,
            Services.DialogService dialogService,
            PageManager pageManager,
            NotificationService notificationService,
            ExternalAccessService externalAccessService,
            MediaPlayer mediaPlayer,
            ObservableMediaPlayer observableMediaPlayer,
            WindowService windowService,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            TogglePlayerDisplayViewCommand togglePlayerDisplayViewCommand,
            ShowPrimaryViewCommand showPrimaryViewCommand,
            UseCase.NicoVideoPlayer.MediaPlayerSoundVolumeManager soundVolumeManager
            )
        {
            _scheduler = scheduler;
            PlayerSettings = playerSettings;
            AppearanceSettings = appearanceSettings;
            NicoLiveProvider = nicoLiveProvider;
            ApplicationLayoutManager = applicationLayoutManager;
            LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
            NiconicoSession = niconicoSession;
            UserProvider = userProvider;
            CommunityProvider = communityProvider;
            HohoemaPlaylist = hohoemaPlaylist;

            _HohoemaDialogService = dialogService;
            PageManager = pageManager;
            _NotificationService = notificationService;
            ExternalAccessService = externalAccessService;
            MediaPlayer = mediaPlayer;
            ObservableMediaPlayer = observableMediaPlayer;
            WindowService = windowService;
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            TogglePlayerDisplayViewCommand = togglePlayerDisplayViewCommand;
            ShowPrimaryViewCommand = showPrimaryViewCommand;
            SoundVolumeManager = soundVolumeManager;
            DisplayingLiveComments = new ReadOnlyObservableCollection<Comment>(_DisplayingLiveComments);

            SeekCommand = new MediaPlayerSeekCommand(MediaPlayer);
            SetPlaybackRateCommand = new MediaPlayerSetPlaybackRateCommand(MediaPlayer);
            ToggleMuteCommand = new MediaPlayerToggleMuteCommand(MediaPlayer);
            VolumeUpCommand = new MediaPlayerVolumeUpCommand(SoundVolumeManager);
            VolumeDownCommand = new MediaPlayerVolumeDownCommand(SoundVolumeManager);

            // play
            WatchStartLiveElapsedTime = new ReactiveProperty<TimeSpan>(raiseEventScheduler: _scheduler, initialValue: TimeSpan.Zero);
            
            LivePlayerType = new ReactiveProperty<Models.Live.LivePlayerType?>(_scheduler);
            LiveStatusType = new ReactiveProperty<StatusType?>(_scheduler);

            CanChangeQuality = new ReactiveProperty<bool>(_scheduler, false);
            RequestQuality = new ReactiveProperty<string>(_scheduler);
            CurrentQuality = new ReactiveProperty<string>(_scheduler, mode: ReactivePropertyMode.DistinctUntilChanged);

            IsLowLatency = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.LiveWatchWithLowLatency, _scheduler, mode: ReactivePropertyMode.DistinctUntilChanged);

            ChangeQualityCommand = new DelegateCommand<string>(
                (quality) =>
                {
                    //                    NicoLiveVideo.ChangeQualityRequest(quality, PlayerSettings.LiveWatchWithLowLatency).ConfigureAwait(false);
                    PlayerSettings.DefaultLiveQuality = quality;
                },
                (quality) => NicoLiveVideo.Qualities.Any(x => x == quality)
            );

            IsCommentDisplayEnable = PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsCommentDisplay_Live, _scheduler)
                .AddTo(_CompositeDisposable);

            CommentCanvasHeight = new ReactiveProperty<double>(_scheduler, 0.0).AddTo(_CompositeDisposable);
            CommentDefaultColor = new ReactiveProperty<Color>(_scheduler, Colors.White).AddTo(_CompositeDisposable);

            CommentOpacity = PlayerSettings.ObserveProperty(x => x.CommentOpacity)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler);

            FilterdComments.SortDescriptions.Add(new Microsoft.Toolkit.Uwp.UI.SortDescription(nameof(Comment.VideoPosition), Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending));
            FilterdComments.Filter = (x) => !(PlayerSettings.IsLiveNGComment((x as Comment)?.UserId));
            PlayerSettings.NGLiveCommentUserIds.CollectionChangedAsObservable()
                .Subscribe(x =>
                {
                    FilterdComments.RefreshFilter();
                })
                .AddTo(_CompositeDisposable);


            IsWatchWithTimeshift = new ReactiveProperty<bool>(_scheduler, false)
                .AddTo(_CompositeDisposable);

            SeekVideoCommand = IsWatchWithTimeshift.ToReactiveCommand<TimeSpan?>(scheduler: _scheduler)
                    .AddTo(_CompositeDisposable);
            SeekVideoCommand.Subscribe(async time =>
            {
                if (!time.HasValue) { return; }
                var session = MediaPlayer.PlaybackSession;

                NicoLiveVideo.TimeshiftPosition = (NicoLiveVideo.TimeshiftPosition ?? TimeSpan.Zero) + session.Position + time;
                await NicoLiveVideo.Refresh();
            })
            .AddTo(_CompositeDisposable);

            SeekBarTimeshiftPosition = new ReactiveProperty<double>(_scheduler, 0.0, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);
            _MaxSeekablePosition = new ReactiveProperty<double>(_scheduler, 0.0)
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

            CommandString = new ReactiveProperty<string>(_scheduler, "").AddTo(_CompositeDisposable);



            // operation command
            BroadcasterLiveOperationCommand = new ReactiveProperty<LiveOperationCommand>(_scheduler)
                .AddTo(_CompositeDisposable);

            OperaterLiveOperationCommand = new ReactiveProperty<LiveOperationCommand>(_scheduler)
                .AddTo(_CompositeDisposable);

            PressLiveOperationCommand = new ReactiveProperty<LiveOperationCommand>(_scheduler)
                .AddTo(_CompositeDisposable);

            Suggestion = new ReactiveProperty<LiveSuggestion>(_scheduler);
            HasSuggestion = Suggestion.Select(x => x != null)
                .ToReactiveProperty(_scheduler);

            CommentUpdateInterval = PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
                .Select(x => TimeSpan.FromSeconds(1.0 / x))
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            RequestCommentDisplayDuration = PlayerSettings
                .ObserveProperty(x => x.CommentDisplayDuration)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            CommentFontScale = PlayerSettings
                .ObserveProperty(x => x.DefaultCommentFontScale)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);




            // Side Pane

            CurrentSidePaneContentType = new ReactiveProperty<PlayerSidePaneContentType?>(_scheduler, _PrevPrevSidePaneContentType)
                .AddTo(_CompositeDisposable);
            CurrentSidePaneContent = CurrentSidePaneContentType
                .Select(GetSidePaneContent)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
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
                        Debug.WriteLine($"Change Quality: {PlayerSettings.DefaultLiveQuality} - Low Latency: {PlayerSettings.LiveWatchWithLowLatency}");
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
                this.LiveStatusType.Select(x => x == StatusType.OnAir || x == StatusType.ComingSoon)
                )
                .Select(x => x.All(y => y))
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler);

            RefreshCommand = CanRefresh
                .ToReactiveCommand(scheduler: _scheduler)
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

            TogglePlayPauseCommand = new ReactiveCommand(_scheduler)
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


            CanSeek = Observable.CombineLatest(
                IsWatchWithTimeshift,
                ObservableMediaPlayer.CurrentState.Select(x => x == MediaPlaybackState.Paused || x == MediaPlaybackState.Playing))
                .Select(x => x.All(y => y))
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
        }



        public IScheduler _scheduler { get; }

        public PlayerSettings PlayerSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
        public NiconicoSession NiconicoSession { get; }
        public UserProvider UserProvider { get; }
        public CommunityProvider CommunityProvider { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public DialogService _HohoemaDialogService { get; }
        public PageManager PageManager { get; }
        public ExternalAccessService ExternalAccessService { get; }

        private NotificationService _NotificationService;


        public MediaPlayer MediaPlayer { get; private set; }
        public ObservableMediaPlayer ObservableMediaPlayer { get; }
        public WindowService WindowService { get; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public TogglePlayerDisplayViewCommand TogglePlayerDisplayViewCommand { get; }
        public ShowPrimaryViewCommand ShowPrimaryViewCommand { get; }
        public MediaPlayerSoundVolumeManager SoundVolumeManager { get; }

        private string _LiveId;
        public string LiveId
        {
            get { return _LiveId; }
            set { SetProperty(ref _LiveId, value); }
        }

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

        private ObservableCollection<Comment> _DisplayingLiveComments { get; } = new ObservableCollection<Comment>();
        public ReadOnlyObservableCollection<Comment> DisplayingLiveComments { get; private set; }


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
		
		public ReactiveProperty<uint> CommentCount { get; private set; }
		public ReactiveProperty<uint> WatchCount { get; private set; }

        public ReactiveProperty<LivePlayerType?> LivePlayerType { get; private set; }
        public ReactiveProperty<StatusType?> LiveStatusType { get; private set; }

        public ReadOnlyReactiveProperty<bool> CanRefresh { get; private set; }

        public ReactiveProperty<bool> CanChangeQuality { get; private set; }
        public ReactiveProperty<string> RequestQuality { get; private set; }

        public ReactiveProperty<string> CurrentQuality { get; private set; }

        public ReactiveProperty<bool> IsLowLatency { get; }

        public ObservableCollection<string> LiveAvailableQualities { get; } = new ObservableCollection<string>();

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
		public CommentCommandEditerViewModel CommandEditerVM { get; private set; }
		public ReactiveProperty<string> CommandString { get; private set; }

		public AsyncReactiveCommand<string> CommentSubmitCommand { get; private set; }

        public Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView FilterdComments { get; } = new Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView();

        // suggestion
        public ReactiveProperty<LiveSuggestion> Suggestion { get; private set; }
		public ReactiveProperty<bool> HasSuggestion { get; private set; }


        // Side Pane Content


        public ReactiveCommand RefreshCommand { get; private set; }

        public ReactiveCommand TogglePlayPauseCommand { get; private set; }


        public MediaPlayerSeekCommand SeekCommand { get; }
        public MediaPlayerSetPlaybackRateCommand SetPlaybackRateCommand { get; }
        public MediaPlayerToggleMuteCommand ToggleMuteCommand { get; }
        public MediaPlayerVolumeUpCommand VolumeUpCommand { get; }
        public MediaPlayerVolumeDownCommand VolumeDownCommand { get; }


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
                        PageManager.OpenPageWithId(HohoemaPageType.Community, CommunityId);
                    }
                    ));
            }
        }





        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            LiveId = parameters.GetValue<string>("id");

            if (parameters.TryGetValue<string>("title", out var title))
            {
                LiveTitle = title;
            }

            if (LiveId != null)
            {
                NicoLiveVideo = new NicoLiveVideo(
                    LiveId,
                    MediaPlayer,
                    NiconicoSession,
                    NicoLiveProvider,
                    LoginUserLiveReservationProvider,
                    PlayerSettings,
                    AppearanceSettings,
                    _scheduler,
                    CommunityId
                    );

                NicoLiveVideo.LiveComments.ObserveAddChanged()
                    .Subscribe(comment =>
                    {
                        if (!comment.IsAnonimity && TryResolveUserId(comment.UserId, out var owner))
                        {
                            comment.UserName = owner.ScreenName;
                            comment.IconUrl = owner.IconUrl;
                        }
                        else
                        {
                            UserIdToComments.AddOrUpdate(comment.UserId
                                , (key) => new List<LiveComment>() { comment }
                                , (key, list) =>
                                {
                                    list.Add(comment);
                                    return list;
                                }
                            );
                        }

                        try
                        {
                            comment.IsLoginUserComment = !comment.IsAnonimity ? NiconicoSession.IsLoginUserId(comment.UserId) : false;
                        }
                        catch { }

                        _scheduler.Schedule(() =>
                        {
                            _DisplayingLiveComments.Add(comment);
                        });
                    }
                )
                .AddTo(_NavigatingCompositeDisposable);

                FilterdComments.Source = NicoLiveVideo.LiveComments;

                CommentCount = NicoLiveVideo.ObserveProperty(x => x.CommentCount)
                    .ToReactiveProperty(_scheduler)
                    .AddTo(_NavigatingCompositeDisposable);
                RaisePropertyChanged(nameof(CommentCount));

                WatchCount = NicoLiveVideo.ObserveProperty(x => x.WatchCount)
                    .ToReactiveProperty(_scheduler)
                    .AddTo(_NavigatingCompositeDisposable);
                RaisePropertyChanged(nameof(WatchCount));

                CommunityId = NicoLiveVideo.BroadcasterCommunityId;

                // post comment 
                CommentSubmitCommand = Observable.CombineLatest(
                    NicoLiveVideo.ObserveProperty(x => x.CanPostComment)
                    )
                    .Select(x => x.All(y => y))
                    .ToAsyncReactiveCommand<string>()
                    .AddTo(_CompositeDisposable);

                CommentSubmitCommand.Subscribe(async x =>
                {
                    if (string.IsNullOrWhiteSpace(x)) { return; }

                    if (NicoLiveVideo != null)
                    {
                        await NicoLiveVideo.PostComment(x, CommandString.Value, LiveElapsedTime);
                    }
                })
                .AddTo(_CompositeDisposable);
                RaisePropertyChanged(nameof(CommentSubmitCommand));
                NicoLiveVideo.PostCommentResult += NicoLiveVideo_PostCommentResult;

                LiveStatusType.Value = NicoLiveVideo.LiveStatus;

                NicoLiveVideo.OpenLive += NicoLiveVideo_OpenLive;
                NicoLiveVideo.CloseLive += NicoLiveVideo_CloseLive;
                NicoLiveVideo.FailedOpenLive += NicoLiveVideo_FailedOpenLive1;
                NicoLiveVideo.OperationCommandRecieved += NicoLiveVideo_OperationCommandRecieved;
                NicoLiveVideo.LiveElapsed += NicoLiveVideo_LiveElapsed;

                // 生放送ではラウドネス正規化は対応してない
                SoundVolumeManager.LoudnessCorrectionValue = 1.0;
            }



            // NavigatedToAsync
            try
            {
                var liveInfo = await NiconicoSession.Context.Live.GetProgramInfoAsync(LiveId);
                if (liveInfo != null && liveInfo.IsOK)
                {
                    LiveTitle = liveInfo.Data.Title;

                    _OpenAt = liveInfo.Data.BeginAt;
                    _StartAt = liveInfo.Data.VposBaseAt;
                    _EndAt = liveInfo.Data.EndAt;
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

                    FilterdComments.Source = NicoLiveVideo?.LiveComments;
                }

                RoomName = NicoLiveVideo.RoomName;
            }
            catch
            {
                NicoLiveVideo?.Dispose();
                NicoLiveVideo = null;

                throw;
            }

        }


        private void NicoLiveVideo_LiveElapsed(object sender, TimeSpan e)
        {
            _scheduler.Schedule(() => 
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


        public override void OnNavigatedFrom(INavigationParameters parameters)
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

            CancelUserInfoResolvingTask();

            _PrevPrevSidePaneContentType = CurrentSidePaneContentType.Value;
            CurrentSidePaneContentType.Value = null;


            base.OnNavigatedFrom(parameters);
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
                await NicoLiveVideo.StartLiveWatchingSessionAsync();

                IsWatchWithTimeshift.Value = NicoLiveVideo.IsWatchWithTimeshift;

                if (!IsWatchWithTimeshift.Value)
                {
                    WatchStartLiveElapsedTime.Value = (DateTimeOffset.Now.ToOffset(NicoLiveVideo.JapanTimeZoneOffset) - _OpenAt);

//                    ResetSuggestion();
                }
                else
                {
                    WatchStartLiveElapsedTime.Value = NicoLiveVideo.TimeshiftPosition.Value;
                }

                //Debug.WriteLine(this.NicoLiveVideo.LiveInfo.VideoInfo.Video.TsArchiveStartTime);
                //Debug.WriteLine(this.NicoLiveVideo.LiveInfo.VideoInfo.Video.TsArchiveEndTime);

                _MaxSeekablePosition.Value = (_EndAt - _OpenAt).TotalSeconds;
            }
            catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
            }
            finally
			{
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
				if (NicoLiveVideo != null)
				{
					_StartAt = NicoLiveVideo.StartTime;
					_EndAt = NicoLiveVideo.EndTime;
				}
				else
				{
				}

                LiveStatusType.Value = NicoLiveVideo.LiveStatus;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.ToString());
			}
			finally
			{
			}

			ResetSuggestion(liveStatus);

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


		// コメント投稿の結果を受け取る
		private void NicoLiveVideo_PostCommentResult(NicoLiveVideo sender, bool postSuccess)
		{
            if (postSuccess)
            {
                // TODO: ポスト成功を通知
            }
		}




        #endregion


        #region NicoLiveVideo Event Handling

        private void NicoLiveVideo_OperationCommandRecieved(object sender, OperationCommandRecievedEventArgs e)
        {
            LiveOperationCommand operationCommand = null;
            var vpos = TimeSpan.FromMilliseconds(e.Comment.VideoPosition * 10);
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

                    LiveStatusType.Value = StatusType.Closed;
                    ResetSuggestion(Models.Live.LiveStatusType.Closed);
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

            LiveStatusType.Value = NicoLiveVideo.LiveStatus;

            if (NicoLiveVideo.LiveStatus == Mntone.Nico2.Live.StatusType.OnAir ||
                    NicoLiveVideo.LiveStatus == Mntone.Nico2.Live.StatusType.ComingSoon ||
                    NicoLiveVideo.IsWatchWithTimeshift
                    )
            {
                LivePlayerType.Value = NicoLiveVideo.LivePlayerType;
                
                RaisePropertyChanged(nameof(MediaPlayer));

                CommunityId = NicoLiveVideo.BroadcasterCommunityId;


                RaisePropertyChanged(nameof(NicoLiveVideo));

                if (CommunityName == null && CommunityId != null)
                {
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

                LiveAvailableQualities.Clear();
                CanChangeQuality.Value = false;
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
                            Debug.WriteLine(String.Join('|', types));

                            if (!LiveAvailableQualities.Any())
                            {
                                foreach (var type in types)
                                {
                                    LiveAvailableQualities.Add(type);
                                }
                            }

                            ChangeQualityCommand.RaiseCanExecuteChanged();
                        });

                    // コメントのユーザー名解決
                    _ = CommentUserInfoResolvingAsync().ConfigureAwait(false);
                }
                else
                {
                    // seet
                    if (NicoLiveVideo != null)
                    {
                        RoomName = NicoLiveVideo.RoomName;
                        //SeetId = NicoLiveVideo.
                    }
                }
            }
            else
            {

                Debug.WriteLine("生放送情報の取得失敗しました " + LiveId);

                ResetSuggestion(await NicoLiveVideo.GetLiveViewingFailedReason());
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
                        sidePaneContent = App.Current.Container.GetContainer().Resolve<LiveCommentSidePaneContentViewModel>(new ParameterOverride("comments", FilterdComments));
                        break;
                    case PlayerSidePaneContentType.Setting:
                        sidePaneContent = App.Current.Container.Resolve<SettingsSidePaneContentViewModel>();
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
        private ConcurrentDictionary<string, List<LiveComment>> UserIdToComments = new ConcurrentDictionary<string, List<LiveComment>>();

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
                    _scheduler.Schedule(() =>
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

#nullable enable
using AngleSharp.Html.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services.Player;
using Hohoema.Helpers;
using Hohoema.Models.Application;
using Hohoema.Models.Live;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Live.LoginUser;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Player;
using Hohoema.Models.Player.Comment;
using Hohoema.Services;
using Hohoema.Services.Player;
using Hohoema.Services.Player.Videos;
using Hohoema.ViewModels.Navigation.Commands;
using Hohoema.ViewModels.Niconico.Share;
using Hohoema.ViewModels.Player.Commands;
using Hohoema.ViewModels.Player.PlayerSidePaneContent;
using Hohoema.Views.Player;
using I18NPortable;
using Microsoft.Toolkit.Uwp;
using NiconicoToolkit.Live;
using NiconicoToolkit.Live.Timeshift;
using NiconicoToolkit.Live.WatchPageProp;
using NiconicoToolkit.Live.WatchSession;
using NiconicoToolkit.Live.WatchSession.Events;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;

namespace Hohoema.ViewModels.Player;

public class LiveOperationCommand
{
    public VerticalAlignment? DisplayPosition;
    public string Header { get; set; }
    public string Content { get; set; }
    public Uri Hyperlink { get; set; }
    public Color Color { get; set; }
    private RelayCommand _OpenHyperlinkCommand;
    public RelayCommand OpenHyperlinkCommand
    {
        get
        {
            return _OpenHyperlinkCommand
                ?? (_OpenHyperlinkCommand = new RelayCommand(async () => 
                {
                    var result = await Launcher.LaunchUriAsync(Hyperlink);
                }));
        }
    }
}

public class LiveContent : ILiveContent
{
    public string ProviderId { get; set; }

    public string ProviderName { get; set; }

    public ProviderType ProviderType { get; set; }

    public LiveId LiveId { get; set; }

    public string Title { get; set; }
}



public class LivePlayerPageViewModel : HohoemaPageViewModelBase
	{
    public ReadOnlyReactivePropertySlim<PlayerDisplayView> CurrentPlayerDisplayView { get; }
    public IScheduler _scheduler { get; }
    public IPlayerView PlayerView { get; }
    public PlayerSettings PlayerSettings { get; }
    public AppearanceSettings AppearanceSettings { get; }
    public NicoLiveProvider NicoLiveProvider { get; }
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    public LoginUserLiveReservationProvider LoginUserLiveReservationProvider { get; }
    public NiconicoSession NiconicoSession { get; }
    public UserProvider UserProvider { get; }
    public CommunityProvider CommunityProvider { get; }
    public DialogService _HohoemaDialogService { get; }

    private readonly IMessenger _messenger;
    private readonly UserNameProvider _userNameRepository;
    private NotificationService _NotificationService;
    private readonly CommentFilteringFacade _commentFiltering;
    
    public MediaPlayer MediaPlayer { get; private set; }
    public ObservableMediaPlayer ObservableMediaPlayer { get; }
    public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
    public TogglePlayerDisplayViewCommand TogglePlayerDisplayViewCommand { get; }
    public ShowPrimaryViewCommand ShowPrimaryViewCommand { get; }
    public MediaPlayerSoundVolumeManager SoundVolumeManager { get; }
    public CommentCommandEditerViewModel CommentCommandEditerViewModel { get; }

    public OpenLinkCommand OpenLinkCommand { get; }
    public CopyToClipboardCommand CopyToClipboardCommand { get; }
    public CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }
    public OpenShareUICommand OpenShareUICommand { get; }
    public OpenPageCommand OpenPageCommand { get; }
    public OpenContentOwnerPageCommand OpenContentOwnerPageCommand { get; }

    private LiveContent _liveInfo;
    public LiveContent LiveInfo
    {
        get { return _liveInfo; }
        private set { SetProperty(ref _liveInfo, value); }
    }



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


    private NiconicoToolkit.NiconicoContext LiveContext => NiconicoSession.ToolkitContext;

    private LiveWatchPageDataProp _PlayerProp;
    private Live2WatchSession _watchSession;
    private WatchSessionTimer _watchSessionTimer;



    private ObservableCollection<LiveComment> _DisplayingLiveComments { get; } = new ObservableCollection<LiveComment>();
    public ReadOnlyObservableCollection<LiveComment> DisplayingLiveComments { get; private set; }

    private ObservableCollection<LiveComment> _ListLiveComments { get; } = new ObservableCollection<LiveComment>();


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

    private TimeSpan _LiveElapsedTimeFromOpen;
    public TimeSpan LiveElapsedTimeFromOpen
    {
        get { return _LiveElapsedTimeFromOpen; }
        private set { SetProperty(ref _LiveElapsedTimeFromOpen, value); }
    }

    private int _CommentCount;
    public int CommentCount
    {
        get => _CommentCount;
        set => SetProperty(ref _CommentCount, value);
    }

    private int _WatchCount;
    public int WatchCount
    {
        get => _WatchCount;
        set => SetProperty(ref _WatchCount, value);
    }

    private int _AdPoint;
    public int AdPoint
    {
        get => _AdPoint;
        set => SetProperty(ref _AdPoint, value);
    }

    private int _GiftPoint;
    public int GiftPoint
    {
        get => _GiftPoint;
        set => SetProperty(ref _GiftPoint, value);
    }

    DateTimeOffset? _OpenTime;
    public DateTimeOffset OpenTime => _OpenTime ??= DateTimeOffset.FromUnixTimeSeconds(_PlayerProp.Program.OpenTime);
    DateTimeOffset? _StartTime;
    public DateTimeOffset StartTime => _StartTime ??= DateTimeOffset.FromUnixTimeSeconds(_PlayerProp.Program.BeginTime);
    DateTimeOffset? _EndTime;
    public DateTimeOffset EndTime => _EndTime ??= DateTimeOffset.FromUnixTimeSeconds(_PlayerProp.Program.EndTime);        

    private bool _IsTimeshift;
    public bool IsTimeshift
    {
        get => _IsTimeshift;
        private set => SetProperty(ref _IsTimeshift, value);
    }

    // play
    public ObservableCollection<LiveQualityType> LiveAvailableQualities { get; } = new ObservableCollection<LiveQualityType>();
    public ObservableCollection<LiveQualityLimitType> LiveAvailableLimitQualities { get; } = new ObservableCollection<LiveQualityLimitType>();


    public ReactiveProperty<LiveStatus?> LiveStatusType { get; private set; }

    public ReactivePropertySlim<bool> NowRefreshing { get; private set; }

    public ReactiveProperty<bool> CanChangeQuality { get; private set; }
    public ReactiveProperty<LiveQualityType> RequestQuality { get; private set; }
    public ReactiveProperty<LiveQualityType> CurrentQuality { get; private set; }

    public ReactiveProperty<LiveQualityLimitType> QualityLimit { get; private set; }

    public ReactiveProperty<bool> IsLowLatency { get; }

    public ReactiveCommand<TimeSpan?> SeekVideoCommand { get; private set; }

    public IReadOnlyReactiveProperty<bool> CanSeek { get; }
    public ReactiveProperty<double> SeekBarTimeshiftPosition { get; }
    private ReactiveProperty<double> _MaxSeekablePosition { get; }
    public IReadOnlyReactiveProperty<double> MaxSeekablePosition => _MaxSeekablePosition;
    private bool _NowSeekBarPositionChanging = false;

    private DispatcherQueue _dispatcherQueue;
    private DispatcherQueueTimer _updateTimer;

    // comment

    public ReactiveProperty<bool> IsCommentDisplayEnable { get; private set; }
    public ReactiveProperty<TimeSpan> RequestCommentDisplayDuration { get; private set; }
    public ReactiveProperty<double> CommentFontScale { get; private set; }

    public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
    public ReactiveProperty<Color> CommentDefaultColor { get; private set; }

    public ReadOnlyReactiveProperty<double> CommentOpacity { get; private set; }



    // post comment
    ISubject<Unit> _SuccessPostCommentSubject { get; }
    public IReadOnlyReactiveProperty<Unit> SuccessPostCommentSubject { get; }
    public CommentCommandEditerViewModel CommandEditerVM { get; private set; }
    public ReactiveProperty<string> CommentSubmitText { get; private set; }
    public ReactiveProperty<bool> NowCommentSubmitting { get; private set; }


    public AsyncReactiveCommand CommentSubmitCommand { get; private set; }

    public Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView FilterdComments { get; } = new Microsoft.Toolkit.Uwp.UI.AdvancedCollectionView();

    // suggestion
    public ReactiveProperty<LiveSuggestion> Suggestion { get; private set; }
    public ReactiveProperty<bool> HasSuggestion { get; private set; }


    // Side Pane Content
    public AsyncReactiveCommand TogglePlayPauseCommand { get; private set; }


    public MediaPlayerSeekCommand SeekCommand { get; }
    public MediaPlayerSetPlaybackRateCommand SetPlaybackRateCommand { get; }
    public MediaPlayerToggleMuteCommand ToggleMuteCommand { get; }
    public MediaPlayerVolumeUpCommand VolumeUpCommand { get; }
    public MediaPlayerVolumeDownCommand VolumeDownCommand { get; }


    private RelayCommand _ShareCommand;
    public RelayCommand ShareCommand
    {
        get
        {
            return _ShareCommand
                ?? (_ShareCommand = new RelayCommand(() =>
                {
                    ShareHelper.Share(_PlayerProp.Program.Title, ShareHelper.MakeLiveShareText(LiveId));
                }
                ));
        }
    }

    private RelayCommand _ShareWithClipboardCommand;
    public RelayCommand ShareWithClipboardCommand
    {
        get
        {
            return _ShareWithClipboardCommand
                ?? (_ShareWithClipboardCommand = new RelayCommand(() =>
                {
                    ClipboardHelper.CopyToClipboard(ShareHelper.MakeLiveShareTextWithTitle(_PlayerProp.Program.Title, LiveId));
                }
                ));
        }
    }



    private RelayCommand _OpenBroadcastCommunityCommand;
    public RelayCommand OpenBroadcastCommunityCommand
    {
        get
        {
            return _OpenBroadcastCommunityCommand
                ?? (_OpenBroadcastCommunityCommand = new RelayCommand(() =>
                {
                    _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.Community, CommunityId);
                }
                ));
        }
    }

    private TimeSpan _WatchStartLiveElapsedTime;
    public TimeSpan WatchStartLiveElapsedTime
    {
        get { return _WatchStartLiveElapsedTime; }
        private set { SetProperty(ref _WatchStartLiveElapsedTime, value); }
    }



    public LivePlayerPageViewModel(
        IMessenger messenger,
        IScheduler scheduler,
        IPlayerView playerView,
        PlayerSettings playerSettings,
        AppearanceSettings appearanceSettings,
        NicoLiveProvider nicoLiveProvider,
        ApplicationLayoutManager applicationLayoutManager,
        LoginUserLiveReservationProvider loginUserLiveReservationProvider,
        NiconicoSession niconicoSession,
        UserProvider userProvider,
        UserNameProvider userNameRepository,
        CommunityProvider communityProvider,
        Services.DialogService dialogService,
        NotificationService notificationService,
        MediaPlayer mediaPlayer,
        ObservableMediaPlayer observableMediaPlayer,
        PrimaryViewPlayerManager primaryViewPlayerManager,
        TogglePlayerDisplayViewCommand togglePlayerDisplayViewCommand,
        ShowPrimaryViewCommand showPrimaryViewCommand,
        MediaPlayerSoundVolumeManager soundVolumeManager,
        CommentCommandEditerViewModel commentCommandEditerViewModel,
        CommentFilteringFacade commentFiltering,
        OpenLinkCommand openLinkCommand,
        CopyToClipboardCommand copyToClipboardCommand,
        CopyToClipboardWithShareTextCommand copyToClipboardWithShareTextCommand,
        OpenShareUICommand openShareUICommand,
        OpenPageCommand openPageCommand,
        OpenContentOwnerPageCommand openContentOwnerPageCommand
        )
    {
        CurrentPlayerDisplayView = appearanceSettings
            .ObserveProperty(x => x.PlayerDisplayView)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_CompositeDisposable);
        _messenger = messenger;
        _scheduler = scheduler;
        PlayerView = playerView;
        PlayerSettings = playerSettings;
        AppearanceSettings = appearanceSettings;
        NicoLiveProvider = nicoLiveProvider;
        ApplicationLayoutManager = applicationLayoutManager;
        LoginUserLiveReservationProvider = loginUserLiveReservationProvider;
        NiconicoSession = niconicoSession;
        UserProvider = userProvider;
        _userNameRepository = userNameRepository;
        CommunityProvider = communityProvider;

        _HohoemaDialogService = dialogService;
        _NotificationService = notificationService;
        MediaPlayer = mediaPlayer;
        ObservableMediaPlayer = observableMediaPlayer;
        PrimaryViewPlayerManager = primaryViewPlayerManager;
        TogglePlayerDisplayViewCommand = togglePlayerDisplayViewCommand;
        ShowPrimaryViewCommand = showPrimaryViewCommand;
        SoundVolumeManager = soundVolumeManager;
        CommentCommandEditerViewModel = commentCommandEditerViewModel;
        _commentFiltering = commentFiltering;
        OpenLinkCommand = openLinkCommand;
        CopyToClipboardCommand = copyToClipboardCommand;
        CopyToClipboardWithShareTextCommand = copyToClipboardWithShareTextCommand;
        OpenShareUICommand = openShareUICommand;
        OpenPageCommand = openPageCommand;
        OpenContentOwnerPageCommand = openContentOwnerPageCommand;
        DisplayingLiveComments = new ReadOnlyObservableCollection<LiveComment>(_DisplayingLiveComments);

        SeekCommand = new MediaPlayerSeekCommand(MediaPlayer);
        SetPlaybackRateCommand = new MediaPlayerSetPlaybackRateCommand(MediaPlayer, PlayerSettings);
        ToggleMuteCommand = new MediaPlayerToggleMuteCommand(MediaPlayer);
        VolumeUpCommand = new MediaPlayerVolumeUpCommand(SoundVolumeManager);
        VolumeDownCommand = new MediaPlayerVolumeDownCommand(SoundVolumeManager);
        
        LiveStatusType = new ReactiveProperty<LiveStatus?>(_scheduler);

        CanChangeQuality = new ReactiveProperty<bool>(_scheduler, false);
        RequestQuality = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.DefaultLiveQuality, _scheduler)
            .AddTo(_CompositeDisposable);
        CurrentQuality = new ReactiveProperty<LiveQualityType>(_scheduler, mode: ReactivePropertyMode.DistinctUntilChanged)
            .AddTo(_CompositeDisposable);

        IsLowLatency = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.LiveWatchWithLowLatency, _scheduler, mode: ReactivePropertyMode.DistinctUntilChanged)
            .AddTo(_CompositeDisposable);

        QualityLimit = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.LiveQualityLimit, _scheduler)
            .AddTo(_CompositeDisposable);



        IsCommentDisplayEnable = PlayerSettings
            .ToReactivePropertyAsSynchronized(x => x.IsCommentDisplay_Live, _scheduler)
            .AddTo(_CompositeDisposable);

        CommentCanvasHeight = new ReactiveProperty<double>(_scheduler, 0.0).AddTo(_CompositeDisposable);
        CommentDefaultColor = new ReactiveProperty<Color>(_scheduler, Colors.White).AddTo(_CompositeDisposable);

        CommentOpacity = PlayerSettings.ObserveProperty(x => x.CommentOpacity)
            .ToReadOnlyReactiveProperty(eventScheduler: _scheduler);

        FilterdComments.Source = _ListLiveComments;
        FilterdComments.SortDescriptions.Add(new Microsoft.Toolkit.Uwp.UI.SortDescription(nameof(IComment.VideoPosition), Microsoft.Toolkit.Uwp.UI.SortDirection.Ascending));
        FilterdComments.Filter = (x) => !_commentFiltering.IsHiddenComment(x as IComment);

        NowCommentSubmitting = new ReactiveProperty<bool>(_scheduler, false)
            .AddTo(_CompositeDisposable);
        CommentSubmitText = new ReactiveProperty<string>(_scheduler, null)
            .AddTo(_CompositeDisposable);
        CommentSubmitCommand = CommentSubmitText.Select(x => string.IsNullOrWhiteSpace(x) is false)
            .ToAsyncReactiveCommand()
            .AddTo(_CompositeDisposable);


        SeekVideoCommand = this.ObserveProperty(x => x.IsTimeshift).ToReactiveCommand<TimeSpan?>(scheduler: _scheduler)
                .AddTo(_CompositeDisposable);

        SeekVideoCommand.Subscribe(time =>
        {
            if (!time.HasValue) { return; }

            SeekBarTimeshiftPosition.Value += time.Value.TotalSeconds;
        })
        .AddTo(_CompositeDisposable);

        SeekBarTimeshiftPosition = new ReactiveProperty<double>(_scheduler, 0.0, mode: ReactivePropertyMode.DistinctUntilChanged)
            .AddTo(_CompositeDisposable);
        _MaxSeekablePosition = new ReactiveProperty<double>(_scheduler, 0.0)
            .AddTo(_CompositeDisposable);

        bool _seelBarChangingFirst = false;
        bool _lastPlaybackPaused = false;
        SeekBarTimeshiftPosition
            .Where(_ => !_NowSeekBarPositionChanging)
            .Do(_ =>
            {
                if (_seelBarChangingFirst)
                {
                    _seelBarChangingFirst = false;
                    _lastPlaybackPaused = MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Paused;
                    MediaPlayer.Pause();
                    _updateTimer.Stop();
                }
            })
            .Throttle(TimeSpan.FromSeconds(0.25))
            .Subscribe(x =>
            {
                _dispatcherQueue.TryEnqueue(async () =>
                {
                    var time = TimeSpan.FromSeconds(x);

                    var session = MediaPlayer.PlaybackSession;                        
                    MediaPlayer.Source = null;
                    _MediaSource?.Dispose();
                    _AdaptiveMediaSource?.Dispose();

                    _watchSessionTimer.PlaybackHeadPosition = time;

                    // TODO: 生放送のシーク処理
                    // タイムシフト視聴時はスタート時間に自動シーク
                    string hlsUri = _hlsUri;
                    hlsUri = LiveClient.MakeSeekedHLSUri(hlsUri, _watchSessionTimer.PlaybackHeadPosition);
#if DEBUG
                    Debug.WriteLine(hlsUri);
#endif

                    // https://platform.uno/docs/articles/implemented/windows-ui-xaml-controls-mediaplayerelement.html
                    // MediaPlayer is supported on UWP/Android/iOS.
                    // not support to MacOS and WASM.
#if WINDOWS_UWP                        
                    var amsCreateResult = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(hlsUri), LiveContext.HttpClient);
                    if (amsCreateResult.Status == AdaptiveMediaSourceCreationStatus.Success)
                    {
                        var ams = amsCreateResult.MediaSource;
                        _MediaSource = MediaSource.CreateFromAdaptiveMediaSource(ams);
                        _AdaptiveMediaSource = ams;
                        MediaPlayer.Source = _MediaSource;
                        _AdaptiveMediaSource.DesiredMaxBitrate = _AdaptiveMediaSource.AvailableBitrates.Max();
                    }

                    
                    _updateTimer.Start();

#elif __IOS__ || __ANDROID__
            var mediaSource = MediaSource.CreateFromUri(new Uri(hlsUri));
            _MediaSource = mediaSource;
            MediaPlayer.Source = mediaSource;
#else
            throw new NotSupportedException();
#endif

                    _DisplayingLiveComments.Clear();
                    _ListLiveComments.Clear();
                    _CommentSession.Seek(NavigationCancellationToken, time);

                    // Note: MediaPlayer.PositionはSourceを再設定するたびに0にリセットされる
                    var elapsedTimeResult = _watchSessionTimer.UpdatePlaybackTime(TimeSpan.Zero);
                    LiveElapsedTime = elapsedTimeResult.LiveElapsedTime;
                    LiveElapsedTimeFromOpen = elapsedTimeResult.LiveElapsedTimeFromOpen;

                    WatchStartLiveElapsedTime = LiveElapsedTime;

                    _seelBarChangingFirst = true;

                    if (_lastPlaybackPaused)
                    {
                        async void MediaPlayer_MediaPlaybackSession(MediaPlaybackSession sender, object args)
                        {
                            if (sender.PlaybackState == MediaPlaybackState.Playing)
                            {
                                sender.PlaybackStateChanged -= MediaPlayer_MediaPlaybackSession;

                                var ct = NavigationCancellationToken;
                                while (sender.PlaybackState != MediaPlaybackState.Paused)
                                {
                                    await Task.Delay(100, ct);
                                    sender.MediaPlayer.Pause();
                                }
                            }
                        }

                        MediaPlayer.PlaybackSession.PlaybackStateChanged += MediaPlayer_MediaPlaybackSession;
                        await Task.Delay(3000);
                        MediaPlayer.PlaybackSession.PlaybackStateChanged -= MediaPlayer_MediaPlaybackSession;
                    }
                });
            })
            .AddTo(_CompositeDisposable);

        // post comment
        _SuccessPostCommentSubject = new BehaviorSubject<Unit>(Unit.Default);
        SuccessPostCommentSubject = _SuccessPostCommentSubject
            .ToReadOnlyReactiveProperty(mode: ReactivePropertyMode.None, eventScheduler: _scheduler)
            .AddTo(_CompositeDisposable);

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
            PlayerSettings.ObserveProperty(x => x.LiveWatchWithLowLatency, isPushCurrentValueAtFirst: false).ToUnit(),
            PlayerSettings.ObserveProperty(x => x.DefaultLiveQuality, isPushCurrentValueAtFirst: false).ToUnit(),
            PlayerSettings.ObserveProperty(x => x.LiveQualityLimit, isPushCurrentValueAtFirst: false).ToUnit()
            )
            .Where(_ => CanChangeQuality.Value)
            .Subscribe(async x =>
            {
                if (_watchSession == null) { return; }

                var limit = GetQualityLimitType();
                Debug.WriteLine($"Change Quality: {PlayerSettings.DefaultLiveQuality} - Low Latency: {PlayerSettings.LiveWatchWithLowLatency} - Limit: {limit}");
                _updateTimer.Stop();
                await _watchSession.ChangeStreamAsync(PlayerSettings.DefaultLiveQuality, PlayerSettings.LiveWatchWithLowLatency, limit);
            })
            .AddTo(_CompositeDisposable);

        // Noneの時
        // OnAirかCommingSoonの時

        NowRefreshing = new ReactivePropertySlim<bool>();

        TogglePlayPauseCommand = new AsyncReactiveCommand()
            .AddTo(_CompositeDisposable);

        TogglePlayPauseCommand.Subscribe(async _ =>
        {
            NowRefreshing.Value = true;
            try
            {
                if (IsTimeshift)
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
                    if (MediaPlayer.Source == null || MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.None)
                    {
                        if (await TryUpdateLiveStatus())
                        {
                            var limit = GetQualityLimitType();
                            Debug.WriteLine($"Change Quality: {PlayerSettings.DefaultLiveQuality} - Low Latency: {PlayerSettings.LiveWatchWithLowLatency} - Limit: {limit}");

                            await _watchSession.ChangeStreamAsync(PlayerSettings.DefaultLiveQuality, PlayerSettings.LiveWatchWithLowLatency, limit);
                            // MediaPlayer.PositionはSourceを再設定するたびに0にリセットされる
                            // ソース更新後のコメント表示再生位置のズレを補正する
                        }
                    }
                    else
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
                }
            }
            finally
            {
                NowRefreshing.Value = false;
            }
        })
        .AddTo(_CompositeDisposable);


        CanSeek = Observable.CombineLatest(
            this.ObserveProperty(x => x.IsTimeshift),
            ObservableMediaPlayer.CurrentState.Select(x => x == MediaPlaybackState.Paused || x == MediaPlaybackState.Playing))
            .Select(x => x.All(y => y))
            .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
            .AddTo(_CompositeDisposable);

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _updateTimer = _dispatcherQueue.CreateTimer();
        _updateTimer.Interval = TimeSpan.FromSeconds(0.1);
        _updateTimer.IsRepeating = true;
        _updateTimer.Stop();
        _updateTimer.Tick += (s, _) =>
        {
            var elaplsedUpdateResult = _watchSessionTimer.UpdatePlaybackTime(MediaPlayer.PlaybackSession.Position);
            LiveElapsedTime = elaplsedUpdateResult.LiveElapsedTime;
            LiveElapsedTimeFromOpen = elaplsedUpdateResult.LiveElapsedTimeFromOpen;

            if (IsTimeshift)
            {
                _NowSeekBarPositionChanging = true;
                SeekBarTimeshiftPosition.Value = LiveElapsedTime.TotalSeconds;
                _NowSeekBarPositionChanging = false;
            }

            // 表示完了したコメントの削除
            /*
            for (int i = _DisplayingLiveComments.Count - 1; i >= 0; i--)
            {
                var c = _DisplayingLiveComments[i];
                var cPos = c.VideoPosition;
                if (LiveElapsedTimeFromOpen > cPos + PlayerSettings.CommentDisplayDuration + TimeSpan.FromSeconds(10))
                {
                    Debug.WriteLine("remove comment : " + _DisplayingLiveComments[i].CommentText);
                    _DisplayingLiveComments.RemoveAt(i);
                }
            }
            */

            if (UnresolvedUserId.TryPop(out var id))
            {                    
                _ = _dispatcherQueue.EnqueueAsync(async () =>
                {
                    var owner = await _userNameRepository.ResolveUserNameAsync(id);
                    if (owner != null)
                    {
                        UpdateCommentUserName(id, owner);
                    }
                });
            }
        };
    }

    

    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        _updateTimer.Stop();

        MediaPlayer.Pause();
        MediaPlayer.Source = null;

        _MediaSource?.Dispose();
        _MediaSource = null;
        _AdaptiveMediaSource?.Dispose();
        _AdaptiveMediaSource = null;

        if (_watchSession != null)
        {
            _watchSession.RecieveStream -= _watchSession_RecieveStream;
            _watchSession.RecieveRoom -= _watchSession_RecieveRoom;
            _watchSession.RecieveSchedule -= _watchSession_RecieveSchedule;
            _watchSession.RecieveStatistics -= _watchSession_RecieveStatistics;
            _watchSession.ReceiveDisconnect -= _watchSession_ReceiveDisconnect;
            _watchSession.RecieveReconnect -= _watchSession_RecieveReconnect;

            _watchSession.Dispose();
            _watchSession = null;
        }


        CloseCommentSession();

        _PrevPrevSidePaneContentType = CurrentSidePaneContentType.Value;
        CurrentSidePaneContentType.Value = null;

        _DisplayingLiveComments.Clear();
        _ListLiveComments.Clear();

        CanChangeQuality.Value = true;
        var requestQuality = RequestQuality.Value;
        LiveAvailableQualities.Clear();
        RequestQuality.Value = requestQuality;
        CanChangeQuality.Value = false;

        base.OnNavigatedFrom(parameters);
    }


    private async Task RefreshTimeshiftProgram()
    {            
        if (NiconicoSession.IsLoggedIn)
        {
            try
            {
                var timeshiftDetailsRes = await LoginUserLiveReservationProvider.GetReservtionsAsync();

                if (timeshiftDetailsRes.Reservations.Items.FirstOrDefault(x => x.ProgramId == LiveId) is { } reservation)
                {
                    _Reservation = reservation;
                }
            }
            catch
            {
                _Reservation = null;
            }
        }
        else
        {
            _Reservation = null;
        }
    }

    public static readonly TimeSpan JapanTimeZoneOffset = +TimeSpan.FromHours(9);

    private async Task ResolveTimeshiftTicketAsync()
    {
        await RefreshTimeshiftProgram();

        // チケットを使用するかダイアログで表示する
        // タイムシフトでの視聴かつタイムシフトの視聴予約済みかつ視聴権が未取得の場合は
        // 視聴権の使用を確認する
        var now = DateTime.Now;
        if (_Reservation != null
            && _Reservation.IsActive
            && _Reservation.TimeshiftSetting.EndTime < now
            )
        {
            var dialog = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IDialogService>();

            // 視聴権に関する詳細な情報提示

            // 視聴権の利用期限は 24H＋放送時間 まで
            // ただし公開期限がそれより先に来る場合には公開期限が視聴期限となる
            var outdatedTime = DateTimeOffset.Now.ToOffset(JapanTimeZoneOffset) + (EndTime - StartTime) + TimeSpan.FromHours(24);
            string desc = string.Empty;
            if (_Reservation.TimeshiftTicket.ExpireTime is { } expireTime && outdatedTime > expireTime)
            {
                outdatedTime = expireTime.LocalDateTime;
                desc = "Dialog_ConfirmTimeshiftWatch_WatchLimitByDate".Translate(expireTime.ToString("g"));
            }
            else
            {
                desc = "Dialog_ConfirmTimeshiftWatch_WatchLimitByDuration".Translate(outdatedTime.ToString("g"));
            }

            var result = await dialog.ShowMessageDialog(
                content: desc,
                title: _Reservation.Program.Title, acceptButtonText: "WatchLiveStreaming".Translate(), cancelButtonText: "Cancel".Translate()
                );

            if (result)
            {
                await Task.Delay(500);

                await LoginUserLiveReservationProvider.UseReservationAsync(_Reservation.ProgramId);

                await Task.Delay(3000);

                // タイムシフト予約一覧を更新
                // 視聴権利用を開始したアイテムがFIRST_WATCH以外の視聴可能を示すステータスになっているはず
                await RefreshTimeshiftProgram();

                Debug.WriteLine($"TSSettings {nameof(_Reservation.TimeshiftSetting.StatusText)}: {_Reservation.TimeshiftSetting.StatusText}");
                Debug.WriteLine($"TSSettings {nameof(_Reservation.TimeshiftSetting.RequirementText)}: {_Reservation.TimeshiftSetting.RequirementText}");
                Debug.WriteLine($"TSSettings {nameof(_Reservation.TimeshiftSetting.WatchLimitText)}: {_Reservation.TimeshiftSetting.WatchLimitText}");

                // 視聴情報を更新
                _PlayerProp = await LiveContext.Live.GetLiveWatchPageDataPropAsync(LiveId);

                Debug.WriteLine("視聴可能回数 : " + _PlayerProp.ProgramTimeshift.WatchLimit);
                Debug.WriteLine("Reservation.ExpireTime : " + DateTimeOffset.FromUnixTimeSeconds(_PlayerProp.ProgramTimeshift.Reservation.ExpireTime));
                Debug.WriteLine("Publication.ExpireTime : " + DateTimeOffset.FromUnixTimeSeconds(_PlayerProp.ProgramTimeshift.Publication.ExpireTime ?? 0));
            }
        }

        // TSしていない場合でもプレミアム会員の場合は後からTS可能


    }


    public override async Task OnNavigatedToAsync(INavigationParameters parameters)
    {
        NowRefreshing.Value = true;
        try
        {
            if (parameters.TryGetValue<string>("id", out var idString))
            {
                LiveId = idString;
            }
            else if (parameters.TryGetValue<LiveId>("id", out LiveId id))
            {
                LiveId = id;
            } 

            if (parameters.TryGetValue<string>("title", out var title))
            {
                LiveTitle = title;
            }

            if (LiveId != null)
            {
                _PlayerProp = await LiveContext.Live.GetLiveWatchPageDataPropAsync(LiveId);


                await ResolveTimeshiftTicketAsync();


                LiveInfo = new LiveContent()
                {
                    Title = _PlayerProp.Program.Title,
                    LiveId = LiveId,
                };

                if (_PlayerProp.UserProgramWatch.RejectedReasons.Any())
                {
                    Suggestion.Value = new LiveSuggestion(_PlayerProp.UserProgramWatch.RejectedReasons.First());
                    return;
                }

                _OpenTime = null;
                _StartTime = null;
                _EndTime = null;

                _watchSession = NiconicoToolkit.Live.LiveClient.CreateWatchSession(_PlayerProp, LiveContext.UserAgent);

                _watchSession.RecieveStream += _watchSession_RecieveStream;
                _watchSession.RecieveRoom += _watchSession_RecieveRoom;
                _watchSession.RecieveSchedule += _watchSession_RecieveSchedule;
                _watchSession.RecieveStatistics += _watchSession_RecieveStatistics;
                _watchSession.ReceiveDisconnect += _watchSession_ReceiveDisconnect;
                _watchSession.RecieveReconnect += _watchSession_RecieveReconnect;

                _watchSessionTimer = !_watchSession.IsWatchWithTimeshift
                    ? WatchSessionTimer.CreateForLiveStreaming(_PlayerProp.Program, chasePlay: false)
                    : WatchSessionTimer.CreateForTimeshift(_PlayerProp.Program)
                    ;

                IsTimeshift = _watchSession.IsWatchWithTimeshift;
                LiveTitle = _PlayerProp.Program.Title;
                _MaxSeekablePosition.Value = (EndTime - OpenTime).TotalSeconds;


                var elapsedTimeResult = _watchSessionTimer.UpdatePlaybackTime(TimeSpan.Zero);
                LiveElapsedTime = elapsedTimeResult.LiveElapsedTime;
                LiveElapsedTimeFromOpen = elapsedTimeResult.LiveElapsedTimeFromOpen;


                _updateTimer.Start();

                var limit = GetQualityLimitType();
                Debug.WriteLine($"Start Watch: Quality: {PlayerSettings.DefaultLiveQuality} - Low Latency: {PlayerSettings.LiveWatchWithLowLatency} - Limit: {limit}");

                await _watchSession.StartWachingAsync(PlayerSettings.DefaultLiveQuality, PlayerSettings.LiveWatchWithLowLatency, limit);

                await TryUpdateLiveStatus();

                CommunityId = _PlayerProp.SocialGroup.Id;

                CommunityName = _PlayerProp.SocialGroup.Name;

                // post comment 

                CommentSubmitCommand.Subscribe(async () =>
                {
                    var text = CommentSubmitText.Value;
                    if (string.IsNullOrWhiteSpace(text)) { return; }
                    CommentSubmitText.Value = string.Empty;

                    NowCommentSubmitting.Value = true;

                    try
                    {
                        var result = await _watchSession.PostCommentAsync(
                            text,
                            LiveElapsedTime + TimeSpan.FromSeconds(1),
                            this.CommentCommandEditerViewModel.IsAnonymousCommenting.Value,
                            this.CommentCommandEditerViewModel.SelectedCommentSize.Value?.ToString().ToLower(),
                            this.CommentCommandEditerViewModel.SelectedAlingment.Value?.ToString().ToLower(),
                            this.CommentCommandEditerViewModel.SelectedColor.Value?.ToString().ToLower()
                            );

                        CommentSubmitText.Value = string.Empty;
                    }
                    finally
                    {
                        NowCommentSubmitting.Value = false;
                    }
                })
                .AddTo(_navigationDisposables);

                // 生放送ではラウドネス正規化は対応してない
                SoundVolumeManager.LoudnessCorrectionValue = 1.0;
            }
        }
        catch
        {
            _updateTimer.Stop();
            if (_watchSession != null)
            {
                await _watchSession.CloseAsync();
            }
            throw;
        }
        finally
        {
            NowRefreshing.Value = false;
            await base.OnNavigatedToAsync(parameters);
        }
    }


    LiveQualityLimitType? GetQualityLimitType()
    {
        LiveQualityLimitType? limit = null;
        if (PlayerSettings.DefaultLiveQuality == LiveQualityType.Abr)
        {
            if (_PlayerProp.Program.Stream.MaxQuality < PlayerSettings.LiveQualityLimit)
            {
                limit = _PlayerProp.Program.Stream.MaxQuality;
            }
            else
            {
                limit = PlayerSettings.LiveQualityLimit;

                if (limit == LiveQualityLimitType.SuperLow)
                {
                    limit = LiveQualityLimitType.Low;
                }
            }
        }

        return limit;
    }


    MediaSource _MediaSource;
    AdaptiveMediaSource _AdaptiveMediaSource;
    string _hlsUri;

    private void _watchSession_RecieveStream(Live2CurrentStreamEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(async () =>
        {
            CanChangeQuality.Value = false;

            Debug.WriteLine(e.Quality);

            // タイムシフト視聴時はスタート時間に自動シーク
            string hlsUri = e.Uri;
            if (_watchSession.IsWatchWithTimeshift)
            {
                _hlsUri = e.Uri;
                _watchSessionTimer.PlaybackHeadPosition = StartTime - OpenTime;
                hlsUri = LiveClient.MakeSeekedHLSUri(e.Uri, _watchSessionTimer.PlaybackHeadPosition);
#if DEBUG
                Debug.WriteLine(hlsUri);
#endif
            }

            // https://platform.uno/docs/articles/implemented/windows-ui-xaml-controls-mediaplayerelement.html
            // MediaPlayer is supported on UWP/Android/iOS.
            // not support to MacOS and WASM.
#if WINDOWS_UWP
            var amsCreateResult = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(hlsUri), LiveContext.HttpClient);
            if (amsCreateResult.Status == AdaptiveMediaSourceCreationStatus.Success)
            {
                var ams = amsCreateResult.MediaSource;
                _MediaSource = MediaSource.CreateFromAdaptiveMediaSource(ams);
                _AdaptiveMediaSource = ams;
                MediaPlayer.Source = _MediaSource;
                MediaPlayer.Play();
                _AdaptiveMediaSource.DesiredMaxBitrate = _AdaptiveMediaSource.AvailableBitrates.Max();
            }

#elif __IOS__ || __ANDROID__
            var mediaSource = MediaSource.CreateFromUri(new Uri(hlsUri));
            _MediaSource = mediaSource;
            MediaPlayer.Source = mediaSource;
#else
            throw new NotSupportedException();
#endif

            // Note: MediaPlayer.PositionはSourceを再設定するたびに0にリセットされる
            var elapsedTimeResult = _watchSessionTimer.UpdatePlaybackTime(TimeSpan.Zero);
            LiveElapsedTime = elapsedTimeResult.LiveElapsedTime;
            LiveElapsedTimeFromOpen = elapsedTimeResult.LiveElapsedTimeFromOpen;

            WatchStartLiveElapsedTime = IsTimeshift ? LiveElapsedTime : LiveElapsedTimeFromOpen;


            var requestQuality = RequestQuality.Value;
            LiveAvailableQualities.Clear();
            foreach (var item in e.AvailableQualities.Where(x => x.Equals(nameof(LiveQualityType.Abr), StringComparison.InvariantCultureIgnoreCase) is false))
            {
                if (Enum.TryParse<LiveQualityType>(item, true, out var quality))
                {
                    LiveAvailableQualities.Add(quality);
                }
            }
            
            RequestQuality.Value = requestQuality;
            CurrentQuality.Value = Enum.TryParse <LiveQualityType>(e.Quality, true, out var qualityType) ? qualityType : LiveQualityType.Abr;

            RequestQuality.ForceNotify();

            if (CurrentQuality.Value == LiveQualityType.Abr)
            {
                var limit = QualityLimit.Value;
                LiveAvailableLimitQualities.Clear();
                foreach (var item in LiveAvailableQualities.Where(x => x is LiveQualityType.SuperLow or LiveQualityType.Low or LiveQualityType.Normal or LiveQualityType.High or LiveQualityType.SuperHigh).Select(x => x switch
                {
                    LiveQualityType.SuperLow => LiveQualityLimitType.SuperLow,
                    LiveQualityType.Low => LiveQualityLimitType.Low,
                    LiveQualityType.Normal => LiveQualityLimitType.Normal,
                    LiveQualityType.High => LiveQualityLimitType.High,
                    LiveQualityType.SuperHigh => LiveQualityLimitType.SuperHigh,
                    _ => throw new NotImplementedException(),
                }))
                {
                    LiveAvailableLimitQualities.Add(item);
                }

                QualityLimit.Value = GetQualityLimitType() ?? PlayerSettings.LiveQualityLimit;
                QualityLimit.ForceNotify();
            }

            CanChangeQuality.Value = true;
            
            _updateTimer.Start();
        });
    }

    void CloseCommentSession()
    {
        if (_CommentSession != null)
        {
            var cs = _CommentSession;
            _CommentSession = null;
            cs.CommentReceived -= _CommentSession_CommentReceived;
            cs.CommentPosted -= _CommentSession_CommentPosted;
            cs.Connected -= _CommentSession_Connected;
            cs.Disconnected -= _CommentSession_Disconnected;
            cs.Dispose();
        }
    }

    LiveCommentSession _CommentSession;
    private void _watchSession_RecieveRoom(Live2CurrentRoomEventArgs e)
    {
        var ct = NavigationCancellationToken;
        _dispatcherQueue.TryEnqueue(async () =>
        {
            CloseCommentSession();

            _ListLiveComments.Clear();
            _DisplayingLiveComments.Clear();

            if (!IsTimeshift)
            {
                _CommentSession = e.CreateCommentClientForLiveStreaming(NiconicoSession.HohoemaUserAgent, _PlayerProp.User.Id.ToString());
            }
            else
            {
                _CommentSession = e.CreateCommentClientForTimeshift(NiconicoSession.HohoemaUserAgent, _PlayerProp.User.Id.ToString(), OpenTime, EndTime);
            }

            _CommentSession.CommentReceived += _CommentSession_CommentReceived;
            _CommentSession.CommentPosted += _CommentSession_CommentPosted;
            _CommentSession.Connected += _CommentSession_Connected;
            _CommentSession.Disconnected += _CommentSession_Disconnected;

            if (_CommentSession != null)
            {                    
                await _CommentSession.OpenAsync(ct, IsTimeshift ? _watchSessionTimer.PlaybackHeadPosition : TimeSpan.Zero);
            }
        });
    }

    private void _CommentSession_CommentReceived(object sender, CommentReceivedEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            AddComment(e.Chat);
        });
    }

    private void AddComment(LiveChatData comment)
    {
        LiveComment ChatToComment(LiveChatData x)
        {
            var comment = new LiveComment();

            comment.VideoPosition = x.VideoPosition;

            comment.CommentText = x.Content;
            comment.CommentId = (uint)x.CommentId;
            comment.IsAnonymity = x.IsAnonymity;
            comment.UserId = x.UserId;
            comment.IsOwnerComment = x.UserId == _PlayerProp.Program.Supplier?.ProgramProviderId?.ToString();
            comment.IsOperationCommand = x.IsOperater && x.HasOperatorCommand;
            if (comment.IsOperationCommand)
            {
                comment.OperatorCommandType = x.OperatorCommandType;
                comment.OperatorCommandParameters = x.OperatorCommandParameters;
            }
            return comment;
        }

        LiveComment commentVM = ChatToComment(comment);


        if (comment.IsOperater && comment.Content.StartsWith('/'))
        {
            Debug.WriteLine($"Operator command: {comment.Content}");
        }
        else
        {
            //               Debug.WriteLine($"comment: {comment.Content}");

            // 表示範囲にある場合は頭から流れるように
            if (!IsTimeshift)
            {
                commentVM.VideoPosition = DateTimeOffset.FromUnixTimeSeconds(comment.Date) + TimeSpan.FromMilliseconds(comment.DateUsec / 1000) - OpenTime + TimeSpan.FromSeconds(1);
            }

            if (!commentVM.IsAnonymity)
            {
                var commentUserId = uint.Parse(comment.UserId);
                if (_userNameRepository.TryResolveUserNameFromCache(commentUserId, out var userName))
                {
                    commentVM.UserName = userName;
                }
                else
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        UserIdToComments.AddOrUpdate(commentUserId
                            , (key) => new List<LiveComment>() { commentVM }
                            , (key, list) =>
                            {
                                list.Add(commentVM);
                                return list;
                            }
                        );
                        if (UnresolvedUserId.Contains(commentUserId) is false)
                        {
                            UnresolvedUserId.Push(commentUserId);
                        }
                    });
                }
            }

            if (IsTimeshift)
            {
                _DisplayingLiveComments.Add(commentVM);
                _ListLiveComments.Add(commentVM);
            }
            else
            {
                if (!_commentFiltering.IsHiddenCommentOwnerUserId(comment.UserId)
                || (LiveElapsedTime - comment.VideoPosition).Duration() < TimeSpan.FromSeconds(3)
                )
                {
                    _DisplayingLiveComments.Add(commentVM);
                }
                else
                {
                    Debug.WriteLine("表示スキップ：" + commentVM.CommentText);
                }

                _ListLiveComments.Add(commentVM);
            }
        }
    }

    private void _CommentSession_CommentPosted(object sender, CommentPostedEventArgs e)
    {
        
    }

    private void _CommentSession_Connected(object sender, CommentServerConnectedEventArgs e)
    {
        
    }

    private void _CommentSession_Disconnected(object sender, CommentServerDisconnectedEventArgs e)
    {
    }


    private void _watchSession_RecieveSchedule(Live2ScheduleEventArgs e)
    {
        _watchSessionTimer.ScheduleUpdated(e.BeginTime, e.EndTime);
    }

    private void _watchSession_RecieveStatistics(Live2StatisticsEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            WatchCount = (int)e.ViewCount;
            CommentCount = (int)e.CommentCount;
            AdPoint = (int)e.AdPoints;
            GiftPoint = (int)e.GiftPoints;
        });
    }


    private void _watchSession_ReceiveDisconnect(Live2DisconnectEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            MediaPlayer.Pause();
            MediaPlayer.Source = null;
        });
    }

    private void _watchSession_RecieveReconnect(Live2ReconnectEventArgs e)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
        });
    }







    /// <summary>
    /// 生放送情報だけを更新し、配信ストリームの更新は行いません。
    /// </summary>
    /// <returns></returns>
    private async Task<bool> TryUpdateLiveStatus()
    {
        var casInfo = await LiveContext.Live.CasApi.GetLiveProgramAsync(LiveId);

        if (casInfo.Meta.Status == 200)
        {
            LiveStatusType.Value = casInfo.Data.LiveStatus;
        }

        _EndTime = casInfo.Data.OnAirTime.EndAt;
        _MaxSeekablePosition.Value = (EndTime - OpenTime).TotalSeconds;

        ResetSuggestion(LiveStatusType.Value);

        return LiveStatusType.Value != null;
    }
    /// <summary>
    /// 生放送終了後などに表示するユーザーアクションの候補を再設定します。
    /// </summary>
    /// <param name="liveStatus"></param>
    private void ResetSuggestion(LiveStatus? liveStatus)
		{
			if (liveStatus == null || liveStatus == LiveStatus.Onair || IsTimeshift)
			{
				Suggestion.Value = null;
			}
			else
			{
				LiveSuggestion suggestion = null;

				//suggestion = liveStatus.Value.Make(NicoLiveVideo, PageManager, NiconicoSession);

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


		//// コメント投稿の結果を受け取る
		//private void NicoLiveVideo_PostCommentResult(NicoLiveVideo sender, bool postSuccess)
		//{
//          if (postSuccess)
//          {
//              CommentSubmitText.Value = string.Empty;
//          }

//          NowCommentSubmitting.Value = false;
//      }




    #endregion


    #region NicoLiveVideo Event Handling


    //private void NicoLiveVideo_OperationCommandRecieved(object sender, OperationCommandRecievedEventArgs e)
    //{
    //    LiveOperationCommand operationCommand = null;
    //    var vpos = TimeSpan.FromMilliseconds(e.Comment.VideoPosition * 10);
    //    bool isDisplayComment = (this.LiveElapsedTime - vpos) < TimeSpan.FromSeconds(10);
    //    switch (e.CommandType)
    //    {
    //        case "perm":
    //            {
    //                var content = e.CommandParameter.ElementAtOrDefault(0)?.Trim('"');

    //                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
    //                document.LoadHtml(content);

    //                var node = document.DocumentNode;

    //                var comment = node.InnerText;



    //                string link = null;
    //                try
    //                {
    //                    link = node.Descendants("a")?.FirstOrDefault()?.Attributes["href"]?.Value;
    //                }
    //                catch { }


    //                operationCommand = new LiveOperationCommand()
    //                {
    //                    DisplayPosition = VerticalAlignment.Top,
    //                    Content = comment,
    //                };
    //                if (!string.IsNullOrEmpty(link))
    //                {
    //                    operationCommand.Hyperlink = new Uri(link);
    //                }


    //                BroadcasterLiveOperationCommand.Value = operationCommand;
    //            }
    //            break;
    //        case "press":
    //            {
    //                if (!isDisplayComment) { return; }

    //                var color = e.CommandParameter.ElementAtOrDefault(1);
    //                var comment = e.CommandParameter.ElementAtOrDefault(2)?.Trim('"');
    //                var screenName = e.CommandParameter.ElementAtOrDefault(3)?.Trim('"');
    //                if (!string.IsNullOrWhiteSpace(comment))
    //                {
    //                    // 表示
    //                    operationCommand = new LiveOperationCommand()
    //                    {
    //                        DisplayPosition = VerticalAlignment.Bottom,
    //                        Content = comment,
    //                        Header = screenName,
    //                    };
    //                }

    //                PressLiveOperationCommand.Value = operationCommand;
    //            }
    //            break;
    //        case "info":
    //            {
    //                if (!isDisplayComment) { return; }

    //                var type = int.Parse(e.CommandParameter.ElementAtOrDefault(0));
    //                var comment = e.CommandParameter.ElementAtOrDefault(1)?.Trim('"');

    //                operationCommand = new LiveOperationCommand()
    //                {
    //                    DisplayPosition = VerticalAlignment.Bottom,
    //                    Content = comment,
    //                };

    //                OperaterLiveOperationCommand.Value = operationCommand;
    //            }
    //            break;
    //        case "telop":
    //            {
    //                if (!isDisplayComment) { return; }

    //                // プレイヤー下部に情報表示
    //                var type = e.CommandParameter.ElementAtOrDefault(0);
    //                var comment = e.CommandParameter.ElementAtOrDefault(1)?.Trim('"');

    //                //on ニコ生クルーズ(リンク付き)/ニコニコ実況コメント
    //                //show クルーズが到着/実況に接続
    //                //show0 実際に流れているコメント
    //                //perm ニコ生クルーズが去って行きました＜改行＞(降りた人の名前、人数)
    //                //off (プレイヤー下部のテロップを消去)

    //                if (type == "off")
    //                {
    //                    OperaterLiveOperationCommand.Value = null;
    //                }
    //                else// if (type == "perm")
    //                {
    //                    operationCommand = new LiveOperationCommand()
    //                    {
    //                        DisplayPosition = VerticalAlignment.Bottom,
    //                        Content = comment,
    //                    };

    //                    OperaterLiveOperationCommand.Value = operationCommand;
    //                }
    //            }
    //            break;
    //        case "uadpoint":
    //            {
    //                if (!isDisplayComment) { return; }

    //                var liveId_wo_lv = e.CommandParameter.ElementAtOrDefault(0);
    //                var adpoint = e.CommandParameter.ElementAtOrDefault(1);

    //                /*
    //                operationCommand = new LiveOperationCommand()
    //                {
    //                    DisplayPosition = VerticalAlignment.Bottom,
    //                    Content = $"広告総ポイント {adpoint}pt",
    //                };

    //                OperaterLiveOperationCommand.Value = operationCommand;
    //                */
    //            }
    //            break;
    //        case "koukoku":
    //            {
    //                if (!isDisplayComment) { return; }

    //                var content = e.CommandParameter.ElementAtOrDefault(0)?.Trim('"');

    //                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
    //                document.LoadHtml(content);

    //                var node = document.DocumentNode;

    //                var comment = node.InnerText;
    //                string link = null;
    //                try
    //                {
    //                    link = node.Descendants("a")?.FirstOrDefault()?.Attributes["href"]?.Value;
    //                }
    //                catch { }

    //                operationCommand = new LiveOperationCommand()
    //                {
    //                    DisplayPosition = VerticalAlignment.Top,
    //                    Content = comment,
    //                };
    //                if (!string.IsNullOrEmpty(link))
    //                {
    //                    operationCommand.Hyperlink = new Uri(link);
    //                }

    //            }
    //            break;
    //        case "disconnect":
    //            if (TogglePlayPauseCommand.CanExecute())
    //            {
    //                TogglePlayPauseCommand.Execute();
    //            }

    //            LiveStatusType.Value = StatusType.Closed;
    //            ResetSuggestion(Models.Live.LiveStatusType.Closed);
    //            break;
    //        case "clear":
    //        case "cls":
    //            BroadcasterLiveOperationCommand.Value = null;
    //            break;
    //        default:
    //            Debug.WriteLine($"非対応な運コメ：{e.CommandType}");
    //            break;
    //    }

    //    if (operationCommand != null)
    //    {
    //        LiveOperationCommands.Add(operationCommand);
    //    }
    //}

    

    #endregion


    #region Side Pane Content


    private Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase> _SidePaneContentCache = new Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase>();

    public ReactiveProperty<PlayerSidePaneContentType?> CurrentSidePaneContentType { get; }
    public ReadOnlyReactiveProperty<SidePaneContentViewModelBase> CurrentSidePaneContent { get; }


    static PlayerSidePaneContentType? _PrevPrevSidePaneContentType;
    SidePaneContentViewModelBase _PrevSidePaneContent;

    private RelayCommand<object> _SelectSidePaneContentCommand;
    public RelayCommand<object> SelectSidePaneContentCommand
    {
        get
        {
            return _SelectSidePaneContentCommand
                ?? (_SelectSidePaneContentCommand = new RelayCommand<object>((type) =>
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
                    sidePaneContent = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<PlaylistSidePaneContentViewModel>();
                    break;
                case PlayerSidePaneContentType.Comment:
                    {
                        var commentContentVM = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<LiveCommentsSidePaneContentViewModel>();
                        commentContentVM.Comments = FilterdComments;
                        sidePaneContent = commentContentVM;
                    }                        
                    break;
                case PlayerSidePaneContentType.Setting:
                    sidePaneContent = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<SettingsSidePaneContentViewModel>();
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



    private RelayCommand _ToggleCommentListSidePaneContentCommand;
    public RelayCommand ToggleCommentListSidePaneContentCommand
    {
        get
        {
            return _ToggleCommentListSidePaneContentCommand
                ?? (_ToggleCommentListSidePaneContentCommand = new RelayCommand(async () =>
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


    #region CommentUserId Resolve

    private ConcurrentStack<uint> UnresolvedUserId = new ConcurrentStack<uint>();
    private ConcurrentDictionary<uint, List<LiveComment>> UserIdToComments = new ConcurrentDictionary<uint, List<LiveComment>>();
    private NiconicoToolkit.Live.Timeshift.Reservation _Reservation;

    private void UpdateCommentUserName(uint userId, string name)
    {
        if (UserIdToComments.TryGetValue(userId, out var comments))
        {
            foreach (var comment in comments)
            {
                comment.UserName = name;
            }
        }
    }

    #endregion
}


public sealed class LiveComment : ObservableObject, IComment
{

    // コメントのデータ構造だけで他のことを知っているべきじゃない
    // このデータを解釈して実際に表示するためのオブジェクトにする部分は処理は
    // View側が持つべき

    bool _isAppliedCommands;
    public void ApplyCommands()
    {
        /*
        if (_isAppliedCommands) { return; }

        foreach (var action in MailToCommandHelper.MakeCommandActions(Commands))
        {
            action(this);
        }

        _isAppliedCommands = true;
        */
    }

    public uint CommentId { get; set; }

    public string CommentText { get; set; }

    public IReadOnlyList<string> Commands { get; set; }

    public string UserId { get; set; }

    public bool IsAnonymity { get; set; }

    public TimeSpan VideoPosition { get; set; }

    public int NGScore { get; set; }

    public bool IsLoginUserComment { get; set; }

    public bool IsOwnerComment { get; set; }

    public int DeletedFlag { get; set; }


    private string _commentText_Transformed;
    public string CommentText_Transformed
    {
        get => _commentText_Transformed ?? CommentText;
        set => _commentText_Transformed = value;
    }


    public CommentDisplayMode DisplayMode { get; set; }

    public bool IsScrolling => DisplayMode == CommentDisplayMode.Scrolling;


    public CommentSizeMode SizeMode { get; set; }

    public bool IsInvisible { get; set; }


    public Color? Color { get; set; }

    private string _UserName;
    public string UserName
    {
        get { return _UserName; }
        set { SetProperty(ref _UserName, value); }
    }

    private string _IconUrl;
    public string IconUrl
    {
        get { return _IconUrl; }
        set { SetProperty(ref _IconUrl, value); }
    }

    public bool IsOperationCommand { get; internal set; }

    public string OperatorCommandType { get; set; }
    public string[] OperatorCommandParameters { get; set; }

    bool? _IsLink;
    public bool IsLink
    {
        get
        {
            ResetLink();

            return _IsLink.Value;
        }
    }

    Uri _Link;
    public Uri Link
    {
        get
        {
            ResetLink();

            return _Link;
        }
    }

    private void ResetLink()
    {
        if (!_IsLink.HasValue)
        {
            if (Uri.IsWellFormedUriString(CommentText, UriKind.Absolute))
            {
                _Link = new Uri(CommentText);
            }
            else
            {
                _Link = ParseLinkFromHtml(CommentText);
            }

            _IsLink = _Link != null;
        }
    }


    static Uri ParseLinkFromHtml(string text)
    {
        if (text == null) { return null; }

        HtmlParser htmlParser = new HtmlParser();
        using var document = htmlParser.ParseDocument(text);

        var anchorNode = document.QuerySelector("a");
        if (anchorNode != null)
        {
            if (anchorNode.GetAttribute("href") is not null and var href)
            {
                return new Uri(href);
            }
        }

        return null;
    }
}

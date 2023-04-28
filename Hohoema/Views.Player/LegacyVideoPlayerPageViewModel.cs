#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services.Player;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Player;
using Hohoema.Models.Player.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Niconico;
using Hohoema.Services.Player;
using Hohoema.Services.Player.Videos;
using Hohoema.ViewModels.Niconico.Likes;
using Hohoema.ViewModels.Niconico.Share;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.Player.Commands;
using Hohoema.ViewModels.Player.PlayerSidePaneContent;
using Hohoema.ViewModels.Player.Video;
using Hohoema.ViewModels.Subscriptions;
using Hohoema.Views.Player;
using I18NPortable;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Video;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using ZLogger;

namespace Hohoema.ViewModels.Player;


public class LegacyVideoPlayerPageViewModel : HohoemaPageViewModelBase
	{
    private readonly IMessenger _messenger;

    // TODO: HohoemaViewModelBaseとの依存性を排除（ViewModelBaseとの関係性は維持）
    private readonly IScheduler _scheduler;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;        

    public LegacyVideoPlayerPageViewModel(
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        IScheduler scheduler,
        IPlayerView playerView,
        NiconicoSession niconicoSession,
        SubscriptionManager subscriptionManager,
        NicoVideoProvider nicoVideoProvider,
        ChannelProvider channelProvider,
        MylistProvider mylistProvider,
        AppearanceSettings appearanceSettings,
        PlayerSettings playerSettings,
        VideoCacheSettings_Legacy cacheSettings,
        ApplicationLayoutManager applicationLayoutManager,
        LocalMylistManager localMylistManager,
        LoginUserOwnedMylistManager userMylistManager,
        QueuePlaylist queuePlaylist,
        HohoemaPlaylistPlayer hohoemaPlaylistPlayer,
        MediaPlayer mediaPlayer,
        VideoTogglePlayPauseCommand videoTogglePlayPauseCommand,
        NotificationService notificationService,
        DialogService dialogService,
        AddSubscriptionCommand addSubscriptionCommand,
        LocalPlaylistCreateCommand createLocalMylistCommand,
        MylistAddItemCommand addMylistCommand,
        LocalPlaylistAddItemCommand localPlaylistAddItemCommand,
        MylistCreateCommand createMylistCommand,
        VideoStreamingOriginOrchestrator videoStreamingOriginOrchestrator,
        VideoCommentPlayer commentPlayer,
        CommentCommandEditerViewModel commentCommandEditerViewModel,
        KeepActiveDisplayWhenPlaying keepActiveDisplayWhenPlaying,
        ObservableMediaPlayer observableMediaPlayer,
        VideoEndedRecommendation videoEndedRecommendation,
        PrimaryViewPlayerManager primaryViewPlayerManager,
        TogglePlayerDisplayViewCommand togglePlayerDisplayViewCommand,
        ShowPrimaryViewCommand showPrimaryViewCommand,
        MediaPlayerSoundVolumeManager soundVolumeManager,
        OpenLinkCommand openLinkCommand,
        CopyToClipboardCommand copyToClipboardCommand,
        ChangeVideoQualityCommand changeVideoQualityCommand,
        CopyToClipboardWithShareTextCommand copyToClipboardWithShareTextCommand,
        OpenShareUICommand openShareUICommand,
        PlaylistSidePaneContentViewModel playlistSidePaneContentViewModel,
        SettingsSidePaneContentViewModel settingsSidePaneContentViewModel,
        VideoCommentSidePaneContentViewModel videoCommentSidePaneContent,
        RelatedVideosSidePaneContentViewModel relatedVideosSidePaneContentViewModel
        )
    {
        _logger = loggerFactory.CreateLogger<VideoPlayerPageViewModel>();
        CurrentPlayerDisplayView = appearanceSettings
            .ObserveProperty(x => x.PlayerDisplayView)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_CompositeDisposable);
        _messenger = messenger;
        _scheduler = scheduler;
        PlayerView = playerView;
        NiconicoSession = niconicoSession;
        SubscriptionManager = subscriptionManager;
        NicoVideoProvider = nicoVideoProvider;
        ChannelProvider = channelProvider;
        MylistProvider = mylistProvider;
        PlayerSettings = playerSettings;
        CacheSettings = cacheSettings;
        ApplicationLayoutManager = applicationLayoutManager;
        LocalMylistManager = localMylistManager;
        UserMylistManager = userMylistManager;
        _queuePlaylist = queuePlaylist;
        _hohoemaPlaylistPlayer = hohoemaPlaylistPlayer;
        _NotificationService = notificationService;
        _HohoemaDialogService = dialogService;
        AddSubscriptionCommand = addSubscriptionCommand;
        CreateLocalMylistCommand = createLocalMylistCommand;
        AddMylistCommand = addMylistCommand;
        LocalPlaylistAddItemCommand = localPlaylistAddItemCommand;
        CreateMylistCommand = createMylistCommand;
        _videoStreamingOriginOrchestrator = videoStreamingOriginOrchestrator;
        CommentPlayer = commentPlayer;
        CommentCommandEditerViewModel = commentCommandEditerViewModel;
        PrimaryViewPlayerManager = primaryViewPlayerManager;
        TogglePlayerDisplayViewCommand = togglePlayerDisplayViewCommand;
        ShowPrimaryViewCommand = showPrimaryViewCommand;
        SoundVolumeManager = soundVolumeManager;
        OpenLinkCommand = openLinkCommand;
        CopyToClipboardCommand = copyToClipboardCommand;
        ChangeVideoQualityCommand = changeVideoQualityCommand;
        CopyToClipboardWithShareTextCommand = copyToClipboardWithShareTextCommand;
        OpenShareUICommand = openShareUICommand;
        _playlistSidePaneContentViewModel = playlistSidePaneContentViewModel;
        _settingsSidePaneContentViewModel = settingsSidePaneContentViewModel;
        _videoCommentSidePaneContentViewModel = videoCommentSidePaneContent;
        _relatedVideosSidePaneContentViewModel = relatedVideosSidePaneContentViewModel;
        ObservableMediaPlayer = observableMediaPlayer
            .AddTo(_CompositeDisposable);
        VideoEndedRecommendation = videoEndedRecommendation
            .AddTo(_CompositeDisposable);
        _keepActiveDisplayWhenPlaying = keepActiveDisplayWhenPlaying
            .AddTo(_CompositeDisposable);
        MediaPlayer = mediaPlayer;
        VideoTogglePlayPauseCommand = videoTogglePlayPauseCommand;
        SeekCommand = new MediaPlayerSeekCommand(MediaPlayer);
        SetPlaybackRateCommand = new MediaPlayerSetPlaybackRateCommand(MediaPlayer);
        ToggleMuteCommand = new MediaPlayerToggleMuteCommand(MediaPlayer);
        VolumeUpCommand = new MediaPlayerVolumeUpCommand(SoundVolumeManager);
        VolumeDownCommand = new MediaPlayerVolumeDownCommand(SoundVolumeManager);

        PlayNextCommand = _hohoemaPlaylistPlayer.GetCanGoNextOrPreviewObservable()
            .SelectMany(async x => await _hohoemaPlaylistPlayer.CanGoNextAsync())
            .ToAsyncReactiveCommand()
            .AddTo(_CompositeDisposable);

        PlayNextCommand.Subscribe(async () => await _hohoemaPlaylistPlayer.GoNextAsync(NavigationCancellationToken))
            .AddTo(_CompositeDisposable);

        PlayPreviousCommand = _hohoemaPlaylistPlayer.GetCanGoNextOrPreviewObservable()
            .SelectMany(async x => await _hohoemaPlaylistPlayer.CanGoPreviewAsync())
            .ToAsyncReactiveCommand()
            .AddTo(_CompositeDisposable);

        PlayPreviousCommand.Subscribe(async () => await _hohoemaPlaylistPlayer.GoPreviewAsync(NavigationCancellationToken))
            .AddTo(_CompositeDisposable);

        IsLoopingEnabled = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsCurrentVideoLoopingEnabled, raiseEventScheduler: scheduler)
            .AddTo(_CompositeDisposable);
        IsLoopingEnabled.Subscribe(x => mediaPlayer.IsLoopingEnabled = x)
            .AddTo(_CompositeDisposable);

        IsPlaylistShuffleRequeted = PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, _scheduler)
            .AddTo(_CompositeDisposable);

        IsAvailablePlaylistRepeatOrShuffle = _hohoemaPlaylistPlayer.ObserveProperty(x => x.IsShuffleAndRepeatAvailable)
            .ToReadOnlyReactiveProperty()
            .AddTo(_CompositeDisposable);

        PlayerSettings.ObserveProperty(x => x.PlaybackRate)
            .Subscribe(rate => MediaPlayer.PlaybackSession.PlaybackRate = rate)
            .AddTo(_CompositeDisposable);

    }

    private readonly ILogger<VideoPlayerPageViewModel> _logger;

    public ReadOnlyReactivePropertySlim<PlayerDisplayView> CurrentPlayerDisplayView { get; }

    public SubscriptionManager SubscriptionManager { get; }
    public NicoVideoProvider NicoVideoProvider { get; }
    public ChannelProvider ChannelProvider { get; }
    public MylistProvider MylistProvider { get; }

    public VideoCacheSettings_Legacy CacheSettings { get; }
    public ApplicationLayoutManager ApplicationLayoutManager { get; }
    
    public LocalMylistManager LocalMylistManager { get; }
    public LoginUserOwnedMylistManager UserMylistManager { get; }
    public SecondaryViewPlayerManager PlayerViewManager { get; }
    public AddSubscriptionCommand AddSubscriptionCommand { get; }
    public LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
    public MylistAddItemCommand AddMylistCommand { get; }
    public LocalPlaylistAddItemCommand LocalPlaylistAddItemCommand { get; }
    public MylistCreateCommand CreateMylistCommand { get; }


    public MediaPlayer MediaPlayer { get; }
    public VideoTogglePlayPauseCommand VideoTogglePlayPauseCommand { get; }
    public IPlayerView PlayerView { get; }
    public NiconicoSession NiconicoSession { get; }        
    public VideoCommentPlayer CommentPlayer { get; }
    public CommentCommandEditerViewModel CommentCommandEditerViewModel { get; }
    public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
    public TogglePlayerDisplayViewCommand TogglePlayerDisplayViewCommand { get; }
    public ShowPrimaryViewCommand ShowPrimaryViewCommand { get; }
    public MediaPlayerSoundVolumeManager SoundVolumeManager { get; }
    public OpenLinkCommand OpenLinkCommand { get; }
    public CopyToClipboardCommand CopyToClipboardCommand { get; }
    public ChangeVideoQualityCommand ChangeVideoQualityCommand { get; }
    public CopyToClipboardWithShareTextCommand CopyToClipboardWithShareTextCommand { get; }
    public OpenShareUICommand OpenShareUICommand { get; }
    public ObservableMediaPlayer ObservableMediaPlayer { get; }
    public VideoEndedRecommendation VideoEndedRecommendation { get; }
    public INicoVideoDetails VideoDetails { get; private set; }
    public PlayerSettings PlayerSettings { get; }

    public MediaPlayerSeekCommand SeekCommand { get; }
    public MediaPlayerSetPlaybackRateCommand SetPlaybackRateCommand { get; }
    public MediaPlayerToggleMuteCommand ToggleMuteCommand { get; }
    public MediaPlayerVolumeUpCommand VolumeUpCommand { get; }
    public MediaPlayerVolumeDownCommand VolumeDownCommand { get; }

    private string _VideoId;
    public string VideoId
    {
        get { return _VideoId; }
        set { SetProperty(ref _VideoId, value); }
    }



    public AsyncReactiveCommand PlayNextCommand { get; }
    public AsyncReactiveCommand PlayPreviousCommand { get; }



    private NicoVideoQuality _requestVideoQuality;

    private NicoVideoQualityEntity _currentQuality;
    public NicoVideoQualityEntity CurrentQuality
    {
        get { return _currentQuality; }
        private set { SetProperty(ref _currentQuality, value); }
    }


    private IReadOnlyCollection<NicoVideoQualityEntity> _avairableQualities;
    public IReadOnlyCollection<NicoVideoQualityEntity> AvailableQualities
    {
        get { return _avairableQualities; }
        set { SetProperty(ref _avairableQualities, value); }
    }

    public ReactiveProperty<bool> IsLoopingEnabled { get; }
    public ReactiveProperty<bool> IsPlaylistShuffleRequeted { get; }
    public IReadOnlyReactiveProperty<bool> IsAvailablePlaylistRepeatOrShuffle { get; }


    private bool nowPlayingWithCache;
    public bool NowPlayingWithCache
    {
        get { return nowPlayingWithCache; }
        set { SetProperty(ref nowPlayingWithCache, value); }
    }


    public IVideoContent VideoContent { get; private set; }


    public VideoSeriesViewModel VideoSeries { get; private set; }

    INotificationService _NotificationService;
    DialogService _HohoemaDialogService;

    private readonly VideoStreamingOriginOrchestrator _videoStreamingOriginOrchestrator;
    private readonly PlaylistSidePaneContentViewModel _playlistSidePaneContentViewModel;
    private readonly SettingsSidePaneContentViewModel _settingsSidePaneContentViewModel;
    private readonly VideoCommentSidePaneContentViewModel _videoCommentSidePaneContentViewModel;
    private readonly RelatedVideosSidePaneContentViewModel _relatedVideosSidePaneContentViewModel;
    private readonly KeepActiveDisplayWhenPlaying _keepActiveDisplayWhenPlaying;




    private NicoVideo _videoInfo;
    public NicoVideo VideoInfo
    {
        get => _videoInfo;
        set => SetProperty(ref _videoInfo, value);
    }

    private RelayCommand _OpenVideoInfoCommand;
    public RelayCommand OpenVideoInfoCommand
    {
        get
        {
            return _OpenVideoInfoCommand
                ?? (_OpenVideoInfoCommand = new RelayCommand(() =>
                {
                    _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.VideoInfomation, VideoId);
                }
                ));
        }
    }

    // ニコニコの「いいね」
    private VideoLikesContext _LikesContext = VideoLikesContext.Default;
    public VideoLikesContext LikesContext
    {
        get => _LikesContext;
        set => SetProperty(ref _LikesContext, value);
    }


    // 再生できない場合の補助

    private bool _IsNotSupportVideoType;
    public bool IsNotSupportVideoType
    {
        get { return _IsNotSupportVideoType; }
        set { SetProperty(ref _IsNotSupportVideoType, value); }
    }

    private string _CannotPlayReason;
    public string CannotPlayReason
    {
        get { return _CannotPlayReason; }
        set { SetProperty(ref _CannotPlayReason, value); }
    }




    public override void Dispose()
    {
        CommentPlayer?.Dispose();

        base.Dispose();
    }




    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);

        _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentQuality)
            .Subscribe(quality => _scheduler.Schedule(() => CurrentQuality = quality))
            .AddTo(_navigationDisposables);

        _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylistItem)
            .Subscribe(item =>
            {
                if (item == null) { return; }

                _scheduler.ScheduleAsync(async (s, ct) => 
                {
                    try
                    {
                        _relatedVideosSidePaneContentViewModel.Clear();
                        PlayerSplitViewIsPaneOpen = false;

                        if (item == null)
                        {
                            VideoInfo = null;
                            VideoId = null;
                            VideoSeries = null;
                            OnPropertyChanged(nameof(VideoContent));
                            OnPropertyChanged(nameof(VideoSeries));
                            VideoContent = null;
                            Title = string.Empty;
                            IsNotSupportVideoType = false;
                            LikesContext = VideoLikesContext.Default;
                            AvailableQualities = null;
                            NowPlayingWithCache = false;
                            return;
                        }
                    }
                    catch { }

                    try
                    {
                        CommentPlayer.ClearCurrentSession();

                        // 削除状態をチェック（再生準備より先に行う）
                        (NiconicoToolkit.ExtApi.Video.ThumbInfoResponse res, NicoVideo video) = await NicoVideoProvider.GetVideoInfoAsync(item.VideoId);
                        VideoInfo = video;
                        CheckDeleted(item.VideoId, res);

                        VideoId = VideoInfo.VideoId;

                        MediaPlayer.AutoPlay = true;

                        var result = _hohoemaPlaylistPlayer.CurrentPlayingSession;
                        if (!result.IsSuccess)
                        {
                            Title = VideoInfo.Title;
                            IsNotSupportVideoType = true;
                            CannotPlayReason = result.Exception?.Message ?? result.PlayingOrchestrateFailedReason.Translate();

                            return;
                        }

                        VideoDetails = result.VideoDetails;

                        _requestVideoQuality = PlayerSettings.DefaultVideoQuality;
                        AvailableQualities = _hohoemaPlaylistPlayer.AvailableQualities;
                        CurrentQuality = _hohoemaPlaylistPlayer.CurrentQuality;
                        NowPlayingWithCache = _hohoemaPlaylistPlayer.NowPlayingWithCache;

                        // コメントを更新
                        await CommentPlayer.UpdatePlayingCommentAsync(result.CommentSessionProvider);

                        VideoContent = VideoInfo;
                        OnPropertyChanged(nameof(VideoContent));

                        VideoEndedRecommendation.SetCurrentVideoSeries(VideoDetails);
                        
                        VideoSeries = VideoDetails.Series is not null and var series ? new VideoSeriesViewModel(series) : null;
                        OnPropertyChanged(nameof(VideoSeries));

                        // 好きの切り替え
                        if (NiconicoSession.IsLoggedIn)
                        {
                            LikesContext = new VideoLikesContext(VideoDetails, NiconicoSession.ToolkitContext.Likes, _NotificationService);
                        }
                        else
                        {
                            LikesContext = VideoLikesContext.Default;
                        }

                        if (PlayerSettings.IsShowCommentList_Video)
                        {
                            SetSidePaneViewModel(PlayerSidePaneContentType.Comment);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.ZLogErrorWithPayload(exception: ex, item.VideoId, "Video playing item display content update failed");
                    }
                });
            })
            .AddTo(_navigationDisposables);

        App.Current.Resuming += Current_Resuming;
        App.Current.Suspending += Current_Suspending;
    }

    

    private void CheckDeleted(VideoId videoId, NiconicoToolkit.ExtApi.Video.ThumbInfoResponse res)
    {
        try
        {
            // 動画が削除されていた場合
            if (res.IsOK is false)
            {
                _logger.ZLogInformation("Video deleted : {0}", VideoId);

                _scheduler.ScheduleAsync(async (scheduler, cancelToken) =>
                {
                    await Task.Delay(100);

                    string toastContent = "";
                    toastContent = "DeletedVideoNoticeWithTitle".Translate(videoId);

                    _NotificationService.ShowToast("DeletedVideoToastNotificationTitleWithVideoId".Translate(videoId), toastContent);
                });

                // ローカルプレイリストの場合は勝手に消しておく
                if (_hohoemaPlaylistPlayer.CurrentPlaylistId == QueuePlaylist.Id
                    && VideoInfo != null
                    )
                {
                    _queuePlaylist.Remove(VideoInfo.VideoId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogErrorWithPayload(exception: ex, videoId.ToString(), "Video deleted process failed");
            return;
        }
    }


    public override void OnNavigatedFrom(INavigationParameters parameters)
    {
        //MediaPlayer.Source = null;

        CommentPlayer.ClearCurrentSession();
        //_ = VideoPlayer.ClearCurrentSessionAsync();

        /*
        MediaPlayer.CommandManager.NextReceived -= CommandManager_NextReceived;
        MediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;
        */

        /*
        var smtc = SystemMediaTransportControls.GetForCurrentView();
        smtc.DisplayUpdater.ClearAll();
        smtc.DisplayUpdater.Update();
        */

        App.Current.Resuming -= Current_Resuming;
        App.Current.Suspending -= Current_Suspending;

        IsNotSupportVideoType = false;
        CannotPlayReason = null;

        _relatedVideosSidePaneContentViewModel.Clear();
        PlayerSplitViewIsPaneOpen = false;

        LikesContext = VideoLikesContext.Default;

        base.OnNavigatedFrom(parameters);
    }

    private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
    {
        var defferal = e.SuspendingOperation.GetDeferral();
        try
        {
            if (MediaPlayer.Source != null)
            {
                MediaPlayer.Pause();
                // サスペンド時にメモリ使用可能量が変更された場合に対応する
                MediaPlayer.Source = null;
            }
        }
        catch (Exception ex) 
        {
            _logger.ZLogError(ex, "Video player suspending failed");
        }
        finally
        {
            defferal.Complete();
        }            
    }

    private void Current_Resuming(object sender, object e)
    {
        try
        {
        }
        catch (Exception ex) 
        {
            _logger.ZLogError(ex, "Video player resuming failed");
        }
    }

    #region SidePaneContent

    private PlayerSidePaneContentType _SidePaneType;
    public PlayerSidePaneContentType SidePaneType
    {
        get => _SidePaneType;
        set => SetProperty(ref _SidePaneType, value);
    }

    private bool _PlayerSplitViewIsPaneOpen;
    public bool PlayerSplitViewIsPaneOpen
    {
        get => _PlayerSplitViewIsPaneOpen;
        set => SetProperty(ref _PlayerSplitViewIsPaneOpen, value);
    }

    private object _SidePaneViewModel;
    public object SidePaneViewModel
    {
        get => _SidePaneViewModel;
        set => SetProperty(ref _SidePaneViewModel, value);
    }


    private RelayCommand<string> _selectSidePaneCommand;
    public RelayCommand<string> SelectSidePaneCommand => _selectSidePaneCommand
        ?? (_selectSidePaneCommand = new RelayCommand<string>(str =>
        {
            if (Enum.TryParse<PlayerSidePaneContentType>(str, out var type))
            {
                SetSidePaneViewModel(type);
            }
        }));

    private void SetSidePaneViewModel(PlayerSidePaneContentType sidePaneType)
    {
        if (sidePaneType == SidePaneType || sidePaneType == PlayerSidePaneContentType.None)
        {
            SidePaneType = PlayerSidePaneContentType.None;
            SidePaneViewModel = EmptySidePaneContentViewModel.Default;
            PlayerSplitViewIsPaneOpen = false;
            PlayerSettings.IsShowCommentList_Video = false;
        }
        else
        {
            SidePaneType = sidePaneType;
            SidePaneViewModel = SidePaneType switch
            {
                PlayerSidePaneContentType.Playlist => _playlistSidePaneContentViewModel,
                PlayerSidePaneContentType.Comment => _videoCommentSidePaneContentViewModel,
                PlayerSidePaneContentType.Setting => _settingsSidePaneContentViewModel,
                PlayerSidePaneContentType.RelatedVideos => _relatedVideosSidePaneContentViewModel,
                _ => EmptySidePaneContentViewModel.Default,
            };

            if (SidePaneViewModel is RelatedVideosSidePaneContentViewModel vm)
            {
                _ = vm.InitializeRelatedVideos(VideoDetails);
            }

            PlayerSettings.IsShowCommentList_Video = SidePaneType is PlayerSidePaneContentType.Comment;
            PlayerSplitViewIsPaneOpen = true;
        }
    }

    #endregion
}

//public class VideoSeriesViewModel : ISeries
//{
//    private readonly WatchApiSeries _userSeries;

//    public VideoSeriesViewModel(WatchApiSeries userSeries)
//    {
//        _userSeries = userSeries;
//    }

//    public string Id => _userSeries.Id.ToString();

//    public string Title => _userSeries.Title;

//    public bool IsListed => throw new NotSupportedException();

//    public string Description => throw new NotSupportedException();

//    public string ThumbnailUrl => _userSeries.ThumbnailUrl.OriginalString;

//    public int ItemsCount => throw new NotSupportedException();

//    public OwnerType ProviderType => throw new NotSupportedException();

//    public string ProviderId => throw new NotSupportedException();
//}

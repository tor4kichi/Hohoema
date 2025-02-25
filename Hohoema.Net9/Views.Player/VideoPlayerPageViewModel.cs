﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services.Player;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
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
using Microsoft.Toolkit.Uwp;
using NiconicoToolkit.Video;
using NiconicoToolkit.Video.Watch;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.System;
using ZLogger;

namespace Hohoema.ViewModels.Player;


public partial class VideoPlayerPageViewModel : HohoemaPageViewModelBase
	{
    private readonly IMessenger _messenger;
    private readonly IScheduler _scheduler;
    private readonly QueuePlaylist _queuePlaylist;
    private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;        

    public VideoPlayerPageViewModel(
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
        INotificationService notificationService,
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
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _logger = loggerFactory.CreateLogger<VideoPlayerPageViewModel>();
        CurrentPlayerDisplayView = appearanceSettings
            .ObserveProperty(x => x.PlayerDisplayView)
            .ToReadOnlyReactivePropertySlim()
            .AddTo(_CompositeDisposable);

        LoudnessCorrectionValue = soundVolumeManager.ObserveProperty(x => x.LoudnessCorrectionValue)
            .ToReadOnlyReactiveProperty(eventScheduler: scheduler)
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
        SetPlaybackRateCommand = new MediaPlayerSetPlaybackRateCommand(MediaPlayer, PlayerSettings);
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

        _hohoemaPlaylistPlayer.GetCanGoNextOrPreviewObservable()
            .Throttle(TimeSpan.FromSeconds(0.5))
            .Where(x => NavigationCancellationToken.IsCancellationRequested is false && NavigationCancellationToken != default)
            .Subscribe(async _ =>
            {
                var prevVideo = await _hohoemaPlaylistPlayer.GetPreviewItemAsync(NavigationCancellationToken);
                var nextVideo = await _hohoemaPlaylistPlayer.GetNextItemAsync(NavigationCancellationToken);
                _scheduler.Schedule(() =>
                {
                    NextVideoContent = nextVideo;
                    PrevVideoContent = prevVideo;
                });
            })
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

    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ILogger<VideoPlayerPageViewModel> _logger;

    public ReadOnlyReactivePropertySlim<PlayerDisplayView> CurrentPlayerDisplayView { get; }

    public ReadOnlyReactiveProperty<double> LoudnessCorrectionValue { get; }

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



    private NicoVideoQualityEntity _requestVideoQuality;

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




    private IVideoContent _videoInfo;
    public IVideoContent VideoInfo
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


    [ObservableProperty]
    public partial IPlaylist CurrentPlaylist { get; set; }

    [ObservableProperty]
    public partial IVideoContent NextVideoContent { get; set; }

    [ObservableProperty]
    public partial IVideoContent PrevVideoContent { get; set; }

    public override void Dispose()
    {
        CommentPlayer?.Dispose();

        base.Dispose();
    }




    public override void OnNavigatedTo(INavigationParameters parameters)
    {
        base.OnNavigatedTo(parameters);

        _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentQuality)
            .Subscribe(quality => _dispatcherQueue.TryEnqueue(() => CurrentQuality = quality))
            .AddTo(_navigationDisposables);

        _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylist)
            .Subscribe(playlist => _dispatcherQueue.TryEnqueue(() => CurrentPlaylist = playlist))
            .AddTo(_navigationDisposables);

        _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylistItem)
            .Subscribe(item =>
            {
                if (item == null) { return; }

                _dispatcherQueue.TryEnqueue(() => 
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

                        VideoId = item.VideoId;

                        var result = _hohoemaPlaylistPlayer.CurrentPlayingSession;
                        if (!result.IsSuccess)
                        {
                            Title = VideoInfo?.Title ?? "?";
                            IsNotSupportVideoType = true;
                            CannotPlayReason = result.Exception?.Message ?? result.PlayingOrchestrateFailedReason.Translate();

                            return;
                        }

                        VideoInfo = VideoDetails = result.VideoDetails;

                        AvailableQualities = _hohoemaPlaylistPlayer.AvailableQualities;
                        _requestVideoQuality = AvailableQualities.FirstOrDefault(x => x.QualityId == PlayerSettings.DefaultVideoQualityId) ?? AvailableQualities.First();
                        CurrentQuality = _hohoemaPlaylistPlayer.CurrentQuality;
                        NowPlayingWithCache = _hohoemaPlaylistPlayer.NowPlayingWithCache;

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

                        MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                        MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                        MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                        MediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
                        async void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
                        {
                            MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                            MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
                            // コメントを更新
                            await _dispatcherQueue.EnqueueAsync(async () =>
                            {
                                await CommentPlayer.UpdatePlayingCommentAsync(result.CommentSessionProvider);

                                // コメント読み込み完了後に再生を開始したい
                                // このためにHohoemaPlaylistPlayerなどで再生開始しないように調整している
                                // 動画準備段階でコメント準備処理が走ると稀に映像が表示されないまま再生するケースがあった
                                MediaPlayer.Play();
                            });
                        }
                        void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
                        {
                            MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
                            MediaPlayer.MediaFailed -= MediaPlayer_MediaFailed;
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



    //private void CheckDeleted(VideoIdSearchSingleResponse res)
    //{
    //    try
    //    {
    //        // 動画が削除されていた場合
    //        if (res.Video.IsDeleted)
    //        {
    //            _logger.ZLogInformation("Video deleted : {0}", VideoId);

    //            _scheduler.ScheduleAsync(async (scheduler, cancelToken) =>
    //            {
    //                await Task.Delay(100);

    //                string toastContent = "";
    //                if (!String.IsNullOrEmpty(res.Video.Title))
    //                {
    //                    toastContent = "DeletedVideoNoticeWithTitle".Translate(res.Video.Title);
    //                }
    //                else
    //                {
    //                    toastContent = "DeletedVideoNotice".Translate();
    //                }

    //                _NotificationService.ShowToast("DeletedVideoToastNotificationTitleWithVideoId".Translate(res.Video.Id), toastContent);
    //            });

    //            // ローカルプレイリストの場合は勝手に消しておく
    //            if (_hohoemaPlaylistPlayer.CurrentPlaylistId == QueuePlaylist.Id
    //                && VideoInfo != null
    //                )
    //            {
    //                _queuePlaylist.Remove(VideoInfo.VideoId);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.ZLogErrorWithPayload(exception: ex, res.Video.Id, "Video deleted process failed");
    //        return;
    //    }
    //}


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

public class VideoSeriesViewModel : ISeries
{
    private readonly WatchApiSeries _userSeries;

    public VideoSeriesViewModel(WatchApiSeries userSeries)
    {
        _userSeries = userSeries;
    }

    public string Id => _userSeries.Id.ToString();

    public string Title => _userSeries.Title;

    public bool IsListed => throw new NotSupportedException();

    public string Description => throw new NotSupportedException();

    public string ThumbnailUrl => _userSeries.ThumbnailUrl.OriginalString;

    public int ItemsCount => throw new NotSupportedException();

    public OwnerType ProviderType => throw new NotSupportedException();

    public string ProviderId => throw new NotSupportedException();
}

using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Niconico.Player;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.Player.Commands;
using Hohoema.Presentation.ViewModels.Subscriptions;
using I18NPortable;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.System;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video.Series;
using Hohoema.Presentation.ViewModels.Niconico.Share;
using System.Collections.Generic;
using Hohoema.Presentation.Views.Player;
using NiconicoToolkit.Video.Watch;
using NiconicoToolkit.Video;
using NiconicoToolkit.SearchWithCeApi.Video;
using Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent;
using Hohoema.Presentation.ViewModels.Niconico.Likes;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.Niconico.Player.Comment;
using Hohoema.Models.UseCase.Hohoema.LocalMylist;
using Hohoema.Models.Domain.LocalMylist;
using Hohoema.Presentation.ViewModels.Player.Video;
using Reactive.Bindings;

namespace Hohoema.Presentation.ViewModels.Player
{

    public class VideoPlayerPageViewModel : HohoemaPageViewModelBase, INavigatedAwareAsync
	{
        // TODO: HohoemaViewModelBaseとの依存性を排除（ViewModelBaseとの関係性は維持）
        private readonly IScheduler _scheduler;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;

        public VideoPlayerPageViewModel(
            IScheduler scheduler,
            NiconicoSession niconicoSession,
            SubscriptionManager subscriptionManager,
            NicoVideoProvider nicoVideoProvider,
            ChannelProvider channelProvider,
            MylistProvider mylistProvider,
            PlayerSettings playerSettings,
            VideoCacheSettings_Legacy cacheSettings,
            ApplicationLayoutManager applicationLayoutManager,
            LocalMylistManager localMylistManager,
            LoginUserOwnedMylistManager userMylistManager,
            PageManager pageManager,
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
            VideoPlayer videoPlayer,
            CommentPlayer commentPlayer,
            CommentCommandEditerViewModel commentCommandEditerViewModel,
            KeepActiveDisplayWhenPlaying keepActiveDisplayWhenPlaying,
            ObservableMediaPlayer observableMediaPlayer,
            WindowService windowService,
            VideoEndedRecommendation videoEndedRecommendation,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            TogglePlayerDisplayViewCommand togglePlayerDisplayViewCommand,
            ShowPrimaryViewCommand showPrimaryViewCommand,
            MediaPlayerSoundVolumeManager soundVolumeManager,
            RestoreNavigationManager restoreNavigationManager,
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
            _scheduler = scheduler;
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
            PageManager = pageManager;
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
            VideoPlayer = videoPlayer;
            CommentPlayer = commentPlayer;
            CommentCommandEditerViewModel = commentCommandEditerViewModel;
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            TogglePlayerDisplayViewCommand = togglePlayerDisplayViewCommand;
            ShowPrimaryViewCommand = showPrimaryViewCommand;
            SoundVolumeManager = soundVolumeManager;
            _restoreNavigationManager = restoreNavigationManager;
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
            WindowService = windowService
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

            _saveTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _saveTimer.Interval = TimeSpan.FromSeconds(5);
            _saveTimer.IsRepeating = true;
            _saveTimer.Tick += (s, _) =>
            {
                //if (PrimaryViewPlayerManager.DisplayMode == PrimaryPlayerDisplayMode.Close) { return; }
                if (VideoInfo == null) { return; }
                if (MediaPlayer.CurrentState != MediaPlayerState.Playing) { return; }

                _restoreNavigationManager.SetCurrentPlayerEntry(
                        new PlayerEntry()
                        {
                            ContentId = VideoInfo.VideoAliasId,
                            Position = MediaPlayer.PlaybackSession.Position,
                            PlaylistId = _hohoemaPlaylistPlayer.CurrentPlaylistId?.Id,
                            PlaylistOrigin = _hohoemaPlaylistPlayer.CurrentPlaylistId?.Origin
                        });
            };

            PlayNextCommand = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylistItem)
                .SelectMany(async x => await _hohoemaPlaylistPlayer.CanGoNextAsync())
                .ToAsyncReactiveCommand()
                .AddTo(_CompositeDisposable);

            PlayNextCommand.Subscribe(async () => await _hohoemaPlaylistPlayer.GoNextAsync(NavigationCancellationToken))
                .AddTo(_CompositeDisposable);

            PlayPreviousCommand = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylistItem)
                .SelectMany(async x => await _hohoemaPlaylistPlayer.CanGoPreviewAsync())
                .ToAsyncReactiveCommand()
                .AddTo(_CompositeDisposable);

            PlayPreviousCommand.Subscribe(async () => await _hohoemaPlaylistPlayer.GoPreviewAsync(NavigationCancellationToken))
                .AddTo(_CompositeDisposable);
        }



        public SubscriptionManager SubscriptionManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public ChannelProvider ChannelProvider { get; }
        public MylistProvider MylistProvider { get; }

        public VideoCacheSettings_Legacy CacheSettings { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        
        public LocalMylistManager LocalMylistManager { get; }
        public LoginUserOwnedMylistManager UserMylistManager { get; }
        public PageManager PageManager { get; }
        public ScondaryViewPlayerManager PlayerViewManager { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
        public MylistAddItemCommand AddMylistCommand { get; }
        public LocalPlaylistAddItemCommand LocalPlaylistAddItemCommand { get; }
        public MylistCreateCommand CreateMylistCommand { get; }


        public MediaPlayer MediaPlayer { get; }
        public VideoTogglePlayPauseCommand VideoTogglePlayPauseCommand { get; }
        public NiconicoSession NiconicoSession { get; }
        public VideoPlayer VideoPlayer { get; }
        public CommentPlayer CommentPlayer { get; }
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
        public WindowService WindowService { get; }
        public VideoEndedRecommendation VideoEndedRecommendation { get; }
        public INicoVideoDetails VideoDetails { get; private set; }
        public PlayerSettings PlayerSettings { get; }

        public MediaPlayerSeekCommand SeekCommand { get; }
        public MediaPlayerSetPlaybackRateCommand SetPlaybackRateCommand { get; }
        public MediaPlayerToggleMuteCommand ToggleMuteCommand { get; }
        public MediaPlayerVolumeUpCommand VolumeUpCommand { get; }
        public MediaPlayerVolumeDownCommand VolumeDownCommand { get; }

        private readonly DispatcherQueueTimer _saveTimer;
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


        private bool nowPlayingWithCache;
        public bool NowPlayingWithCache
        {
            get { return nowPlayingWithCache; }
            set { SetProperty(ref nowPlayingWithCache, value); }
        }


        public IVideoContent VideoContent { get; private set; }


        public VideoSeriesViewModel VideoSeries { get; private set; }

        NotificationService _NotificationService;
        DialogService _HohoemaDialogService;

        private readonly VideoStreamingOriginOrchestrator _videoStreamingOriginOrchestrator;
        private readonly RestoreNavigationManager _restoreNavigationManager;
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

        private DelegateCommand _OpenVideoInfoCommand;
        public DelegateCommand OpenVideoInfoCommand
        {
            get
            {
                return _OpenVideoInfoCommand
                    ?? (_OpenVideoInfoCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.VideoInfomation, VideoId);
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
            VideoPlayer?.Dispose();
            CommentPlayer?.Dispose();

            base.Dispose();
        }

        private void StartStateSavingTimer()
        {
            _saveTimer.Start();
        }

        private void StopStateSavingTimer()
        {
            _saveTimer.Stop();
        }




        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
			Debug.WriteLine("VideoPlayer OnNavigatedToAsync start.");

            VideoId = _hohoemaPlaylistPlayer.CurrentPlaylistItem?.ItemId;

            _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentQuality)
                .Subscribe(quality => _scheduler.Schedule(() => CurrentQuality = quality))
                .AddTo(_NavigatingCompositeDisposable);

            _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylistItem)
                .Subscribe(x =>
                {
                    _scheduler.ScheduleAsync(async (s, ct) => 
                    {
                        if (x == null)
                        {
                            VideoInfo = null;
                            VideoSeries = null;
                            RaisePropertyChanged(nameof(VideoContent));
                            RaisePropertyChanged(nameof(VideoSeries));
                            VideoContent = null;
                            Title = string.Empty;
                            IsNotSupportVideoType = false;
                            LikesContext = VideoLikesContext.Default;
                            AvailableQualities = null;
                            NowPlayingWithCache = false;
                            return;
                        }

                        // 削除状態をチェック（再生準備より先に行う）
                        var (res, video) = await NicoVideoProvider.GetVideoInfoAsync(x.ItemId);
                        VideoInfo = video;
                        CheckDeleted(res);

                        MediaPlayer.AutoPlay = true;

                        //var result = await _videoStreamingOriginOrchestrator.CreatePlayingOrchestrateResultAsync(VideoId);
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
                        RaisePropertyChanged(nameof(VideoContent));

                        VideoEndedRecommendation.SetCurrentVideoSeries(VideoDetails);
                        Debug.WriteLine("次シリーズ動画: " + VideoDetails.Series?.Video.Next?.Title);

                        VideoSeries = VideoDetails.Series is not null and var series ? new VideoSeriesViewModel(series) : null;
                        RaisePropertyChanged(nameof(VideoSeries));

                        // 好きの切り替え
                        if (NiconicoSession.IsLoggedIn)
                        {
                            LikesContext = new VideoLikesContext(VideoDetails, NiconicoSession.ToolkitContext.Likes, _NotificationService);
                        }
                    });
                })
                .AddTo(_NavigatingCompositeDisposable);

            /*
            _requestVideoQuality = PlayerSettings.DefaultVideoQuality;
            if (parameters.TryGetValue("quality", out NicoVideoQuality quality))
            {
                _requestVideoQuality = quality;
            }
            else if (parameters.TryGetValue("quality", out string qualityString))
            {
                if (Enum.TryParse(qualityString, out quality))
                {
                    _requestVideoQuality = quality;
                }
            }
            */

            /*
            TimeSpan startPosition = TimeSpan.Zero;
            if (parameters.TryGetValue("position", out int position))
            {
                startPosition = TimeSpan.FromSeconds(position);
            }
            */

           
            
            // 動画再生コンテンツをセット
            //await VideoPlayer.UpdatePlayingVideoAsync(result.VideoSessionProvider);

            // そのあとで表示情報を取得
            //VideoInfo ??= await NicoVideoProvider.GetVideoInfoAsync(VideoId);
            /*
            try
            {
                // デフォルト指定した画質で再生開始
                await VideoPlayer.PlayAsync(_requestVideoQuality, startPosition);
            }
            catch (Models.Domain.VideoCache.VideoCacheException)
            {
                result = await _videoStreamingOriginOrchestrator.PreperePlayWithOnline(VideoId);
                VideoDetails = result.VideoDetails;
                await VideoPlayer.UpdatePlayingVideoAsync(result.VideoSessionProvider);
                await VideoPlayer.PlayAsync(_requestVideoQuality, startPosition);
            }
            */


            /*
            var smtc = SystemMediaTransportControls.GetForCurrentView();
            //            smtc.AutoRepeatModeChangeRequested += Smtc_AutoRepeatModeChangeRequested;
            MediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;

            smtc.DisplayUpdater.ClearAll();
            smtc.IsEnabled = true;
            smtc.IsPlayEnabled = true;
            smtc.IsPauseEnabled = true;
            smtc.DisplayUpdater.Type = MediaPlaybackType.Video;
            smtc.DisplayUpdater.VideoProperties.Title = VideoInfo.Title;
            smtc.DisplayUpdater.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(VideoInfo.ThumbnailUrl));
            smtc.DisplayUpdater.Update();
            */

            // 実行順依存：VideoPlayerで再生開始後に次シリーズ動画を設定する
            
            StartStateSavingTimer();


            Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");

            App.Current.Resuming += Current_Resuming;
            App.Current.Suspending += Current_Suspending;
        }

        

        private void CheckDeleted(VideoIdSearchSingleResponse res)
        {
            try
            {
                // 動画が削除されていた場合
                if (res.Video.IsDeleted)
                {
                    Debug.WriteLine($"cant playback{VideoId}. due to denied access to watch page, or connection offline.");

                    _scheduler.ScheduleAsync(async (scheduler, cancelToken) =>
                    {
                        await Task.Delay(100);

                        string toastContent = "";
                        if (!String.IsNullOrEmpty(res.Video.Title))
                        {
                            toastContent = "DeletedVideoNoticeWithTitle".Translate(res.Video.Title);
                        }
                        else
                        {
                            toastContent = "DeletedVideoNotice".Translate();
                        }

                        _NotificationService.ShowToast("DeletedVideoToastNotificationTitleWithVideoId".Translate(res.Video.Id), toastContent);
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

            StopStateSavingTimer();

            App.Current.Resuming -= Current_Resuming;
            App.Current.Suspending -= Current_Suspending;

            Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");

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
                StopStateSavingTimer();

                if (MediaPlayer.Source != null)
                {
                    MediaPlayer.Pause();
                    // サスペンド時にメモリ使用可能量が変更された場合に対応する
                    MediaPlayer.Source = null;
                }
            }
            catch (Exception ex) { ErrorTrackingManager.TrackError(ex); }
            finally
            {
                defferal.Complete();
            }            
        }

        private void Current_Resuming(object sender, object e)
        {
            try
            {
                StartStateSavingTimer();
            }
            catch (Exception ex) { ErrorTrackingManager.TrackError(ex); }
        }




        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                if (VideoPlayer.PlayPreviousCommand.CanExecute())
                {
                    VideoPlayer.PlayPreviousCommand.Execute();
                }
            }
        }

        private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                if (VideoPlayer.PlayNextCommand.CanExecute())
                {
                    VideoPlayer.PlayNextCommand.Execute();
                }
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


        private DelegateCommand<string> _selectSidePaneCommand;
        public DelegateCommand<string> SelectSidePaneCommand => _selectSidePaneCommand
            ?? (_selectSidePaneCommand = new DelegateCommand<string>(str =>
            {
                if (Enum.TryParse<PlayerSidePaneContentType>(str, out var type))
                {
                    if (type == SidePaneType || type == PlayerSidePaneContentType.None)
                    {
                        SidePaneType = PlayerSidePaneContentType.None;
                        SidePaneViewModel = EmptySidePaneContentViewModel.Default;
                        PlayerSplitViewIsPaneOpen = false;
                    }
                    else
                    {
                        SidePaneType = type;
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
                        PlayerSplitViewIsPaneOpen = true;
                    }
                }
            }));

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

        public string ProviderType => throw new NotSupportedException();

        public string ProviderId => throw new NotSupportedException();
    }
}

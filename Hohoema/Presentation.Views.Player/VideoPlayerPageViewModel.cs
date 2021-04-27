using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.NicoVideos.Player;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.Services.Player;
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

namespace Hohoema.Presentation.ViewModels.Player
{

    public class VideoPlayerPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
	{
        // TODO: HohoemaViewModelBaseとの依存性を排除（ViewModelBaseとの関係性は維持）
        private readonly IScheduler _scheduler;




        public VideoPlayerPageViewModel(
            IScheduler scheduler,
            NiconicoSession niconicoSession,
            SubscriptionManager subscriptionManager,
            NicoVideoProvider nicoVideoProvider,
            ChannelProvider channelProvider,
            MylistProvider mylistProvider,
            PlayerSettings playerSettings,
            VideoCacheSettings cacheSettings,
            ApplicationLayoutManager applicationLayoutManager,
            HohoemaPlaylist hohoemaPlaylist,
            LocalMylistManager localMylistManager,
            UserMylistManager userMylistManager,
            PageManager pageManager,
            MediaPlayer mediaPlayer,
            NotificationService notificationService,
            DialogService dialogService,
            ExternalAccessService externalAccessService,
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
            NicoVideoCacheRepository nicoVideoRepository
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
            HohoemaPlaylist = hohoemaPlaylist;
            LocalMylistManager = localMylistManager;
            UserMylistManager = userMylistManager;
            PageManager = pageManager;
            _NotificationService = notificationService;
            _HohoemaDialogService = dialogService;
            ExternalAccessService = externalAccessService;
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
            _nicoVideoRepository = nicoVideoRepository;
            ObservableMediaPlayer = observableMediaPlayer
                .AddTo(_CompositeDisposable);
            WindowService = windowService
                .AddTo(_CompositeDisposable);
            VideoEndedRecommendation = videoEndedRecommendation
                .AddTo(_CompositeDisposable);
            _keepActiveDisplayWhenPlaying = keepActiveDisplayWhenPlaying
                .AddTo(_CompositeDisposable);
            MediaPlayer = mediaPlayer;

            SeekCommand = new MediaPlayerSeekCommand(MediaPlayer);
            SetPlaybackRateCommand = new MediaPlayerSetPlaybackRateCommand(MediaPlayer);
            ToggleMuteCommand = new MediaPlayerToggleMuteCommand(MediaPlayer);
            VolumeUpCommand = new MediaPlayerVolumeUpCommand(SoundVolumeManager);
            VolumeDownCommand = new MediaPlayerVolumeDownCommand(SoundVolumeManager);
        }



        public SubscriptionManager SubscriptionManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public ChannelProvider ChannelProvider { get; }
        public MylistProvider MylistProvider { get; }

        public VideoCacheSettings CacheSettings { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public PageManager PageManager { get; }
        public ScondaryViewPlayerManager PlayerViewManager { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public LocalPlaylistCreateCommand CreateLocalMylistCommand { get; }
        public MylistAddItemCommand AddMylistCommand { get; }
        public LocalPlaylistAddItemCommand LocalPlaylistAddItemCommand { get; }
        public MylistCreateCommand CreateMylistCommand { get; }


        public MediaPlayer MediaPlayer { get; }

        public NiconicoSession NiconicoSession { get; }
        public VideoPlayer VideoPlayer { get; }
        public CommentPlayer CommentPlayer { get; }
        public CommentCommandEditerViewModel CommentCommandEditerViewModel { get; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public TogglePlayerDisplayViewCommand TogglePlayerDisplayViewCommand { get; }
        public ShowPrimaryViewCommand ShowPrimaryViewCommand { get; }
        public MediaPlayerSoundVolumeManager SoundVolumeManager { get; }
        public ObservableMediaPlayer ObservableMediaPlayer { get; }
        public WindowService WindowService { get; }
        public VideoEndedRecommendation VideoEndedRecommendation { get; }
        public INicoVideoDetails VideoDetails { get; private set; }
        public PlayerSettings PlayerSettings { get; }
        public ExternalAccessService ExternalAccessService { get; }

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



        private NicoVideoQuality _requestVideoQuality;

        private NicoVideoQualityEntity _currentQuality;
        public NicoVideoQualityEntity CurrentQuality
        {
            get { return _currentQuality; }
            private set { SetProperty(ref _currentQuality, value); }
        }

        public IVideoContent VideoContent { get; private set; }


        public VideoSeriesViewModel VideoSeries { get; private set; }

        NotificationService _NotificationService;
        DialogService _HohoemaDialogService;

        private readonly VideoStreamingOriginOrchestrator _videoStreamingOriginOrchestrator;
        private readonly RestoreNavigationManager _restoreNavigationManager;
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
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

        private bool _isLikedVideo;
        public bool IsLikedVideo
        {
            get { return _isLikedVideo; }
            set { SetProperty(ref _isLikedVideo, value); }
        }

        private string _LikeThanksMessage;
        public string LikeThanksMessage
        {
            get { return _LikeThanksMessage; }
            private set { SetProperty(ref _LikeThanksMessage, value); }
        }


        private bool _NowLikeProcessing;
        public bool NowLikeProcessing
        {
            get { return _NowLikeProcessing; }
            private set { SetProperty(ref _NowLikeProcessing, value); }
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






        public override void Destroy()
        {
            VideoPlayer?.Dispose();
            CommentPlayer?.Dispose();

            base.Destroy();
        }


        CompositeDisposable _TimerDiposable;

        private void InitializeTimers()
        {
            CloseTimers();

            _TimerDiposable = new CompositeDisposable();
            Observable.Timer(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(2.5), _scheduler)
                .Subscribe(_ =>
                {
                    //if (PrimaryViewPlayerManager.DisplayMode == PrimaryPlayerDisplayMode.Close) { return; }

                    _restoreNavigationManager.SetCurrentPlayerEntry(
                            new PlayerEntry()
                            {
                                ContentId = VideoInfo.VideoId,
                                Position = MediaPlayer.PlaybackSession.Position,
                                PlaylistId = HohoemaPlaylist.CurrentPlaylist?.Id,
                                PlaylistOrigin = HohoemaPlaylist.CurrentPlaylist?.GetOrigin()
                            });

                    Debug.WriteLine("SetCurrentPlayerEntry");
                })
                .AddTo(_TimerDiposable);
        }

        private void CloseTimers()
        {
            _TimerDiposable?.Dispose();
            _TimerDiposable = null;
        }


        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
			Debug.WriteLine("VideoPlayer OnNavigatedToAsync start.");

            VideoId = parameters.GetValue<string>("id");

            _requestVideoQuality = PlayerSettings.DefaultQuality;
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

            TimeSpan startPosition = TimeSpan.Zero;
            if (parameters.TryGetValue("position", out int position))
            {
                startPosition = TimeSpan.FromSeconds(position);
            }
           
            // 削除状態をチェック（再生準備より先に行う）
            VideoInfo = _nicoVideoRepository.Get(VideoId);
            CheckDeleted(VideoInfo);

            MediaPlayer.AutoPlay = true;

            var result = await _videoStreamingOriginOrchestrator.CreatePlayingOrchestrateResultAsync(VideoId);
            if (!result.IsSuccess)
            {
                Title = VideoInfo.Title;
                IsNotSupportVideoType = true;
                CannotPlayReason = result.Exception?.Message ?? result.PlayingOrchestrateFailedReason.Translate();

                VideoInfo = await NicoVideoProvider.GetNicoVideoInfo(VideoId)
                    ?? _nicoVideoRepository.Get(VideoId);

                // 改めて削除状態をチェック（動画リスト経由してない場合の削除チェック）
                CheckDeleted(VideoInfo);

                return;
            }

            
            VideoDetails = result.VideoDetails;

            SoundVolumeManager.LoudnessCorrectionValue = VideoDetails.LoudnessCorrectionValue;
            IsLikedVideo = VideoDetails.IsLikedVideo;

            // 動画再生コンテンツをセット
            await VideoPlayer.UpdatePlayingVideoAsync(result.VideoSessionProvider);

            // そのあとで表示情報を取得
            VideoInfo = await NicoVideoProvider.GetNicoVideoInfo(VideoId)
                ?? _nicoVideoRepository.Get(VideoId);

            
            // デフォルト指定した画質で再生開始
            await VideoPlayer.PlayAsync(_requestVideoQuality, startPosition);

            // コメントを更新
            await CommentPlayer.UpdatePlayingCommentAsync(result.CommentSessionProvider);

            VideoContent = VideoInfo;
            RaisePropertyChanged(nameof(VideoContent));

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

            // 実行順依存：VideoPlayerで再生開始後に次シリーズ動画を設定する
            VideoEndedRecommendation.SetCurrentVideoSeries(VideoDetails.Series);
            Debug.WriteLine("次シリーズ動画: " + VideoDetails.Series?.Video.Next?.Title);

            VideoSeries = VideoDetails.Series is not null and var series ? new VideoSeriesViewModel(series) : null;
            RaisePropertyChanged(nameof(VideoSeries));

            // 好きの切り替え
            this.ObserveProperty(x => x.IsLikedVideo, isPushCurrentValueAtFirst: false)
                .Where(x => !NowLikeProcessing)
                .Subscribe(async like => 
                {
                    await ProcessLikeAsync(like);
                })
                .AddTo(_NavigatingCompositeDisposable);

            InitializeTimers();


            Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");

            App.Current.Resuming += Current_Resuming;
            App.Current.Suspending += Current_Suspending;
        }

        
        private async Task ProcessLikeAsync(bool like)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            NowLikeProcessing = true;

            try
            {
                if (like)
                {
                    var res = await NiconicoSession.Context.User.DoLikeVideoAsync(this.VideoId);
                    if (!res.IsOK)
                    {
                        this.IsLikedVideo = false;
                    }
                    else
                    {
                        LikeThanksMessage = res.ThanksMessage;

                        if (!string.IsNullOrEmpty(LikeThanksMessage))
                        {
                            _NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                            {
                                Title = "LikeThanksMessageDescWithVideoOwnerName".Translate(VideoInfo.Owner?.ScreenName),
                                Icon = VideoInfo.Owner?.IconUrl,
                                Content = LikeThanksMessage,
                                IsShowDismissButton = true,
                                ShowDuration = TimeSpan.FromSeconds(7),
                            });
                        }
                    }
                }
                else
                {
                    LikeThanksMessage = null;

                    var res = await NiconicoSession.Context.User.UnDoLikeVideoAsync(this.VideoId);
                    if (!res.IsOK)
                    {
                        this.IsLikedVideo = true;
                    }
                }
            }
            finally
            {
                NowLikeProcessing = false;
            }
        }

        private void CheckDeleted(NicoVideo videoInfo)
        {
            try
            {
                // 動画が削除されていた場合
                if (videoInfo.IsDeleted)
                {
                    Debug.WriteLine($"cant playback{VideoId}. due to denied access to watch page, or connection offline.");

                    _scheduler.ScheduleAsync(async (scheduler, cancelToken) =>
                    {
                        await Task.Delay(100);

                        string toastContent = "";
                        if (!String.IsNullOrEmpty(videoInfo.Title))
                        {
                            toastContent = "DeletedVideoNoticeWithTitle".Translate(videoInfo.Title);
                        }
                        else
                        {
                            toastContent = "DeletedVideoNotice".Translate();
                        }

                        _NotificationService.ShowToast("DeletedVideoToastNotificationTitleWithVideoId".Translate(videoInfo.RawVideoId), toastContent);
                    });

                    // ローカルプレイリストの場合は勝手に消しておく
                    if (HohoemaPlaylist.CurrentPlaylist is LocalPlaylist localPlaylist)
                    {
                        if (localPlaylist.IsQueuePlaylist())
                        {
                            HohoemaPlaylist.RemoveQueue(videoInfo);
                        }
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

            if (VideoInfo != null)
            {
                HohoemaPlaylist.PlayDone(VideoInfo, MediaPlayer.PlaybackSession.Position);
            }

            MediaPlayer.Source = null;

            CommentPlayer.ClearCurrentSession();
            _ = VideoPlayer.ClearCurrentSessionAsync();

            MediaPlayer.CommandManager.NextReceived -= CommandManager_NextReceived;
            MediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;

            var smtc = SystemMediaTransportControls.GetForCurrentView();
            smtc.DisplayUpdater.ClearAll();
            smtc.DisplayUpdater.Update();

            CloseTimers();

            App.Current.Resuming -= Current_Resuming;
            App.Current.Suspending -= Current_Suspending;

            Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");

            IsNotSupportVideoType = false;
            CannotPlayReason = null;

            base.OnNavigatedFrom(parameters);
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            var defferal = e.SuspendingOperation.GetDeferral();
            try
            {
                CloseTimers();

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
            InitializeTimers();
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

                /*
                if (HohoemaPlaylist.Player.CanGoBack)
                {
                    HohoemaPlaylist.Player.GoBack();
                }
                */
            }
        }


    }

    public class VideoSeriesViewModel : ISeries
    {
        private readonly Mntone.Nico2.Videos.Dmc.Series _userSeries;

        public VideoSeriesViewModel(Mntone.Nico2.Videos.Dmc.Series userSeries)
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

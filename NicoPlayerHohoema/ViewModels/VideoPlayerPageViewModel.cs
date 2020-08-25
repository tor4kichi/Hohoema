using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Dmc;
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
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer;
using NicoPlayerHohoema.UseCase.Playlist;
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
using Prism.Ioc;
using NicoPlayerHohoema.Repository.Playlist;
using NicoPlayerHohoema.Services.Player;
using NicoPlayerHohoema.UseCase.NicoVideoPlayer.Commands;
using I18NPortable;
using NicoPlayerHohoema.ViewModels.Subscriptions;
using NicoPlayerHohoema.Models.RestoreNavigation;

namespace NicoPlayerHohoema.ViewModels
{

    public class VideoPlayerPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
	{
        // TODO: HohoemaViewModelBaseとの依存性を排除（ViewModelBaseとの関係性は維持）
        private readonly IScheduler _scheduler;




        public VideoPlayerPageViewModel(
            IScheduler scheduler,
            IEventAggregator eventAggregator,
            Models.NiconicoSession niconicoSession,
            Models.Subscription.SubscriptionManager subscriptionManager,
            NicoVideoProvider nicoVideoProvider,
            ChannelProvider channelProvider,
            MylistProvider mylistProvider,
            PlayerSettings playerSettings,
            CacheSettings cacheSettings,
            NGSettings ngSettings,
            ApplicationLayoutManager applicationLayoutManager,
            HohoemaPlaylist hohoemaPlaylist,
            LocalMylistManager localMylistManager,
            UserMylistManager userMylistManager,
            PageManager pageManager,
            MediaPlayer mediaPlayer,
            NotificationService notificationService,
            DialogService dialogService,
            ExternalAccessService externalAccessService,
            ViewModels.Subscriptions.AddSubscriptionCommand addSubscriptionCommand,
            Commands.Mylist.CreateLocalMylistCommand createLocalMylistCommand,
            Commands.Mylist.CreateMylistCommand createMylistCommand,
            UseCase.NicoVideoPlayer.VideoStreamingOriginOrchestrator videoStreamingOriginOrchestrator,
            UseCase.VideoPlayer videoPlayer,
            UseCase.CommentPlayer commentPlayer,
            CommentCommandEditerViewModel commentCommandEditerViewModel,
            KeepActiveDisplayWhenPlaying keepActiveDisplayWhenPlaying,
            ObservableMediaPlayer observableMediaPlayer,
            WindowService windowService,
            VideoEndedRecommendation videoEndedRecommendation,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            TogglePlayerDisplayViewCommand togglePlayerDisplayViewCommand,
            ShowPrimaryViewCommand showPrimaryViewCommand,
            MediaPlayerSoundVolumeManager soundVolumeManager,
            RestoreNavigationManager restoreNavigationManager
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
            NgSettings = ngSettings;
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



        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public ChannelProvider ChannelProvider { get; }
        public MylistProvider MylistProvider { get; }

        public CacheSettings CacheSettings { get; }
        public NGSettings NgSettings { get; }
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public LocalMylistManager LocalMylistManager { get; }
        public UserMylistManager UserMylistManager { get; }
        public PageManager PageManager { get; }
        public ScondaryViewPlayerManager PlayerViewManager { get; }
        public AddSubscriptionCommand AddSubscriptionCommand { get; }
        public Commands.Mylist.CreateLocalMylistCommand CreateLocalMylistCommand { get; }
        public Commands.Mylist.CreateMylistCommand CreateMylistCommand { get; }


        public MediaPlayer MediaPlayer { get; }

        public Models.NiconicoSession NiconicoSession { get; }
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


        NotificationService _NotificationService;
        DialogService _HohoemaDialogService;

        private readonly VideoStreamingOriginOrchestrator _videoStreamingOriginOrchestrator;
        private readonly RestoreNavigationManager _restoreNavigationManager;
        private readonly KeepActiveDisplayWhenPlaying _keepActiveDisplayWhenPlaying;




        private Database.NicoVideo _videoInfo;
        public Database.NicoVideo VideoInfo
        {
            get => _videoInfo;
            set => SetProperty(ref _videoInfo, value);
        }


        public Models.Subscription.SubscriptionSource? SubscriptionSource => this.VideoInfo?.Owner != null 
            ? new Models.Subscription.SubscriptionSource(VideoInfo.Owner.ScreenName, VideoInfo.Owner.UserType == Database.NicoVideoUserType.User 
                ? Models.Subscription.SubscriptionSourceType.User 
                : Models.Subscription.SubscriptionSourceType.Channel, VideoInfo.Owner.OwnerId)
            : default(Models.Subscription.SubscriptionSource)
            ;


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
            VideoInfo = Database.NicoVideoDb.Get(VideoId);
            await CheckDeleted(VideoInfo);

            MediaPlayer.AutoPlay = true;

            var result = await _videoStreamingOriginOrchestrator.CreatePlayingOrchestrateResultAsync(VideoId);

            if (!result.IsSuccess)
            {
                // TODO: 再生できない理由を表示したい
                return;
            }

            VideoDetails = result.VideoDetails;

            SoundVolumeManager.LoudnessCorrectionValue = VideoDetails.LoudnessCorrectionValue;

            // 動画再生コンテンツをセット
            await VideoPlayer.UpdatePlayingVideoAsync(result.VideoSessionProvider);

            // そのあとで表示情報を取得
            VideoInfo = await NicoVideoProvider.GetNicoVideoInfo(VideoId)
                ?? Database.NicoVideoDb.Get(VideoId);

            // 改めて削除状態をチェック（動画リスト経由してない場合の削除チェック）

            if (VideoInfo.IsDeleted)
            {
                _ = CheckDeleted(VideoInfo);

                IsNotSupportVideoType = true;
                CannotPlayReason = "CanNotPlayNotice_WithPrivateReason".Translate(VideoInfo.PrivateReasonType);
            }
            else if (VideoInfo.MovieType == Database.MovieType.Swf)
            {
                IsNotSupportVideoType = true;
                CannotPlayReason = "CanNotPlayNotice_NotSupportSWF".Translate();
            }
            else
            {
                // デフォルト指定した画質で再生開始
                await VideoPlayer.PlayAsync(_requestVideoQuality, startPosition );

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
            }

            // 実行順依存：VideoPlayerで再生開始後に次シリーズ動画を設定する
            VideoEndedRecommendation.SetCurrentVideoSeries(VideoDetails.Series);
            Debug.WriteLine("次シリーズ動画: " + VideoDetails.Series?.NextVideo?.Title);

            Observable.Timer(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), _scheduler)
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
                .AddTo(_NavigatingCompositeDisposable);

            Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");

            App.Current.Resuming += Current_Resuming;
            App.Current.Suspending += Current_Suspending;
        }

       

        private async Task CheckDeleted(Database.NicoVideo videoInfo)
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
                        if (localPlaylist.IsWatchAfterPlaylist())
                        {
                            HohoemaPlaylist.RemoveWatchAfter(videoInfo);
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

            MediaPlayer.Source = null;

            _ = VideoPlayer.ClearCurrentSessionAsync();
            CommentPlayer.ClearCurrentSession();

            MediaPlayer.CommandManager.NextReceived -= CommandManager_NextReceived;
            MediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;

            var smtc = SystemMediaTransportControls.GetForCurrentView();
            smtc.DisplayUpdater.ClearAll();
            smtc.DisplayUpdater.Update();

            if (VideoInfo != null)
            {
                HohoemaPlaylist.PlayDone(VideoInfo);
            }

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


        // プレイリスト

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

		    PlayerSettings.NGCommentUserIds.Add(new UserIdInfo()
			{
				UserId = commentViewModel.UserId,
				Description = userName
			});

			return Task.CompletedTask;
		}
    }

}

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
            AppearanceSettings appearanceSettings,
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            MediaPlayer mediaPlayer,
            NotificationService notificationService,
            DialogService dialogService,
            ExternalAccessService externalAccessService,
            Commands.Subscriptions.CreateSubscriptionGroupCommand createSubscriptionGroupCommand,
            Commands.Mylist.CreateLocalMylistCommand createLocalMylistCommand,
            Commands.Mylist.CreateMylistCommand createMylistCommand,
            UseCase.NicoVideoPlayer.VideoStreamingOriginOrchestrator videoStreamingOriginOrchestrator,
            UseCase.VideoPlayer videoPlayer,
            UseCase.CommentPlayer commentPlayer,
            KeepActiveDisplayWhenPlaying keepActiveDisplayWhenPlaying,
            ObservableMediaPlayer observableMediaPlayer,
            WindowService windowService,
            VideoEndedRecommendation videoEndedRecommendation,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            TogglePlayerDisplayViewCommand togglePlayerDisplayViewCommand,
            ShowPrimaryViewCommand showPrimaryViewCommand
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
            AppearanceSettings = appearanceSettings;
            HohoemaPlaylist = hohoemaPlaylist;
            PageManager = pageManager;
            _NotificationService = notificationService;
            _HohoemaDialogService = dialogService;
            ExternalAccessService = externalAccessService;
            CreateSubscriptionGroupCommand = createSubscriptionGroupCommand;
            CreateLocalMylistCommand = createLocalMylistCommand;
            CreateMylistCommand = createMylistCommand;
            _videoStreamingOriginOrchestrator = videoStreamingOriginOrchestrator;
            VideoPlayer = videoPlayer;
            CommentPlayer = commentPlayer;
            PrimaryViewPlayerManager = primaryViewPlayerManager;
            TogglePlayerDisplayViewCommand = togglePlayerDisplayViewCommand;
            ShowPrimaryViewCommand = showPrimaryViewCommand;
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
            VolumeUpCommand = new MediaPlayerVolumeUpCommand(MediaPlayer);
            VolumeDownCommand = new MediaPlayerVolumeDownCommand(MediaPlayer);
        }



        public Models.Subscription.SubscriptionManager SubscriptionManager { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public ChannelProvider ChannelProvider { get; }
        public MylistProvider MylistProvider { get; }

        public CacheSettings CacheSettings { get; }
        public NGSettings NgSettings { get; }
        public AppearanceSettings AppearanceSettings { get; }

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PageManager PageManager { get; }
        public ScondaryViewPlayerManager PlayerViewManager { get; }
        public Commands.Subscriptions.CreateSubscriptionGroupCommand CreateSubscriptionGroupCommand { get; }
        public Commands.Mylist.CreateLocalMylistCommand CreateLocalMylistCommand { get; }
        public Commands.Mylist.CreateMylistCommand CreateMylistCommand { get; }


        public MediaPlayer MediaPlayer { get; }

        public Models.NiconicoSession NiconicoSession { get; }
        public VideoPlayer VideoPlayer { get; }
        public CommentPlayer CommentPlayer { get; }
        public PrimaryViewPlayerManager PrimaryViewPlayerManager { get; }
        public TogglePlayerDisplayViewCommand TogglePlayerDisplayViewCommand { get; }
        public ShowPrimaryViewCommand ShowPrimaryViewCommand { get; }
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
        private readonly KeepActiveDisplayWhenPlaying _keepActiveDisplayWhenPlaying;





        // TODO: IsXbox , VisualStateで実現する
        public bool IsXbox => Services.Helpers.DeviceTypeHelper.IsXbox;

        // TODO: IsTVModeEnabled 、VideoPlayerPage上でフォーカスを与える目的で利用、レイアウト側で対応すべき
        public bool IsTVModeEnabled => AppearanceSettings.IsForceTVModeEnable || Services.Helpers.DeviceTypeHelper.IsXbox;

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

            // 動画再生コンテンツをセット
            VideoPlayer.UpdatePlayingVideo(result.VideoSessionProvider);

            // そのあとで表示情報を取得
            VideoInfo = await NicoVideoProvider.GetNicoVideoInfo(VideoId)
                ?? Database.NicoVideoDb.Get(VideoId);

            // 改めて削除状態をチェック（動画リスト経由してない場合の削除チェック）

            if (VideoInfo.IsDeleted)
            {
                _ = CheckDeleted(VideoInfo);

                IsNotSupportVideoType = true;
                CannotPlayReason = $"この動画は {VideoInfo.PrivateReasonType.ToCulturelizeString()} のため視聴できません";
            }
            else if (VideoInfo.MovieType == Database.MovieType.Swf)
            {
                IsNotSupportVideoType = true;
                CannotPlayReason = $" SWF形式の動画は対応していないため視聴できません";
            }
            else
            {
                // TODO: デフォルト指定した画質で再生開始
                await VideoPlayer.PlayAsync(_requestVideoQuality);

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

#if DEBUG
            if (HohoemaPlaylist.CurrentPlaylist == null)
            {
                throw new Exception();
            }
#endif


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
                            toastContent = $"\"{videoInfo.Title}\" は削除された動画です";
                        }
                        else
                        {
                            toastContent = $"削除された動画です";
                        }
                        _NotificationService.ShowToast($"動画 {VideoId} は再生できません", toastContent);
                    });

                    // ローカルプレイリストの場合は勝手に消しておく
                    if (HohoemaPlaylist.CurrentPlaylist is LocalPlaylist localPlaylist)
                    {
                        if (localPlaylist.IsWatchAfterPlaylist())
                        {
                            var item = HohoemaPlaylist.QueuePlaylist.FirstOrDefault(x => x.Id == VideoId);
                            if (item != null)
                            {
                                HohoemaPlaylist.QueuePlaylist.Remove(item);
                            }
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

            App.Current.Suspending -= Current_Suspending;

            MediaPlayer.CommandManager.NextReceived -= CommandManager_NextReceived;
            MediaPlayer.CommandManager.PreviousReceived -= CommandManager_PreviousReceived;

            var smtc = SystemMediaTransportControls.GetForCurrentView();
            smtc.DisplayUpdater.ClearAll();
            smtc.DisplayUpdater.Update();

            VideoPlayer.ClearCurrentSession();
            CommentPlayer.ClearCurrentSession();

            // プレイリストへ再生完了を通知
            HohoemaPlaylist.PlayDone();


            App.Current.Resuming -= Current_Resuming;
            App.Current.Suspending -= Current_Suspending;

            Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");

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

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            pin = null;
            return false;
        }
    }

}

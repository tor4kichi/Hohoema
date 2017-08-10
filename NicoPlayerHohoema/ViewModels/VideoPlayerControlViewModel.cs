using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.ViewModels.VideoInfoContent;
using NicoPlayerHohoema.Views;
using NicoPlayerHohoema.Views.DownloadProgress;
using NicoPlayerHohoema.Views.Service;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using FFmpegInterop;
using Windows.Foundation.Collections;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Media;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoPlayerControlViewModel : HohoemaViewModelBase, IDisposable
	{
		// TODO: HohoemaViewModelBaseとの依存性を排除（ViewModelBaseとの関係性は維持）



		const uint default_DisplayTime = 400; // 1 = 10ms, 400 = 4000ms = 4.0 Seconds



		private SynchronizationContextScheduler _PlayerWindowUIDispatcherScheduler;
		public SynchronizationContextScheduler PlayerWindowUIDispatcherScheduler
		{
			get
			{
				return _PlayerWindowUIDispatcherScheduler
					?? (_PlayerWindowUIDispatcherScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current));
			}
		}


		public VideoPlayerControlViewModel(
			HohoemaApp hohoemaApp, 
			EventAggregator ea,
			PageManager pageManager, 
			ToastNotificationService toast,
			TextInputDialogService textInputDialog,
            MylistRegistrationDialogService mylistDialog
			)
			: base(hohoemaApp, pageManager, canActivateBackgroundUpdate:true)
		{
			_ToastService = toast;
			_TextInputDialogService = textInputDialog;
            _MylistResistrationDialogService = mylistDialog;

            MediaPlayer = HohoemaApp.MediaPlayer;

            ThumbnailUri = new ReactiveProperty<string>();

            CurrentVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			ReadVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero);
//				.AddTo(_CompositeDisposable);
			CommentVideoPosition = new ReactiveProperty<TimeSpan>(PlayerWindowUIDispatcherScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			NowSubmittingComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
			SliderVideoPosition = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0, mode:ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);
			VideoLength = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0)
				.AddTo(_CompositeDisposable);
			CurrentState = new ReactiveProperty<MediaPlaybackState>(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);
            LegacyCurrentState = CurrentState.Select(x =>
            {
                switch (x)
                {
                    case MediaPlaybackState.None:
                        return MediaElementState.Closed;
                    case MediaPlaybackState.Opening:
                        return MediaElementState.Opening;
                    case MediaPlaybackState.Buffering:
                        return MediaElementState.Buffering;
                    case MediaPlaybackState.Playing:
                        return MediaElementState.Playing;
                    case MediaPlaybackState.Paused:
                        if (Video != null 
                        && MediaPlayer.Source != null
                        && MediaPlayer.PlaybackSession.Position >= (Video.VideoLength - TimeSpan.FromSeconds(1)))
                        {
                            return MediaElementState.Stopped;
                        }
                        else
                        {
                            return MediaElementState.Paused;
                        }
                    default:
                        throw new Exception();
                }
            })
            .ToReactiveProperty();

            NowQualityChanging = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
			Comments = new ObservableCollection<Comment>();

            CanSubmitComment = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
            NowCommentWriting = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);
			NowSoundChanging = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);
            IsCommentDisplayEnable = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, true)
                .AddTo(_CompositeDisposable);
            // プレイヤーがフィル表示かつコメント表示を有効にしている場合のみ表示
            IsVisibleComment =
                Observable.CombineLatest(
                    HohoemaApp.Playlist.ObserveProperty(x => x.IsPlayerFloatingModeEnable).Select(x => !x),
                    IsCommentDisplayEnable
                    )
                    .Select(x => x.All(y => y))
                    .ToReactiveProperty();

			IsEnableRepeat = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
				.AddTo(_CompositeDisposable);
			
			WritingComment = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			CommentSubmitCommand = WritingComment.Select(x => !string.IsNullOrWhiteSpace(x))
				.ToReactiveCommand()
				.AddTo(_CompositeDisposable);

			CommentSubmitCommand.Subscribe(async x => await SubmitComment())
				.AddTo(_CompositeDisposable);

			NowCommentWriting.Subscribe(x => Debug.WriteLine("NowCommentWriting:" + NowCommentWriting.Value))
				.AddTo(_CompositeDisposable);

			IsPlayWithCache = new ReactiveProperty<bool>(false)
				.AddTo(_CompositeDisposable);

			IsNeedResumeExitWrittingComment = new ReactiveProperty<bool>();

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

			CommandString = new ReactiveProperty<string>("")
				.AddTo(_CompositeDisposable);

			CommentCanvasHeight = new ReactiveProperty<double>(0);
			CommentCanvasWidth = new ReactiveProperty<double>(0);

            CommentOpacity = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentOpacity)
                .Select(x => x.ToOpacity())
                .ToReadOnlyReactiveProperty(eventScheduler: PlayerWindowUIDispatcherScheduler);





            CurrentVideoQuality = new ReactiveProperty<NicoVideoQuality?>(PlayerWindowUIDispatcherScheduler, null, ReactivePropertyMode.None)
				.AddTo(_CompositeDisposable);
            RequestVideoQuality = new ReactiveProperty<NicoVideoQuality?>(PlayerWindowUIDispatcherScheduler, null, ReactivePropertyMode.None)
                .AddTo(_CompositeDisposable);

            IsAvailableDmcHighQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
                .AddTo(_CompositeDisposable);
            IsAvailableDmcMidiumQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
                .AddTo(_CompositeDisposable);
            IsAvailableDmcLowQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
                            .AddTo(_CompositeDisposable);
            IsAvailableDmcMobileQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
                            .AddTo(_CompositeDisposable);
            IsAvailableLegacyOriginalQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
                            .AddTo(_CompositeDisposable);
            IsAvailableLegacyLowQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false)
                            .AddTo(_CompositeDisposable);


            IsCacheLegacyOriginalQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false, mode: ReactivePropertyMode.None);
            IsCacheLegacyLowQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false, mode:ReactivePropertyMode.None);

            CanToggleCacheRequestLegacyOriginalQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
            CanToggleCacheRequestLegacyLowQuality = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
            
            CanToggleCurrentQualityCacheState = CurrentVideoQuality
				.SubscribeOnUIDispatcher()
				.Select(x =>
				{
					if (this.Video == null || IsDisposed) { return false; }

                    if (!x.HasValue) { return false; }

                    var div = Video.GetDividedQualityNicoVideo(x.Value);

                    if (div.IsCacheRequested)
                    {
                        return true;
                    }
                    else 
                    {
                        return div.CanRequestCache;
                    }
				})
				.ToReactiveProperty()
				.AddTo(_CompositeDisposable);

			IsSaveRequestedCurrentQualityCache = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false, ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);

			IsSaveRequestedCurrentQualityCache
				.Where(x => !IsDisposed)
				.SubscribeOnUIDispatcher()
				.Subscribe(async saveRequested => 
			{
				if (saveRequested)
				{
					await Video.RequestCache(this.CurrentVideoQuality.Value);
				}
				else
				{
					await Video.CancelCacheRequest(this.CurrentVideoQuality.Value);
				}

				CanToggleCurrentQualityCacheState.ForceNotify();
			})
			.AddTo(_CompositeDisposable);



			TogglePlayQualityCommand = new ReactiveCommand<NicoVideoQuality>()
				.AddTo(_CompositeDisposable);

			TogglePlayQualityCommand
				.Where(x => !IsDisposed && !IsNotSupportVideoType)
				.SubscribeOnUIDispatcher()
				.Subscribe(async quality => 
				{
                    _PreviosPlayingVideoPosition = ReadVideoPosition.Value;

                    RequestVideoQuality.Value = quality;

					await PlayingQualityChangeAction();
				})
				.AddTo(_CompositeDisposable);



			

			

			

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
                PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

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
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_CompositeDisposable);

			CurrentState
                .SubscribeOnUIDispatcher()
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
                    if (Video != null)
                    {
                        //await Video.StopPlay();

                        // TODO: ユーザー手動の再読み込みに変更する
                        //                        await Task.Delay(500);

                        //                        await this.PlayingQualityChangeAction();

                        Debug.WriteLine("再生中に動画がClosedになったため、強制的に再初期化を実行しました。これは非常措置です。");
                    }
                }

                await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
                {
                    SetKeepDisplayWithCurrentState();
                });

				Debug.WriteLine("player state :" + x.ToString());
			})
			.AddTo(_CompositeDisposable);


            // 再生速度
            PlaybackRate = new ReactiveProperty<double>(
                HohoemaApp.UserSettings.PlayerSettings.DefaultPlaybackRate
                )
                .AddTo(_CompositeDisposable);
            PlaybackRate.Subscribe(x => 
            {
                MediaPlayer.PlaybackSession.PlaybackRate = x;
            })
            .AddTo(_CompositeDisposable);

            ResetDefaultPlaybackRate = new DelegateCommand(() => PlaybackRate.Value = 1.0);




            DownloadCompleted = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false);
			ProgressPercent = new ReactiveProperty<double>(PlayerWindowUIDispatcherScheduler, 0.0);
			IsFullScreen = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, false, ReactivePropertyMode.DistinctUntilChanged);
			IsFullScreen
				.Subscribe(isFullScreen => 
			{
				var appView = ApplicationView.GetForCurrentView();
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

            IsSmallWindowModeEnable = HohoemaApp.Playlist
                .ToReactivePropertyAsSynchronized(x => x.IsPlayerFloatingModeEnable);

            IsStillLoggedInTwitter = new ReactiveProperty<bool>(!TwitterHelper.IsLoggedIn)
                .AddTo(_CompositeDisposable);


            // playlist
            CurrentPlaylistName = new ReactiveProperty<string>(HohoemaApp.Playlist.CurrentPlaylist?.Name)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = HohoemaApp.UserSettings.PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable)
                .AddTo(_CompositeDisposable);



            IsTrackRepeatModeEnable = HohoemaApp.UserSettings.PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);
            IsListRepeatModeEnable = HohoemaApp.UserSettings.PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.List)
                .ToReactiveProperty()
                .AddTo(_CompositeDisposable);

            HohoemaApp.UserSettings.PlaylistSettings.PropertyChangedAsObservable()
                .Subscribe(_ => 
                {
                    HohoemaApp.UserSettings.PlaylistSettings.Save().ConfigureAwait(false);
                })
                .AddTo(_CompositeDisposable);


            IsTrackRepeatModeEnable.Subscribe(x => 
            {
                MediaPlayer.IsLoopingEnabled = x;
            })
                .AddTo(_CompositeDisposable);

            PlaylistCanGoBack = new ReactiveProperty<bool>(false);
            PlaylistCanGoNext = new ReactiveProperty<bool>(false);
            IsDisplayControlUI = HohoemaApp.Playlist.ToReactivePropertyAsSynchronized(x => x.IsDisplayPlayerControlUI);
        }

        protected override async Task OnOffline(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
        {
            // キャッシュから再生する
            if (HohoemaApp.ServiceStatus <= HohoemaAppServiceLevel.OnlineWithoutLoggedIn)
            {
                var playWithCache = false;
                // キャッシュされた動画から再生
                if (CurrentVideoQuality.Value == NicoVideoQuality.Original)
                {
                    if (Video.OriginalQuality.IsCached)
                    {
                        await PlayingQualityChangeAction();
                        playWithCache = true;
                    }
                }
                else
                {
                    if (Video.LowQuality.IsCached)
                    {
                        await PlayingQualityChangeAction();
                        playWithCache = true;
                    }
                }


                if (playWithCache)
                {
                    await UpdateComments();

                    ChangeRequireServiceLevel(HohoemaAppServiceLevel.Offline);
                }
                else
                {
                    // オフラインでは再生不可
                    IsNotSupportVideoType = true;
                    CannotPlayReason = "インターネット接続、及びニコニコ動画サービスへのログインが必要です";
                }

                // TODO : オフライン再生確定時にコメント投稿の無効化
            }


            if (Util.InputCapabilityHelper.IsMouseCapable)
            {
                IsAutoHideEnable = Observable.CombineLatest(
                    NowPlaying,
                    NowSoundChanging.Select(x => !x),
                    NowCommentWriting.Select(x => !x)
                    )
                .Select(x => x.All(y => y))
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(_CompositeDisposable);
            }
            else
            {
                IsAutoHideEnable = new ReactiveProperty<bool>(false);
            }

            AutoHideDelayTime = new ReactiveProperty<TimeSpan>(TimeSpan.FromSeconds(3));
            OnPropertyChanged(nameof(AutoHideDelayTime));


            IsMuted = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsMute, PlayerWindowUIDispatcherScheduler)
                .AddTo(userSessionDisposer);
            OnPropertyChanged(nameof(IsMuted));

            SoundVolume = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.SoundVolume, PlayerWindowUIDispatcherScheduler)
                .AddTo(userSessionDisposer);
            OnPropertyChanged(nameof(SoundVolume));

            CommentDefaultColor = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.CommentColor, PlayerWindowUIDispatcherScheduler)
                .AddTo(userSessionDisposer);
            OnPropertyChanged(nameof(CommentDefaultColor));


            Observable.Merge(
                IsMuted.ToUnit(),
                SoundVolume.ToUnit(),
                CommentDefaultColor.ToUnit()
                )
                .Throttle(TimeSpan.FromSeconds(5))
                .Where(x => !IsDisposed)
                .Subscribe(_ =>
                {
                    HohoemaApp.UserSettings.PlayerSettings.Save().ConfigureAwait(false);
                })
                .AddTo(userSessionDisposer);


            SoundVolume.Subscribe(volume => 
            {
                MediaPlayer.Volume = volume;
            });


            RequestUpdateInterval = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
                .Select(x => TimeSpan.FromSeconds(1.0 / x))
                .ToReactiveProperty()
                .AddTo(userSessionDisposer);
            OnPropertyChanged(nameof(RequestUpdateInterval));

            RequestCommentDisplayDuration = HohoemaApp.UserSettings.PlayerSettings
                .ObserveProperty(x => x.CommentDisplayDuration)
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(userSessionDisposer);
            OnPropertyChanged(nameof(RequestCommentDisplayDuration));

            CommentFontScale = HohoemaApp.UserSettings.PlayerSettings
                .ObserveProperty(x => x.DefaultCommentFontScale)
                .ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
                .AddTo(userSessionDisposer);
            OnPropertyChanged(nameof(CommentFontScale));


            HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.IsKeepDisplayInPlayback)
                .Subscribe(isKeepDisplay =>
                {
                    SetKeepDisplayWithCurrentState();
                })
                .AddTo(userSessionDisposer);


            IsForceLandscape = new ReactiveProperty<bool>(PlayerWindowUIDispatcherScheduler, HohoemaApp.UserSettings.PlayerSettings.IsForceLandscape);
            OnPropertyChanged(nameof(IsForceLandscape));


            // お気に入りフィード上の動画を既読としてマーク
            await HohoemaApp.FeedManager.MarkAsRead(Video.VideoId);
            await HohoemaApp.FeedManager.MarkAsRead(Video.RawVideoId);

            cancelToken.ThrowIfCancellationRequested();



            //            return base.OnOffline(userSessionDisposer, cancelToken);
        }

        protected override async Task OnSignIn(ICollection<IDisposable> userSessionDisposer, CancellationToken cancelToken)
		{
            var currentUIDispatcher = Window.Current.Dispatcher;

            // TODO: キャッシュから再生済みの場合にキャッシュの更新をキャンセルして
            // 動画情報の取得だけ行う

            try
            {
                var videoInfo = await HohoemaApp.MediaManager.GetNicoVideoAsync(VideoId);

                // 内部状態を更新
                await videoInfo.VisitWatchPage(NicoVideoQuality.Dmc_Mobile);

                // 動画が削除されていた場合
                if (videoInfo.IsDeleted)
                {
                    Debug.WriteLine($"cant playback{VideoId}. due to denied access to watch page, or connection offline.");

                    var dispatcher = Window.Current.CoreWindow.Dispatcher;

                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await Task.Delay(100);
                        PageManager.NavigationService.GoBack();

                        string toastContent = "";
                        if (!String.IsNullOrEmpty(videoInfo.Title))
                        {
                            toastContent = $"\"{videoInfo.Title}\" は削除された動画です";
                        }
                        else
                        {
                            toastContent = $"削除された動画です";
                        }
                        _ToastService.ShowText($"動画 {VideoId} は再生できません", toastContent);
                    })
                    .AsTask()
                    .ConfigureAwait(false);

                    VideoPlayed(canPlayNext: true);

                    // ローカルプレイリストの場合は勝手に消しておく
                    if (HohoemaApp.Playlist.CurrentPlaylist is LocalMylist)
                    {
                        if (HohoemaApp.Playlist.CurrentPlaylist != HohoemaApp.Playlist.DefaultPlaylist)
                        {
                            var item = HohoemaApp.Playlist.CurrentPlaylist.PlaylistItems.FirstOrDefault(x => x.ContentId == Video.RawVideoId);
                            if (item != null)
                            {
                                (HohoemaApp.Playlist.CurrentPlaylist as LocalMylist).Remove(item);
                            }
                        }
                    }

                    return;
                }

                cancelToken.ThrowIfCancellationRequested();

                // 有害動画へのアクセスに対して意思確認された場合
                if (videoInfo.IsBlockedHarmfulVideo)
                {
                    // 有害動画を視聴するか確認するページを表示
                    PageManager.OpenPage(HohoemaPageType.ConfirmWatchHurmfulVideo,
                        new VideoPlayPayload()
                        {
                            VideoId = VideoId,
                            Quality = Quality,
                        }
                        .ToParameterString()
                        );

                    return;
                }

                cancelToken.ThrowIfCancellationRequested();


                // ビデオタイプとプロトコルタイプをチェックする

                if (videoInfo.ProtocolType != MediaProtocolType.RTSPoverHTTP)
                {
                    // サポートしていないプロトコルです
                    IsNotSupportVideoType = true;
                    CannotPlayReason = videoInfo.ProtocolType.ToString() + " はHohoemaでサポートされないデータ通信形式です";

                    VideoPlayed(canPlayNext: true);
                }
                else
                {
                    IsNotSupportVideoType = false;
                    CannotPlayReason = "";
                }
            }
            catch (Exception exception)
            {
                // 動画情報の取得に失敗
                System.Diagnostics.Debug.Write(exception.Message);
                return;
            }

            cancelToken.ThrowIfCancellationRequested();


            // 全画面表示の設定を反映
            if (HohoemaApp.UserSettings.PlayerSettings.IsFullScreenDefault)
            {
                IsFullScreen.Value = true;
            }

            IsAvailableDmcHighQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_High).CanPlay;
            IsAvailableDmcMidiumQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Midium).CanPlay;
            IsAvailableDmcLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Low).CanPlay;
            IsAvailableDmcMobileQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Mobile).CanPlay;
            IsAvailableLegacyOriginalQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Original).CanPlay;
            IsAvailableLegacyLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Low).CanPlay;

            UpdateCache();

            ToggleCacheRequestCommand.RaiseCanExecuteChanged();

            cancelToken.ThrowIfCancellationRequested();

            if (IsNotSupportVideoType)
            {
                // コメント入力不可
                NowSubmittingComment.Value = true;
            }
            else
            {
                cancelToken.ThrowIfCancellationRequested();

                // コメント送信を有効に
                CanSubmitComment.Value = true;

                // コメントのコマンドエディタを初期化
                CommandEditerVM = new CommentCommandEditerViewModel()
                    .AddTo(userSessionDisposer);

                OnPropertyChanged(nameof(CommandEditerVM));

                CommandEditerVM.OnCommandChanged += () => UpdateCommandString();
                CommandEditerVM.IsPremiumUser = base.HohoemaApp.IsPremiumUser;

                CommandEditerVM.IsAnonymousDefault = HohoemaApp.UserSettings.PlayerSettings.IsDefaultCommentWithAnonymous;
                CommandEditerVM.IsAnonymousComment.Value = HohoemaApp.UserSettings.PlayerSettings.IsDefaultCommentWithAnonymous;

                // コミュニティやチャンネルの動画では匿名コメントは利用できない
                CommandEditerVM.ChangeEnableAnonymity(!(Video.IsCommunity || Video.IsChannel));

                UpdateCommandString();


                cancelToken.ThrowIfCancellationRequested();


                // PlayerSettings
                var playerSettings = HohoemaApp.UserSettings.PlayerSettings;
                IsCommentDisplayEnable.Value = playerSettings.DefaultCommentDisplay;



                cancelToken.ThrowIfCancellationRequested();

                
                // バッファリング状態のモニターが使うタイマーだけはページ稼働中のみ動くようにする
                InitializeBufferingMonitor();

                // 再生ストリームの準備を開始する
                await PlayingQualityChangeAction();

                cancelToken.ThrowIfCancellationRequested();

                // Note: 0.4.1現在ではキャッシュはmp4のみ対応
                var isCanCache = Video.ContentType == MovieType.Mp4;
                var isAcceptedCache = HohoemaApp.UserSettings?.CacheSettings?.IsUserAcceptedCache ?? false;
                var isEnabledCache = (HohoemaApp.UserSettings?.CacheSettings?.IsEnableCache ?? false) || IsSaveRequestedCurrentQualityCache.Value;

                CanDownload = isAcceptedCache && isEnabledCache && isCanCache;

                await UpdateComments();

                // 再生履歴に反映
                //VideoPlayHistoryDb.VideoPlayed(Video.RawVideoId);
            }


            IsPauseWithCommentWriting = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting, PlayerWindowUIDispatcherScheduler)
				.AddTo(userSessionDisposer);
			OnPropertyChanged(nameof(IsPauseWithCommentWriting));
		}


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

		private CompositeDisposable _BufferingMonitorDisposable;


		/// <summary>
		/// 再生処理
		/// </summary>
		/// <returns></returns>
		private async Task PlayingQualityChangeAction()
		{
			if (Video == null || IsDisposed) { IsSaveRequestedCurrentQualityCache.Value = false; return; }

            NowQualityChanging.Value = true;

            Video?.StopPlay();

            // サポートされたメディアの再生
            CurrentState.Value = MediaPlaybackState.Opening;

            try
            {
                CurrentVideoQuality.Value = await Video.StartPlay(RequestVideoQuality.Value, _PreviosPlayingVideoPosition);
            }
            catch (NotSupportedException ex)
            {
                IsNotSupportVideoType = true;
                CannotPlayReason = ex.Message;
                CurrentState.Value = MediaPlaybackState.None;

                VideoPlayed(canPlayNext: true);

                return;
            }



            if (IsDisposed)
			{
                Video?.StopPlay();
				return;
			}

			var isCurrentQualityCacheDownloadCompleted = false;
			switch (CurrentVideoQuality.Value)
			{
				case NicoVideoQuality.Original:
					IsSaveRequestedCurrentQualityCache.Value = Video.OriginalQuality.IsCacheRequested;
					isCurrentQualityCacheDownloadCompleted = Video.OriginalQuality.IsCached;
					break;
				case NicoVideoQuality.Low:
					IsSaveRequestedCurrentQualityCache.Value = Video.LowQuality.IsCacheRequested;
					isCurrentQualityCacheDownloadCompleted = Video.LowQuality.IsCached;
					break;
				default:
					IsSaveRequestedCurrentQualityCache.Value = false;
					break;
			}

			if (isCurrentQualityCacheDownloadCompleted)
			{
				// CachedStreamを使わずに直接ファイルから再生している場合
				// キャッシュ済みとして表示する
				IsPlayWithCache.Value = true;
			}
			else
			{ 
				// 完全なオンライン再生
				IsPlayWithCache.Value = false;
			}

            
            MediaPlayer.PlaybackSession.PlaybackRate = 
                HohoemaApp.UserSettings.PlayerSettings.DefaultPlaybackRate;

            // リクエストどおりの画質が再生された場合、画質をデフォルトとして設定する
            if (RequestVideoQuality.Value == CurrentVideoQuality.Value)
            {
                if (CurrentVideoQuality.Value.HasValue && CurrentVideoQuality.Value.Value.IsDmc())
                {
                    HohoemaApp.UserSettings.PlayerSettings.DefaultQuality = CurrentVideoQuality.Value.Value;
                    await HohoemaApp.UserSettings.PlayerSettings.Save().ConfigureAwait(false);
                }
            }

            if (!Util.InputCapabilityHelper.IsMouseCapable)
            {
                IsDisplayControlUI.Value = false;
            }
        }

		private void InitializeBufferingMonitor()
		{
			_BufferingMonitorDisposable?.Dispose();
			_BufferingMonitorDisposable = new CompositeDisposable();

			NowBuffering = 
				Observable.Merge(
                    CurrentState.ToUnit(),
                    DownloadCompleted.ToUnit()
                    )
					.Select(x =>
					{
						if (DownloadCompleted.Value) { return false; }

						if (CurrentState.Value == MediaPlaybackState.Paused)
						{
							return false;
						}

						if (CurrentState.Value == MediaPlaybackState.Buffering 
						|| CurrentState.Value == MediaPlaybackState.Opening)
						{
							return true;
						}

                        return false;
					}
				)
				.ObserveOnUIDispatcher()
				.ToReactiveProperty(PlayerWindowUIDispatcherScheduler)
				.AddTo(_BufferingMonitorDisposable);

			OnPropertyChanged(nameof(NowBuffering));
#if DEBUG
			NowBuffering
				.Subscribe(x => Debug.WriteLine(x ? "Buffering..." : "Playing..."))
				.AddTo(_BufferingMonitorDisposable);
#endif
			
		}


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

		



		static readonly ReadOnlyCollection<char> glassChars =
			new ReadOnlyCollection<char>(new char[] { 'w', 'ｗ', 'W', 'Ｗ' });		

		private Comment ChatToComment(Chat comment)
		{
			
			if (comment.Text == null)
			{
				return null;
			}

			var playerSettings = HohoemaApp.UserSettings.PlayerSettings;

			string commentText = comment.Text;

			// 自動芝刈り機
			if (playerSettings.CommentGlassMowerEnable)
			{
				foreach (var someGlassChar in glassChars)
				{
					if (commentText.Last() == someGlassChar)
					{
						commentText = new String(commentText.Reverse().SkipWhile(x => x == someGlassChar).Reverse().ToArray()) + someGlassChar;
						break;
					}
				}
			}

			
			var vpos_value = int.Parse(comment.Vpos);
			var vpos = vpos_value >= 0 ? (uint)vpos_value : 0;



			var commentVM = new Comment(this)
			{
				CommentText = commentText,
				CommentId = comment.GetCommentNo(),
				VideoPosition = vpos,
				EndPosition = vpos + default_DisplayTime,
			};


			commentVM.IsOwnerComment = comment.User_id != null ? comment.User_id == Video.OwnerId.ToString() : false;

			IEnumerable<CommandType> commandList = null;

			// コメントの装飾許可設定に従ってコメントコマンドの取得を行う
			var isAllowOwnerCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.Owner) == CommentCommandPermissionType.Owner;
			var isAllowUserCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.User) == CommentCommandPermissionType.User;
			var isAllowAnonymousCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.Anonymous) == CommentCommandPermissionType.Anonymous;
			if ((commentVM.IsOwnerComment && isAllowOwnerCommentCommnad)
				|| (comment.User_id != null && isAllowUserCommentCommnad)
				|| (comment.User_id == null && isAllowAnonymousCommentCommnad)
				)
			{
				try
				{
					commandList = comment.ParseCommandTypes();
					commentVM.ApplyCommands(commandList);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
					Debug.WriteLine(comment.Mail);
				}
			}
			
			
			return commentVM;
		}

        private Comment ChatToComment(NMSG_Chat chat)
        {

            if (chat.Content == null)
            {
                return null;
            }

            var playerSettings = HohoemaApp.UserSettings.PlayerSettings;

            string commentText = chat.Content;

            // 自動芝刈り機
            if (playerSettings.CommentGlassMowerEnable)
            {
                foreach (var someGlassChar in glassChars)
                {
                    if (commentText.Last() == someGlassChar)
                    {
                        commentText = new String(commentText.Reverse().SkipWhile(x => x == someGlassChar).Reverse().ToArray()) + someGlassChar;
                        break;
                    }
                }
            }


            var vpos_value = chat.Vpos;
            var vpos = vpos_value >= 0 ? (uint)vpos_value : 0;



            var commentVM = new Comment(this)
            {
                CommentText = commentText,
                CommentId = (uint)chat.No,
                VideoPosition = vpos,
                EndPosition = vpos + default_DisplayTime,
            };


            commentVM.IsOwnerComment = chat.UserId != null ? chat.UserId == Video.OwnerId.ToString() : false;

            IEnumerable<CommandType> commandList = null;

            // コメントの装飾許可設定に従ってコメントコマンドの取得を行う
            var isAllowOwnerCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.Owner) == CommentCommandPermissionType.Owner;
            var isAllowUserCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.User) == CommentCommandPermissionType.User;
            var isAllowAnonymousCommentCommnad = (playerSettings.CommentCommandPermission & CommentCommandPermissionType.Anonymous) == CommentCommandPermissionType.Anonymous;
            if ((commentVM.IsOwnerComment && isAllowOwnerCommentCommnad)
                || (chat.UserId != null && isAllowUserCommentCommnad)
                || (chat.UserId == null && isAllowAnonymousCommentCommnad)
                )
            {
                try
                {
                    commandList = chat.ParseCommandTypes();
                    commentVM.ApplyCommands(commandList);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(chat.Mail);
                }
            }


            return commentVM;
        }



        private void UpdateCommentNGStatus()
		{
			var ngSettings = HohoemaApp.UserSettings.NGSettings;
			foreach (var comment in Comments)
			{
				if (comment.UserId != null)
				{
					var userNg = ngSettings.IsNGCommentUser(comment.UserId);
					if (userNg != null)
					{
						comment.NgResult = userNg;
						continue;
					}
				}

				var keywordNg = ngSettings.IsNGComment(comment.CommentText);
				if (keywordNg != null)
				{
					comment.NgResult = keywordNg;
					continue;
				}

				comment.NgResult = null;
			}
		}		

		protected override async Task NavigatedToAsync(CancellationToken cancelToken, NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
		{
			Debug.WriteLine("VideoPlayer OnNavigatedToAsync start.");

            if (e?.Parameter is string)
			{
				var payload = VideoPlayPayload.FromParameterString(e.Parameter as string);
				VideoId = payload.VideoId;
                RequestVideoQuality.Value = payload.Quality;
			}
			else if (viewModelState.ContainsKey(nameof(VideoId)))
			{
				VideoId = (string)viewModelState[nameof(VideoId)];
			}

            

            cancelToken.ThrowIfCancellationRequested();

            var videoInfo = await HohoemaApp.MediaManager.GetNicoVideoAsync(VideoId);

            Video = videoInfo;

            Title = Video.Title;
            VideoTitle = Title;
            VideoLength.Value = Video.VideoLength.TotalSeconds;
            CurrentVideoPosition.Value = TimeSpan.Zero;

            cancelToken.ThrowIfCancellationRequested();

            if (viewModelState.ContainsKey(nameof(CurrentVideoPosition)))
            {
                CurrentVideoPosition.Value = TimeSpan.FromSeconds((double)viewModelState[nameof(CurrentVideoPosition)]);
            }


            if (HohoemaApp.Playlist.CurrentPlaylist == null)
            {
                throw new Exception();
            }

            ThumbnailUri.Value = Video.ThumbnailUrl;
            CurrentPlaylist = HohoemaApp.Playlist.CurrentPlaylist;
            _CurrentPlayingItem = HohoemaApp.Playlist.Player.Current;

            CurrentPlaylistName.Value = CurrentPlaylist.Name;
            PlaylistItems = CurrentPlaylist.PlaylistItems.ToReadOnlyReactiveCollection();
            OnPropertyChanged(nameof(PlaylistItems));

            HohoemaApp.MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            HohoemaApp.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;
            

            Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");

            // 基本的にオンラインで再生、
            // オフラインの場合でキャッシュがあるようならキャッシュで再生できる
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.LoggedIn);

            App.Current.Suspending += Current_Suspending;
            App.Current.LeavingBackground += Current_LeavingBackground;
            App.Current.EnteredBackground += Current_EnteredBackground;

            PlaylistCanGoBack.Value = HohoemaApp.Playlist.Player.CanGoBack;
            PlaylistCanGoNext.Value = HohoemaApp.Playlist.Player.CanGoNext;
        }

        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
        }

        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            PlayerWindowUIDispatcherScheduler.Schedule(() =>
            {
                if (IsDisposed) { return; }
                RequestUpdateInterval.ForceNotify();
            });
        }

        private void PlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
        {
            if (IsDisposed) { return; }

            if (sender.PlaybackState == MediaPlaybackState.Playing)
            {
                ReadVideoPosition.Value = sender.Position;
            }
        }

        private void PlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
        {
            Debug.WriteLine(sender.PlaybackState);

            PlayerWindowUIDispatcherScheduler.Schedule(() => 
            {
                if (IsDisposed) { return; }

                CurrentState.Value = sender.PlaybackState;
            });

            // 最後まで到達していた場合
            if (sender.PlaybackState == MediaPlaybackState.Paused
                && sender.Position >= (Video.VideoLength - TimeSpan.FromSeconds(1))
                )
            {
                VideoPlayed(canPlayNext:true);
            }
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            //            PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

            _PreviosPlayingVideoPosition = TimeSpan.FromSeconds(SliderVideoPosition.Value);

            _IsNeddPlayInResumed = this.CurrentState.Value != MediaPlaybackState.Paused
                && this.CurrentState.Value != MediaPlaybackState.None;

            IsFullScreen.Value = false;

            ExitKeepDisplay();

            HohoemaApp.MediaPlayer.Pause();
            HohoemaApp.MediaPlayer.Source = null;


            _BufferingMonitorDisposable?.Dispose();
            _BufferingMonitorDisposable = new CompositeDisposable();
        }

        private async Task UpdateComments()
        {
            Comments.Clear();

            bool isSuccessGetCommentFromNMSG = false;
            // 新コメサーバーからコメント取得
            if (Video.CommentClient.CanGetCommentsFromNMSG)
            {
                try
                {
                    var res = await Video.CommentClient.GetCommentsFromNMSG();

                    foreach (var chat in res.ParseComments())
                    {
                        var comment = ChatToComment(chat);
                        if (comment != null)
                        {
                            Comments.Add(comment);
                        }
                    }

                    isSuccessGetCommentFromNMSG = true;
                }
                catch
                {
                }
            }
            
            // 新コメサーバーがダメだったら旧サーバーから取得
            if (!isSuccessGetCommentFromNMSG)
            { 
                List<Chat> oldFormatComments = null;
                try
                {
                    oldFormatComments = await Video.CommentClient.GetComments();
                    if (oldFormatComments == null || oldFormatComments.Count == 0)
                    {
                        oldFormatComments = Video.CommentClient.GetCommentsFromLocal();
                    }
                }
                catch
                {
                    oldFormatComments = Video.CommentClient.GetCommentsFromLocal();
                }

                if (oldFormatComments == null)
                {
                    System.Diagnostics.Debug.WriteLine("コメントは取得できませんでした");
                    return;
                }

                foreach (var chat in oldFormatComments)
                {
                    var comment = ChatToComment(chat);
                    if (comment != null)
                    {
                        Comments.Add(comment);
                    }
                }
            }

            

            UpdateCommentNGStatus();

            System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");
        }

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

        protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
		{
			Debug.WriteLine("VideoPlayer OnNavigatingFromAsync start.");

//			PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

			if (suspending)
			{
				viewModelState[nameof(VideoId)] = VideoId;
				viewModelState[nameof(CurrentVideoPosition)] = CurrentVideoPosition.Value.TotalSeconds;
            }
			else 
			{
                App.Current.Suspending -= Current_Suspending;
                HohoemaApp.MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                HohoemaApp.MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;

                Comments.Clear();
            }

            Video?.StopPlay();

            // プレイリストへ再生完了を通知
            VideoPlayed();

            ExitKeepDisplay();

			_BufferingMonitorDisposable?.Dispose();
			_BufferingMonitorDisposable = new CompositeDisposable();

			base.OnHohoemaNavigatingFrom(e, viewModelState, suspending);


			Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");
		}


        bool _IsVideoPlayed = false;
        private void VideoPlayed(bool canPlayNext = false)
        {
            if (_IsVideoPlayed == false)
            {
                // Note: 次の再生用VMの作成を現在ウィンドウのUIDsipatcher上で行わないと同期コンテキストが拾えず再生に失敗する
                // VideoPlayedはMediaPlayerが動作しているコンテキスト上から呼ばれる可能性がある
                PlayerWindowUIDispatcherScheduler.Schedule(() => 
                {
                    HohoemaApp.Playlist.PlayDone(_CurrentPlayingItem, canPlayNext);
                });

                _IsVideoPlayed = true;
            }
        }



		protected override void OnDispose()
		{
			base.OnDispose();

            Video?.StopPlay();

            _BufferingMonitorDisposable?.Dispose();
		}



		private async Task SubmitComment()
		{
            if (Video?.CommentClient == null) { return; }

            Debug.WriteLine($"try comment submit:{WritingComment.Value}");
            
			NowSubmittingComment.Value = true;
			try
			{
				var vpos = (uint)(ReadVideoPosition.Value.TotalMilliseconds / 10);
				var commands = CommandString.Value;
				var res = await Video.CommentClient.SubmitComment(WritingComment.Value, ReadVideoPosition.Value, commands);

				if (res.Chat_result.Status == ChatResult.Success)
				{
					_ToastService.ShowText("コメント投稿", $"{VideoId}に「{WritingComment.Value}」を投稿しました", isSuppress:true);

					Debug.WriteLine("コメントの投稿に成功: " + res.Chat_result.No);

					var commentVM = new Comment(this)
					{
						CommentId = (uint)res.Chat_result.No,
						VideoPosition = vpos,
						EndPosition = vpos + default_DisplayTime,
						UserId = base.HohoemaApp.LoginUserId.ToString(),
						CommentText = WritingComment.Value,
					};

					var commentCommands = CommandEditerVM.MakeCommands();

					commentVM.ApplyCommands(commentCommands);

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
					Debug.WriteLine("コメントの投稿に失敗: " + res.Chat_result.Status.ToString());
				}

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
				CommandString.Value = "コマンド";
			}
			else
			{
				CommandString.Value = str;
			}
		}




        private void UpdateCache()
        {
            IsCacheLegacyOriginalQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Original).IsCacheRequested;
            IsCacheLegacyLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Low).IsCacheRequested;

            CanToggleCacheRequestLegacyOriginalQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Original).CanRequestCache || IsCacheLegacyOriginalQuality.Value;
            CanToggleCacheRequestLegacyLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Low).CanRequestCache || IsCacheLegacyLowQuality.Value;
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

        private DelegateCommand _ForwardVideoPositionCommand;
        public DelegateCommand ForwardVideoPositionCommand
        {
            get
            {
                return _ForwardVideoPositionCommand
                    ?? (_ForwardVideoPositionCommand = new DelegateCommand(() =>
                    {
                        var session = MediaPlayer.PlaybackSession;
                        var time = session.Position + TimeSpan.FromSeconds(30);
                        session.Position = time;
                    }));
            }
        }

        private DelegateCommand _PreviewVideoPositionCommand;
        public DelegateCommand PreviewVideoPositionCommand
        {
            get
            {
                return _PreviewVideoPositionCommand
                    ?? (_PreviewVideoPositionCommand = new DelegateCommand(() =>
                    {
                        var session = MediaPlayer.PlaybackSession;
                        var time = session.Position - TimeSpan.FromSeconds(10);
                        session.Position = time;
                    }));
            }
        }


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
						var amount = HohoemaApp.UserSettings.PlayerSettings.ScrollVolumeFrequency;
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
						var amount = HohoemaApp.UserSettings.PlayerSettings.ScrollVolumeFrequency;
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
                        IsVisibleComment.Value = !IsVisibleComment.Value;
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
		public ReactiveCommand<NicoVideoQuality> TogglePlayQualityCommand { get; private set; }

        private DelegateCommand _OpenVideoPageWithBrowser;
		public DelegateCommand OpenVideoPageWithBrowser
		{
			get
			{
				return _OpenVideoPageWithBrowser
					?? (_OpenVideoPageWithBrowser = new DelegateCommand(async () =>
					{
						var watchPageUri = Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl + Video.RawVideoId;
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

        private DelegateCommand _PlayerSmallWindowDisplayCommand;
        public DelegateCommand PlayerSmallWindowDisplayCommand
        {
            get
            {
                return _PlayerSmallWindowDisplayCommand
                    ?? (_PlayerSmallWindowDisplayCommand = new DelegateCommand(() =>
                    {
                        HohoemaApp.Playlist.IsPlayerFloatingModeEnable = true;
                    }
                    ));
            }
        }

        private DelegateCommand _OpenPlayerSettingCommand;
        public DelegateCommand OpenPlayerSettingCommand
        {
            get
            {
                return _OpenPlayerSettingCommand
                    ?? (_OpenPlayerSettingCommand = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.Settings, HohoemaSettingsKind.Player.ToString());
                        HohoemaApp.Playlist.IsPlayerFloatingModeEnable = true;
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
                        PageManager.OpenPage(HohoemaPageType.VideoInfomation, Video.RawVideoId);
                        HohoemaApp.Playlist.IsPlayerFloatingModeEnable = true;
                    }
                    ));
            }
        }


        public ReactiveProperty<bool> IsStillLoggedInTwitter { get; private set; }

        private DelegateCommand _ShereWithTwitterCommand;
        public DelegateCommand ShereWithTwitterCommand
        {
            get
            {
                return _ShereWithTwitterCommand
                    ?? (_ShereWithTwitterCommand = new DelegateCommand(async () =>
                    {
                        if (!TwitterHelper.IsLoggedIn)
                        {

                            if (!await TwitterHelper.LoginOrRefreshToken())
                            {
                                return;
                            }
                        }

                        IsStillLoggedInTwitter.Value = !TwitterHelper.IsLoggedIn;

                        if (TwitterHelper.IsLoggedIn)
                        {
                            var text = $"{Video.Title} http://nico.ms/{Video.VideoId} #{Video.VideoId}";
                            var twitterLoginUserName = TwitterHelper.TwitterUser.ScreenName;
                            var customText = await _TextInputDialogService.GetTextAsync($"{twitterLoginUserName} としてTwitterへ投稿", "", text);

                            if (customText != null)
                            {
                                var result = await TwitterHelper.SubmitTweet(customText);

                                if (!result)
                                {
                                    _ToastService.ShowText("ツイートに失敗しました", "もう一度お試しください");
                                }
                            }
                        }
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
                        var videoUrl = $"http://nico.ms/{Video.VideoId}";
                        var text = $"{Video.Title} {videoUrl} #{Video.VideoId}";
                        var datapackage = new DataPackage();
                        datapackage.SetText(text);
                        datapackage.SetWebLink(new Uri(videoUrl));

                        Clipboard.SetContent(datapackage);
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
                        var groupAndComment = await _MylistResistrationDialogService.ShowDialog(1);
                        if (groupAndComment != null)
                        {
                            await groupAndComment.Item1.Registration(VideoId, groupAndComment.Item2);
                        }
                    }
                    ));
            }
        }



        private DelegateCommand _ClosePlayerCommand;
        public DelegateCommand ClosePlayerCommand
        {
            get
            {
                return _ClosePlayerCommand
                    ?? (_ClosePlayerCommand = new DelegateCommand(() =>
                    {
                        HohoemaApp.Playlist.IsDisplayPlayer = false;
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
                            var divided = Video.GetDividedQualityNicoVideo(quality);
                            if (divided != null)
                            {
                                if (divided.IsCacheRequested)
                                {
                                    var qualityText = new Views.Converters.NicoVideoQualityToCultualizedTextConverter().Convert(quality, typeof(string), null, null);

                                    var dialog = new MessageDialog($"{VideoTitle} の キャッシュデータ（{qualityText}画質）を削除します。この操作は元に戻せません。", "キャッシュ削除の確認");
                                    dialog.Commands.Add(new UICommand("キャッシュを削除") { Id = "delete" } );
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

                                        await Video.CancelCacheRequest(quality);
                                    }
                                }
                                else
                                {
                                    await Video.RequestCache(quality);
                                }

                                UpdateCache();
                            }
                        }
                    }, (qualityName) =>
                    {
                        if (Enum.TryParse<NicoVideoQuality>(qualityName, out var quality))
                        {

                            var divided = Video.GetDividedQualityNicoVideo(quality);
                            if (divided.IsCacheRequested)
                            {
                                return true;
                            }
                            else
                            {
                                return divided.CanDownload;
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
                        var player = HohoemaApp.Playlist.Player;
                        if (player != null)
                        {
                            // 先に_IsVideoPlayed をtrueにしておくことで
                            // NavigatingFromで再生完了が呼ばれた時にPlaylist.PlayDoneが多重呼び出しされないようにする
                            _IsVideoPlayed = true;

                            HohoemaApp.Playlist.PlayDone(_CurrentPlayingItem);

                            if (player.CanGoBack)
                            {
                                player.GoBack();
                            }
                        }
                    }
                    , () => HohoemaApp.Playlist.Player?.CanGoBack ?? false
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
                        var player = HohoemaApp.Playlist.Player;
                        if (player != null)
                        {
                            // 先に_IsVideoPlayed をtrueにしておくことで
                            // NavigatingFromで再生完了が呼ばれた時にPlaylist.PlayDoneが多重呼び出しされないようにする
                            _IsVideoPlayed = true;

                            HohoemaApp.Playlist.PlayDone(_CurrentPlayingItem, canPlayNext:true);
                        }
                    }
                    , () => HohoemaApp.Playlist.Player?.CanGoNext ?? false
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
                        HohoemaApp.Playlist.Play(item);
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
                        HohoemaApp.Playlist.IsPlayerFloatingModeEnable = true;

                        PageManager.OpenPage(HohoemaPageType.Mylist,
                            new MylistPagePayload(HohoemaApp.Playlist.CurrentPlaylist).ToParameterString()
                            );
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
                        var playlistSettings = HohoemaApp.UserSettings.PlaylistSettings;
                        switch (playlistSettings.RepeatMode)
                        {
                            case MediaPlaybackAutoRepeatMode.None:
                                playlistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.Track;
                                break;
                            case MediaPlaybackAutoRepeatMode.Track:
                                playlistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.List;
                                break;
                            case MediaPlaybackAutoRepeatMode.List:
                                playlistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.None;
                                break;
                            default:
                                break;
                        }

                        PlaylistCanGoBack.Value = HohoemaApp.Playlist.Player.CanGoBack;
                        PlaylistCanGoNext.Value = HohoemaApp.Playlist.Player.CanGoNext;
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
                        HohoemaApp.UserSettings.PlaylistSettings.IsShuffleEnable = !HohoemaApp.UserSettings.PlaylistSettings.IsShuffleEnable;

                        PlaylistCanGoBack.Value = HohoemaApp.Playlist.Player.CanGoBack;
                        PlaylistCanGoNext.Value = HohoemaApp.Playlist.Player.CanGoNext;
                    }
                    ));
            }
        }


        #endregion




        #region player settings method


        void SetKeepDisplayIfEnable()
		{
			ExitKeepDisplay();

//			if (HohoemaApp.UserSettings.PlayerSettings.IsKeepDisplayInPlayback)
			{
				DisplayRequestHelper.RequestKeepDisplay();
			}
		}

		void ExitKeepDisplay()
		{
			DisplayRequestHelper.StopKeepDisplay();
		}



		



		#endregion


		private NicoVideo _Video;
		public NicoVideo Video
		{
			get { return _Video; }
			set { SetProperty(ref _Video, value); }
		}

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


        private string _VideoTitle;
        public string VideoTitle
        {
            get { return _VideoTitle; }
            set { SetProperty(ref _VideoTitle, value); }
        }



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
        public ReactiveProperty<NicoVideoQuality?> RequestVideoQuality { get; private set; }
        public ReactiveProperty<bool> CanToggleCurrentQualityCacheState { get; private set; }
		public ReactiveProperty<bool> IsSaveRequestedCurrentQualityCache { get; private set; }

        public ReactiveProperty<bool> IsAvailableDmcHighQuality { get; private set; }
        public ReactiveProperty<bool> IsAvailableDmcMidiumQuality { get; private set; }
        public ReactiveProperty<bool> IsAvailableDmcLowQuality { get; private set; }
        public ReactiveProperty<bool> IsAvailableDmcMobileQuality { get; private set; }

        public ReactiveProperty<bool> IsAvailableLegacyOriginalQuality { get; private set; }
        public ReactiveProperty<bool> IsAvailableLegacyLowQuality { get; private set; }

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

		public ReactiveProperty<double> SliderVideoPosition { get; private set; }
		public ReactiveProperty<double> VideoLength { get; private set; }
		public ReactiveProperty<MediaPlaybackState> CurrentState { get; private set; }
        public ReactiveProperty<MediaElementState> LegacyCurrentState { get; private set; }
        public ReactiveProperty<bool> NowBuffering { get; private set; }
		public ReactiveProperty<bool> NowPlaying { get; private set; }
		public ReactiveProperty<bool> NowQualityChanging { get; private set; }
		public ReactiveProperty<bool> IsEnableRepeat { get; private set; }
        public ReactiveProperty<double> PlaybackRate { get; private set; }
        public DelegateCommand ResetDefaultPlaybackRate { get; private set; }

        public ReactiveProperty<bool> IsAutoHideEnable { get; private set; }
        public ReactiveProperty<TimeSpan> AutoHideDelayTime { get; private set; }

        public ReactiveProperty<bool> IsDisplayControlUI { get; private set; }

        private TimeSpan _PreviosPlayingVideoPosition;

        private bool _IsNeddPlayInResumed;

		// Sound
		public ReactiveProperty<bool> NowSoundChanging { get; private set; }
		public ReactiveProperty<bool> IsMuted { get; private set; }
		public ReactiveProperty<double> SoundVolume { get; private set; }

		// Settings
		public ReactiveProperty<TimeSpan> RequestUpdateInterval { get; private set; }
		public ReactiveProperty<TimeSpan> RequestCommentDisplayDuration { get; private set; }
		public ReactiveProperty<double> CommentFontScale { get; private set; }
		public ReactiveProperty<bool> IsFullScreen { get; private set; }
        public ReactiveProperty<bool> IsSmallWindowModeEnable { get; private set; }
        public ReactiveProperty<bool> IsForceLandscape { get; private set; }




        public ReactiveProperty<bool> CanSubmitComment { get; private set; }
        public ReactiveProperty<bool> NowSubmittingComment { get; private set; }
		public ReactiveProperty<string> WritingComment { get; private set; }
		public ReactiveProperty<bool> IsVisibleComment { get; private set; }
        public ReactiveProperty<bool> IsCommentDisplayEnable { get; private set; }
        public ReactiveProperty<bool> NowCommentWriting { get; private set; }
		public ObservableCollection<Comment> Comments { get; private set; }
		public ReactiveProperty<bool> IsPauseWithCommentWriting { get; private set; }
		public ReactiveProperty<bool> IsNeedResumeExitWrittingComment { get; private set; }
		public ReactiveProperty<double> CommentCanvasHeight { get; private set; }
		public ReactiveProperty<double> CommentCanvasWidth { get; private set; }
		public ReactiveProperty<Color> CommentDefaultColor { get; private set; }
        public ReadOnlyReactiveProperty<double> CommentOpacity { get; private set; }


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


        // プレイリスト
        public IPlayableList CurrentPlaylist { get; private set; }
        public ReactiveProperty<string> CurrentPlaylistName { get; private set; }
        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool?> RepeatMode { get; private set; }
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistItem> PlaylistItems { get; private set; }



        ToastNotificationService _ToastService;
		TextInputDialogService _TextInputDialogService;
        MylistRegistrationDialogService _MylistResistrationDialogService;


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

		    HohoemaApp.UserSettings.NGSettings.NGCommentUserIds.Add(new UserIdInfo()
			{
				UserId = commentViewModel.UserId,
				Description = userName
			});

			UpdateCommentNGStatus();

			return Task.CompletedTask;
		}
	}


	


	

	public class TagViewModel
	{
		public string TagText { get; private set; }
		public bool IsCategoryTag { get; private set; }
		public bool IsLock { get; private set; }
		PageManager _PageManager;


		public TagViewModel(string tag, PageManager pageManager)
		{
			_PageManager = pageManager;

			TagText = tag;
			IsCategoryTag = false;
			IsLock = false;
		}

		public TagViewModel(ThumbnailTag tag, PageManager pageManager)
		{
			_PageManager = pageManager;

			TagText = tag.Value;
			IsCategoryTag = tag.Category;
			IsLock = tag.Lock;
		}


		private DelegateCommand _OpenSearchPageWithTagCommand;
		public DelegateCommand OpenSearchPageWithTagCommand
		{
			get
			{
				return _OpenSearchPageWithTagCommand
					?? (_OpenSearchPageWithTagCommand = new DelegateCommand(() => 
					{
						var payload = 
							new Models.TagSearchPagePayloadContent()
							{
								Keyword = TagText,
								Sort = Sort.FirstRetrieve,
								Order = Order.Descending
							}
							;

                        _PageManager.Search(payload);
					}));
			}
		}


		private DelegateCommand _OpenTagDictionaryInBrowserCommand;
		public DelegateCommand OpenTagDictionaryInBrowserCommand
		{
			get
			{
				return _OpenTagDictionaryInBrowserCommand
					?? (_OpenTagDictionaryInBrowserCommand = new DelegateCommand(() =>
					{
						// TODO: 
					}));
			}
		}

	}


	public enum MediaInfoDisplayType
	{
		Summary,
		Mylist,
		Comment,
		Shere,
		Settings,
	}










}

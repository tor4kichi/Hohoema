using Mntone.Nico2;
using Mntone.Nico2.Videos.Comment;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Util;
using NicoPlayerHohoema.ViewModels.PlayerSidePaneContent;
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
using Windows.ApplicationModel.Core;
using Windows.Foundation.Metadata;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.ViewModels
{
	public class VideoPlayerPageViewModel : HohoemaViewModelBase, IDisposable
	{
		// TODO: HohoemaViewModelBaseとの依存性を排除（ViewModelBaseとの関係性は維持）



		const uint default_DisplayTime = 400; // 1 = 10ms, 400 = 4000ms = 4.0 Seconds

        public bool IsXbox => Util.DeviceTypeHelper.IsXbox;

		public VideoPlayerPageViewModel(
			HohoemaApp hohoemaApp, 
			EventAggregator ea,
			PageManager pageManager, 
			ToastNotificationService toast,
			TextInputDialogService textInputDialog
			)
			: base(hohoemaApp, pageManager)
		{
			_ToastService = toast;
			_TextInputDialogService = textInputDialog;

            MediaPlayer = new MediaPlayer();

            MediaPlayer.AutoPlay = true;
            MediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;

            ThumbnailUri = new ReactiveProperty<string>(CurrentWindowContextScheduler);

            CurrentVideoPosition = new ReactiveProperty<TimeSpan>(CurrentWindowContextScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			ReadVideoPosition = new ReactiveProperty<TimeSpan>(CurrentWindowContextScheduler, TimeSpan.Zero);
//				.AddTo(_CompositeDisposable);
			CommentVideoPosition = new ReactiveProperty<TimeSpan>(CurrentWindowContextScheduler, TimeSpan.Zero)
				.AddTo(_CompositeDisposable);
			NowSubmittingComment = new ReactiveProperty<bool>(CurrentWindowContextScheduler)
				.AddTo(_CompositeDisposable);
			SliderVideoPosition = new ReactiveProperty<double>(CurrentWindowContextScheduler, 0, mode:ReactivePropertyMode.DistinctUntilChanged)
				.AddTo(_CompositeDisposable);
			VideoLength = new ReactiveProperty<double>(CurrentWindowContextScheduler, 0)
				.AddTo(_CompositeDisposable);
			CurrentState = new ReactiveProperty<MediaPlaybackState>(CurrentWindowContextScheduler)
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

            NowQualityChanging = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
			Comments = new ObservableCollection<Comment>();

            CanSubmitComment = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            NowCommentWriting = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
				.AddTo(_CompositeDisposable);
			NowSoundChanging = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
				.AddTo(_CompositeDisposable);
            IsCommentDisplayEnable = HohoemaApp.UserSettings.PlayerSettings.ToReactivePropertyAsSynchronized(x => x.CommentDisplay_Video, CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            // プレイヤーがフィル表示かつコメント表示を有効にしている場合のみ表示
            IsVisibleComment =
                Observable.CombineLatest(
                    HohoemaApp.Playlist.ObserveProperty(x => x.IsPlayerFloatingModeEnable).Select(x => !x),
                    IsCommentDisplayEnable
                    )
                    .Select(x => x.All(y => y))
                    .ToReactiveProperty(CurrentWindowContextScheduler);

			IsEnableRepeat = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
				.AddTo(_CompositeDisposable);
			
			WritingComment = new ReactiveProperty<string>(CurrentWindowContextScheduler, "")
				.AddTo(_CompositeDisposable);

			CommentSubmitCommand = WritingComment.Select(x => !string.IsNullOrWhiteSpace(x))
				.ToReactiveCommand(CurrentWindowContextScheduler)
				.AddTo(_CompositeDisposable);

			CommentSubmitCommand.Subscribe(async x => await SubmitComment())
				.AddTo(_CompositeDisposable);

			NowCommentWriting.Subscribe(x => Debug.WriteLine("NowCommentWriting:" + NowCommentWriting.Value))
				.AddTo(_CompositeDisposable);

			IsPlayWithCache = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
				.AddTo(_CompositeDisposable);

			IsNeedResumeExitWrittingComment = new ReactiveProperty<bool>(CurrentWindowContextScheduler);

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

			CommandString = new ReactiveProperty<string>(CurrentWindowContextScheduler, "")
				.AddTo(_CompositeDisposable);

			CommentCanvasHeight = new ReactiveProperty<double>(CurrentWindowContextScheduler, 0);
			CommentCanvasWidth = new ReactiveProperty<double>(CurrentWindowContextScheduler, 0);

            CommentOpacity = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentOpacity)
                .Select(x => x.ToOpacity())
                .ToReadOnlyReactiveProperty(eventScheduler: CurrentWindowContextScheduler);





            CurrentVideoQuality = new ReactiveProperty<NicoVideoQuality?>(CurrentWindowContextScheduler, null, ReactivePropertyMode.None)
				.AddTo(_CompositeDisposable);
            RequestVideoQuality = new ReactiveProperty<NicoVideoQuality?>(CurrentWindowContextScheduler, null, ReactivePropertyMode.None)
                .AddTo(_CompositeDisposable);

            IsAvailableDmcHighQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
                .AddTo(_CompositeDisposable);
            IsAvailableDmcMidiumQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
                .AddTo(_CompositeDisposable);
            IsAvailableDmcLowQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
                            .AddTo(_CompositeDisposable);
            IsAvailableDmcMobileQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
                            .AddTo(_CompositeDisposable);
            IsAvailableLegacyOriginalQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
                            .AddTo(_CompositeDisposable);
            IsAvailableLegacyLowQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false)
                            .AddTo(_CompositeDisposable);


            IsCacheLegacyOriginalQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false, mode: ReactivePropertyMode.None);
            IsCacheLegacyLowQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false, mode:ReactivePropertyMode.None);

            CanToggleCacheRequestLegacyOriginalQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            CanToggleCacheRequestLegacyLowQuality = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            
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
				.ToReactiveProperty(CurrentWindowContextScheduler)
				.AddTo(_CompositeDisposable);

			IsSaveRequestedCurrentQualityCache = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false, ReactivePropertyMode.DistinctUntilChanged)
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



			TogglePlayQualityCommand = new ReactiveCommand<NicoVideoQuality>(CurrentWindowContextScheduler)
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
				.ToReactiveProperty(CurrentWindowContextScheduler)
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
            PlaybackRate = HohoemaApp.UserSettings.PlayerSettings.ToReactivePropertyAsSynchronized(x => x.PlaybackRate, CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            PlaybackRate.Subscribe(x =>
            {
                MediaPlayer.PlaybackSession.PlaybackRate = x;
            })
            .AddTo(_CompositeDisposable);

            SetPlaybackRateCommand = new DelegateCommand<double?>(
                (rate) => PlaybackRate.Value = rate.HasValue ? rate.Value : 1.0                
                , (rate) => rate.HasValue ? rate.Value != PlaybackRate.Value : true
            );




            DownloadCompleted = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
			ProgressPercent = new ReactiveProperty<double>(CurrentWindowContextScheduler, 0.0);
			IsFullScreen = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false, ReactivePropertyMode.DistinctUntilChanged);
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


            // プレイヤーを閉じた際のコンパクトオーバーレイの解除はPlayerWithPageContainerViewModel側で行う
            

            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
            {
                IsCompactOverlay = new ReactiveProperty<bool>(CurrentWindowContextScheduler,
                    ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay,
                    ReactivePropertyMode.DistinctUntilChanged);

                // This device supports all APIs in UniversalApiContract version 2.0
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
                                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
                                appView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                                appView.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                                IsDisplayControlUI.Value = false;
                            }
                        }
                        else
                        {
                            var result = await appView.TryEnterViewModeAsync(ApplicationViewMode.Default);
                            if (result)
                            {
                                CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
                                appView.TitleBar.ButtonBackgroundColor = null;
                                appView.TitleBar.ButtonInactiveBackgroundColor = null;
                            }
                        }
                    }
                })
                .AddTo(_CompositeDisposable);
            }
            else
            {
                IsCompactOverlay = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            }
            


            IsSmallWindowModeEnable = HohoemaApp.Playlist
                .ObserveProperty(x => x.IsPlayerFloatingModeEnable)
                .ToReadOnlyReactiveProperty(eventScheduler: CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);


            // playlist
            CurrentPlaylistName = new ReactiveProperty<string>(CurrentWindowContextScheduler, HohoemaApp.Playlist.CurrentPlaylist?.Name)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = HohoemaApp.UserSettings.PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);



            IsTrackRepeatModeEnable = HohoemaApp.UserSettings.PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            IsListRepeatModeEnable = HohoemaApp.UserSettings.PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.List)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            IsTrackRepeatModeEnable.Subscribe(x => 
            {
                MediaPlayer.IsLoopingEnabled = x;
            })
                .AddTo(_CompositeDisposable);

            IsDisplayControlUI = HohoemaApp.Playlist.ToReactivePropertyAsSynchronized(x => x.IsDisplayPlayerControlUI, CurrentWindowContextScheduler);

            PlaylistCanGoBack = HohoemaApp.Playlist.Player.ObserveProperty(x => x.CanGoBack).ToReactiveProperty(CurrentWindowContextScheduler);
            PlaylistCanGoNext = HohoemaApp.Playlist.Player.ObserveProperty(x => x.CanGoNext).ToReactiveProperty(CurrentWindowContextScheduler);


            CurrentSidePaneContentType = new ReactiveProperty<PlayerSidePaneContentType?>(CurrentWindowContextScheduler, initialValue: _PrevSidePaneContentType)
                .AddTo(_CompositeDisposable);
            CurrentSidePaneContent = CurrentSidePaneContentType
                .Select(GetSidePaneContent)
                .ToReadOnlyReactiveProperty(eventScheduler:CurrentWindowContextScheduler)
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


            if (Util.InputCapabilityHelper.IsMouseCapable && !IsForceTVModeEnable.Value)
            {
                IsAutoHideEnable = Observable.CombineLatest(
                    NowPlaying,
                    NowSoundChanging.Select(x => !x),
                    NowCommentWriting.Select(x => !x)
                    )
                .Select(x => x.All(y => y))
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

                IsMouseCursolAutoHideEnable = Observable.CombineLatest(
                    IsDisplayControlUI.Select(x => !x),
                    IsSmallWindowModeEnable.Select(x => !x)
                    )
                    .Select(x => x.All(y => y))
                    .ToReactiveProperty(CurrentWindowContextScheduler)
                    .AddTo(_CompositeDisposable);
            }
            else
            {
                IsAutoHideEnable = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
                IsMouseCursolAutoHideEnable = new ReactiveProperty<bool>(CurrentWindowContextScheduler, false);
            }

            AutoHideDelayTime = new ReactiveProperty<TimeSpan>(CurrentWindowContextScheduler, TimeSpan.FromSeconds(3));
            RaisePropertyChanged(nameof(AutoHideDelayTime));


            IsMuted = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.IsMute, CurrentWindowContextScheduler)
                .AddTo(userSessionDisposer);
            RaisePropertyChanged(nameof(IsMuted));

            SoundVolume = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.SoundVolume, CurrentWindowContextScheduler)
                .AddTo(userSessionDisposer);
            RaisePropertyChanged(nameof(SoundVolume));

            CommentDefaultColor = HohoemaApp.UserSettings.PlayerSettings
                .ToReactivePropertyAsSynchronized(x => x.CommentColor, CurrentWindowContextScheduler)
                .AddTo(userSessionDisposer);
            RaisePropertyChanged(nameof(CommentDefaultColor));

            SoundVolume.Subscribe(volume => 
            {
                MediaPlayer.Volume = volume;
            });


            RequestUpdateInterval = HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.CommentRenderingFPS)
                .Select(x => TimeSpan.FromSeconds(1.0 / x))
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(userSessionDisposer);
            RaisePropertyChanged(nameof(RequestUpdateInterval));

            RequestCommentDisplayDuration = HohoemaApp.UserSettings.PlayerSettings
                .ObserveProperty(x => x.CommentDisplayDuration)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(userSessionDisposer);
            RaisePropertyChanged(nameof(RequestCommentDisplayDuration));

            CommentFontScale = HohoemaApp.UserSettings.PlayerSettings
                .ObserveProperty(x => x.DefaultCommentFontScale)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(userSessionDisposer);
            RaisePropertyChanged(nameof(CommentFontScale));


            HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.IsKeepDisplayInPlayback)
                .Subscribe(isKeepDisplay =>
                {
                    SetKeepDisplayWithCurrentState();
                })
                .AddTo(userSessionDisposer);


            IsForceLandscape = HohoemaApp.UserSettings.PlayerSettings.ToReactivePropertyAsSynchronized(x => x.IsForceLandscape);
            RaisePropertyChanged(nameof(IsForceLandscape));


            // お気に入りフィード上の動画を既読としてマーク
            await HohoemaApp.FeedManager.MarkAsRead(Video.VideoId);
            await HohoemaApp.FeedManager.MarkAsRead(Video.RawVideoId);

            cancelToken.ThrowIfCancellationRequested();


            App.Current.LeavingBackground += Current_LeavingBackground;
            App.Current.EnteredBackground += Current_EnteredBackground;

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

            IsAvailableDmcHighQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_High).CanPlay;
            IsAvailableDmcMidiumQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Midium).CanPlay;
            IsAvailableDmcLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Low).CanPlay;
            IsAvailableDmcMobileQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Dmc_Mobile).CanPlay;
            IsAvailableLegacyOriginalQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Original).CanPlay;
            IsAvailableLegacyLowQuality.Value = Video.GetDividedQualityNicoVideo(NicoVideoQuality.Low).CanPlay;

            UpdateCache();

            ToggleCacheRequestCommand.RaiseCanExecuteChanged();

            if (_SidePaneContentCache.ContainsKey(PlayerSidePaneContentType.Setting))
            {
                (_SidePaneContentCache[PlayerSidePaneContentType.Setting] as SettingsSidePaneContentViewModel).SetupAvairableVideoQualities(
                                    Video.GetAllQuality()
                                    .Where(x => x.CanPlay)
                                    .Select(x => x.Quality)
                                    .ToList()
                                    );
            }

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

                RaisePropertyChanged(nameof(CommandEditerVM));

                CommandEditerVM.OnCommandChanged += () => UpdateCommandString();
                CommandEditerVM.IsPremiumUser = base.HohoemaApp.IsPremiumUser;

                CommandEditerVM.IsAnonymousDefault = HohoemaApp.UserSettings.PlayerSettings.IsDefaultCommentWithAnonymous;
                CommandEditerVM.IsAnonymousComment.Value = HohoemaApp.UserSettings.PlayerSettings.IsDefaultCommentWithAnonymous;

                // コミュニティやチャンネルの動画では匿名コメントは利用できない
                CommandEditerVM.ChangeEnableAnonymity(!(Video.IsCommunity || Video.IsChannel));

                UpdateCommandString();


                cancelToken.ThrowIfCancellationRequested();
                
                // バッファリング状態のモニターが使うタイマーだけはページ稼働中のみ動くようにする
                InitializeBufferingMonitor();

                // 再生ストリームの準備を開始する
                try
                {
                    await PlayingQualityChangeAction();

                    cancelToken.ThrowIfCancellationRequested();
                }
                catch
                {
                    Video?.StopPlay(MediaPlayer);
                    throw;
                }

                // Note: 0.4.1現在ではキャッシュはmp4のみ対応
                var isCanCache = Video.ContentType == MovieType.Mp4;
                var isAcceptedCache = HohoemaApp.UserSettings?.CacheSettings?.IsUserAcceptedCache ?? false;
                var isEnabledCache = (HohoemaApp.UserSettings?.CacheSettings?.IsEnableCache ?? false) || IsSaveRequestedCurrentQualityCache.Value;

                CanDownload = isAcceptedCache && isEnabledCache && isCanCache;

                await UpdateComments();

                // 再生履歴に反映
                //VideoPlayHistoryDb.VideoPlayed(Video.RawVideoId);
            }

            HohoemaApp.UserSettings.PlayerSettings.ObserveProperty(x => x.DefaultQuality, false)
                .Subscribe(async x =>
                {
                    if (IsDisposed) { return; }

                    _PreviosPlayingVideoPosition = ReadVideoPosition.Value;

                    RequestVideoQuality.Value = x;

                    await PlayingQualityChangeAction();
                })
                .AddTo(userSessionDisposer);


            IsPauseWithCommentWriting = HohoemaApp.UserSettings.PlayerSettings
				.ToReactivePropertyAsSynchronized(x => x.PauseWithCommentWriting, CurrentWindowContextScheduler)
				.AddTo(userSessionDisposer);
			RaisePropertyChanged(nameof(IsPauseWithCommentWriting));
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


            // サポートされたメディアの再生
            CurrentState.Value = MediaPlaybackState.Opening;

            try
            {
                CurrentVideoQuality.Value = await Video.StartPlay(MediaPlayer, RequestVideoQuality.Value, _PreviosPlayingVideoPosition);
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
                await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Video?.StopPlay(MediaPlayer);
                });

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
                HohoemaApp.UserSettings.PlayerSettings.PlaybackRate;

            // リクエストどおりの画質が再生された場合、画質をデフォルトとして設定する
            if (RequestVideoQuality.Value == CurrentVideoQuality.Value)
            {
                if (CurrentVideoQuality.Value.HasValue && CurrentVideoQuality.Value.Value.IsDmc())
                {
                    HohoemaApp.UserSettings.PlayerSettings.DefaultQuality = CurrentVideoQuality.Value.Value;
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

						if (CurrentState.Value == MediaPlaybackState.Paused
                        || CurrentState.Value == MediaPlaybackState.None)
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
				.ToReactiveProperty(CurrentWindowContextScheduler)
				.AddTo(_BufferingMonitorDisposable);

			RaisePropertyChanged(nameof(NowBuffering));
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



			var commentVM = new Comment(this, HohoemaApp.UserSettings.NGSettings)
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



            var commentVM = new Comment(this, HohoemaApp.UserSettings.NGSettings)
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


            System.Diagnostics.Debug.WriteLine($"コメント数:{Comments.Count}");
        }

        /*
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
        */



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
            CurrentPlayingItem = HohoemaApp.Playlist.Player.Current;

            CurrentPlaylistName.Value = CurrentPlaylist.Name;
            PlaylistItems = CurrentPlaylist.PlaylistItems.ToReadOnlyReactiveCollection();
            RaisePropertyChanged(nameof(PlaylistItems));

            
            
            MediaPlayer.PlaybackSession.PlaybackStateChanged += PlaybackSession_PlaybackStateChanged;
            MediaPlayer.PlaybackSession.PositionChanged += PlaybackSession_PositionChanged;

            var smtc = MediaPlayer.SystemMediaTransportControls;
//            smtc.AutoRepeatModeChangeRequested += Smtc_AutoRepeatModeChangeRequested;
            MediaPlayer.CommandManager.NextReceived += CommandManager_NextReceived;
            MediaPlayer.CommandManager.PreviousReceived += CommandManager_PreviousReceived;



            Debug.WriteLine("VideoPlayer OnNavigatedToAsync done.");

            // 基本的にオンラインで再生、
            // オフラインの場合でキャッシュがあるようならキャッシュで再生できる
            ChangeRequireServiceLevel(HohoemaAppServiceLevel.LoggedIn);

            App.Current.Suspending += Current_Suspending;

            PlaylistCanGoBack.Value = HohoemaApp.Playlist.Player.CanGoBack;
            PlaylistCanGoNext.Value = HohoemaApp.Playlist.Player.CanGoNext;

        }


        protected override void OnHohoemaNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            var mediaPlayer = MediaPlayer;
            MediaPlayer = null;
            RaisePropertyChanged(nameof(MediaPlayer));
            MediaPlayer = mediaPlayer;

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
                MediaPlayer.PlaybackSession.PlaybackStateChanged -= PlaybackSession_PlaybackStateChanged;
                MediaPlayer.PlaybackSession.PositionChanged -= PlaybackSession_PositionChanged;

                Comments.Clear();
            }

            Video?.StopPlay(MediaPlayer);

            // プレイリストへ再生完了を通知
            VideoPlayed();

            ExitKeepDisplay();

            _BufferingMonitorDisposable?.Dispose();
            _BufferingMonitorDisposable = new CompositeDisposable();

            // サイドペインの片付け
            _PrevSidePaneContentType = CurrentSidePaneContentType.Value;
            
            if (_SidePaneContentCache.ContainsKey(PlayerSidePaneContentType.Comment))
            {
                try
                {
                    var commentSidePaneContent = _SidePaneContentCache[PlayerSidePaneContentType.Comment];
                    commentSidePaneContent.Dispose();
                    _SidePaneContentCache.Remove(PlayerSidePaneContentType.Comment);
                }
                catch { Debug.WriteLine("failed dispose PlayerSidePaneContentType.Comment"); }

                CurrentSidePaneContentType.Value = null;
            }

            base.OnHohoemaNavigatingFrom(e, viewModelState, suspending);

            App.Current.LeavingBackground -= Current_LeavingBackground;
            App.Current.EnteredBackground -= Current_EnteredBackground;


            Debug.WriteLine("VideoPlayer OnNavigatingFromAsync done.");
        }


        private void Current_EnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
        }

        private void Current_LeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            CurrentWindowContextScheduler.Schedule(() =>
            {
                if (IsDisposed) { return; }
                RequestUpdateInterval.ForceNotify();
            });
        }

        private void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            //            PreviousVideoPosition = ReadVideoPosition.Value.TotalSeconds;

            _PreviosPlayingVideoPosition = TimeSpan.FromSeconds(SliderVideoPosition.Value);

            _IsNeddPlayInResumed = this.CurrentState.Value != MediaPlaybackState.Paused
                && this.CurrentState.Value != MediaPlaybackState.None;

            IsFullScreen.Value = false;

            ExitKeepDisplay();

            MediaPlayer.Pause();
            MediaPlayer.Source = null;


            _BufferingMonitorDisposable?.Dispose();
            _BufferingMonitorDisposable = new CompositeDisposable();
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

            CurrentWindowContextScheduler.Schedule(() =>
            {
                if (IsDisposed) { return; }

                CurrentState.Value = sender.PlaybackState;
            });

            // 最後まで到達していた場合
            if (sender.PlaybackState == MediaPlaybackState.Paused
                && sender.Position >= (Video.VideoLength - TimeSpan.FromSeconds(1))
                )
            {
                VideoPlayed(canPlayNext: true);
            }
        }




        private void CommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                HohoemaApp.Playlist.PlayDone(CurrentPlayingItem, canPlayNext:false);

                if (HohoemaApp.Playlist.Player.CanGoBack)
                {
                    HohoemaApp.Playlist.Player.GoBack();
                }
            }
        }

        private void CommandManager_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            if (args.Handled != true)
            {
                args.Handled = true;

                HohoemaApp.Playlist.PlayDone(CurrentPlayingItem, canPlayNext: true);

                /*
                if (HohoemaApp.Playlist.Player.CanGoBack)
                {
                    HohoemaApp.Playlist.Player.GoBack();
                }
                */
            }
        }


        private void ResetMediaPlayerCommand()
        {
            if (MediaPlayer == null) { return; }

            var isEnableNextButton = this.PlaylistCanGoBack.Value;
            if (isEnableNextButton)
            {
                MediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            }
            else
            {
                MediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Never;
            }

            var isEnableBackButton = this.PlaylistCanGoNext.Value;
            if (isEnableBackButton)
            {
                MediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            }
            else
            {
                MediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Never;
            }
        }


        bool _IsVideoPlayed = false;
        private void VideoPlayed(bool canPlayNext = false)
        {
            if (_IsVideoPlayed == false)
            {
                // Note: 次の再生用VMの作成を現在ウィンドウのUIDsipatcher上で行わないと同期コンテキストが拾えず再生に失敗する
                // VideoPlayedはMediaPlayerが動作しているコンテキスト上から呼ばれる可能性がある
                CurrentWindowContextScheduler.Schedule(() => 
                {
                    HohoemaApp.Playlist.PlayDone(CurrentPlayingItem, canPlayNext);
                });

                _IsVideoPlayed = true;
            }
        }



		protected override void OnDispose()
		{
            Video?.StopPlay(MediaPlayer);

            MediaPlayer.Dispose();

            _BufferingMonitorDisposable?.Dispose();

            var sidePaneContents = _SidePaneContentCache.Values.ToArray();
            _SidePaneContentCache.Clear();
            foreach (var sidePaneContent in sidePaneContents)
            {
                sidePaneContent.Dispose();
            }

            base.OnDispose();
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

					var commentVM = new Comment(this, HohoemaApp.UserSettings.NGSettings)
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
                        IsCommentDisplayEnable.Value = !IsCommentDisplayEnable.Value;
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
                        HohoemaApp.Playlist.PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
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
                        HohoemaApp.Playlist.PlayerDisplayType = PlayerDisplayType.PrimaryView;
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
                        HohoemaApp.Playlist.PlayerDisplayType = PlayerDisplayType.SecondaryView;
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
                        if (HohoemaApp.Playlist.PlayerDisplayType == PlayerDisplayType.PrimaryView)
                        {
                            HohoemaApp.Playlist.PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
                        }

                        PageManager.OpenPage(HohoemaPageType.VideoInfomation, Video.RawVideoId);
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
                        ShareHelper.Share(Video);
                    }
                    , () => DataTransferManager.IsSupported()
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
                        await ShareHelper.ShareToTwitter(Video);
                    }
                    ));
            }
        }

        private DelegateCommand _VideoInfoCopyToClipboardCommand;
        public DelegateCommand VideoInfoCopyToClipboardCommand
        {
            get
            {
                return _VideoInfoCopyToClipboardCommand
                    ?? (_VideoInfoCopyToClipboardCommand = new DelegateCommand(() =>
                    {
                        ShareHelper.CopyToClipboard(Video);
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
                        var targetMylist = await HohoemaApp.ChoiceMylist();
                        if (targetMylist != null)
                        {
                            var result = await HohoemaApp.AddMylistItem(targetMylist, Video.Title, Video.RawVideoId);
                            (App.Current as App).PublishInAppNotification(
                                InAppNotificationPayload.CreateRegistrationResultNotification(
                                    result,
                                    "マイリスト",
                                    targetMylist.Name,
                                    Video.Title
                                    ));
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
                        HohoemaApp.Playlist.IsDisplayMainViewPlayer = false;
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

                                    var dialog = new MessageDialog(
                                        $"{VideoTitle} の キャッシュデータ（{qualityText}画質）を削除します。この操作は元に戻せません。", 
                                        "キャッシュ削除の確認"
                                        );
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

                                        await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => 
                                        {
                                            await Video.CancelCacheRequest(quality);
                                        });
                                    }
                                }
                                else
                                {
                                    await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                                    {
                                        await Video.RequestCache(quality);
                                    });
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

                            HohoemaApp.Playlist.PlayDone(CurrentPlayingItem);

                            if (player.CanGoBack)
                            {
                                player.GoBack();
                            }
                        }
                    }
//                    , () => HohoemaApp.Playlist.Player?.CanGoBack ?? false
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

                            HohoemaApp.Playlist.PlayDone(CurrentPlayingItem, canPlayNext:true);
                        }
                    }
//                    , () => HohoemaApp.Playlist.Player?.CanGoNext ?? false
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
                        if (item != CurrentPlayingItem)
                        {
                            HohoemaApp.Playlist.Play(item);
                        }
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
                        if (HohoemaApp.Playlist.PlayerDisplayType == PlayerDisplayType.PrimaryView)
                        {
                            HohoemaApp.Playlist.PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
                        }


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
        public PlaylistItem CurrentPlayingItem
        {
            get { return _CurrentPlayingItem; }
            set { SetProperty(ref _CurrentPlayingItem, value); }
        }

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

        public bool IsDisplayInSecondaryView => HohoemaApp.Playlist.PlayerDisplayType == PlayerDisplayType.SecondaryView;


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
        public DelegateCommand<double?> SetPlaybackRateCommand { get; private set; }

        public ReactiveProperty<bool> IsAutoHideEnable { get; private set; }
        public ReactiveProperty<TimeSpan> AutoHideDelayTime { get; private set; }

        public ReactiveProperty<bool> IsMouseCursolAutoHideEnable { get; private set; }

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
        public ReactiveProperty<bool> IsCompactOverlay { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsSmallWindowModeEnable { get; private set; }
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
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistItem> PlaylistItems { get; private set; }

        private Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase> _SidePaneContentCache = new Dictionary<PlayerSidePaneContentType, SidePaneContentViewModelBase>();

        public ReactiveProperty<PlayerSidePaneContentType?> CurrentSidePaneContentType { get; }
        public ReadOnlyReactiveProperty<SidePaneContentViewModelBase> CurrentSidePaneContent { get; }


        static PlayerSidePaneContentType? _PrevSidePaneContentType;
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
                return EmptySidePaneContent;
            }
            else
            {
                SidePaneContentViewModelBase sidePaneContent = null;
                switch (maybeType.Value)
                {
                    case PlayerSidePaneContentType.Playlist:
                        sidePaneContent = new PlayerSidePaneContent.PlaylistSidePaneContentViewModel(MediaPlayer, HohoemaApp.Playlist, HohoemaApp.UserSettings.PlaylistSettings, PageManager);
                        break;
                    case PlayerSidePaneContentType.Comment:
                        throw new NotImplementedException();
                    //                        sidePaneContent = new PlayerSidePaneContent.CommentSidePaneContentViewModel(HohoemaApp.UserSettings, LiveComments);
                    //                        break;
                    case PlayerSidePaneContentType.Setting:
                        sidePaneContent = new PlayerSidePaneContent.SettingsSidePaneContentViewModel(HohoemaApp.UserSettings);
                        if (Video != null)
                        {
                            (sidePaneContent as SettingsSidePaneContentViewModel).SetupAvairableVideoQualities(
                                Video.GetAllQuality()
                                .Where(x => x.CanPlay)
                                .Select(x => x.Quality)
                                .ToList()
                                );
                        }
                        break;
                    default:
                        sidePaneContent = new PlayerSidePaneContent.EmptySidePaneContentViewModel();
                        break;
                }

                _SidePaneContentCache.Add(maybeType.Value, sidePaneContent);
                return sidePaneContent;
            }
        }

        

        public static EmptySidePaneContentViewModel EmptySidePaneContent { get; } = new EmptySidePaneContentViewModel();

        ToastNotificationService _ToastService;
		TextInputDialogService _TextInputDialogService;


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


	public enum PlayerSidePaneContentType
	{
        Playlist,
		Comment,
		Setting,
	}










}

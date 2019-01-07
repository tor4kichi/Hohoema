using NicoPlayerHohoema.ViewModels;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Unity;
using NicoPlayerHohoema.Models.Cache;
using NicoPlayerHohoema.Services.Page;
using System.Reactive.Concurrency;
using Prism.Mvvm;
using Microsoft.Toolkit.Uwp.Helpers;
using Prism.Events;
using Prism.Commands;
using System.Reactive;
using System.Threading;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace NicoPlayerHohoema.Services
{
    public sealed class PlayerViewManager : BindableBase
    {
        /* 複数ウィンドウでプレイヤーを一つだけ表示するための管理をしています
         * 
         * 前提としてUnityContainer（PrismUnityApplication App.xaml.cs を参照）による依存性解決を利用しており
         * さらに「PerThreadLifetimeManager」を指定して依存解決時にウィンドウのUIスレッドごとに
         * PlayerViewManagerが生成されるように設定しています。
         * 
         * これはBindableBaseによるINofityPropertyChangedイベントがウィンドウスレッドを越えて利用できないことが理由です。
         * 
         * PlayerViewManagerはNowPlayingとPlayerViewModeの２つを公開プロパティとして保持しています。
         * NowPlaying
         * 
        */


        public class PlayerCloseEvent : PubSubEvent<Unit>
        {

        }

        public class PlayerNowPlayingChangeEvent : PubSubEvent<bool>
        {

        }

        public class PlayerViewModeChangeEvent : PubSubEvent<PlayerViewModeChangeEventArgs>
        {

        }


        public struct PlayerViewModeChangeEventArgs
        {
            public PlayerViewMode ViewMode { get; set; }
        }

        // プレイヤーの表示状態を管理する
        // これまでHohoemaPlaylist、MenuNavigationViewModelBaseなどに散らばっていた

        static IScheduler PrimaryViewScheduler { get; set; }
        static IScheduler SecondaryViewScheduler { get; set; }


        Models.Helpers.AsyncLock NowPlayingLock = new Models.Helpers.AsyncLock();

        public PlayerViewManager(
            IScheduler currentScheduler,
            INavigationService primaryViewPlayerNavigationService,
            IEventAggregator eventAggregator
            )
        {
            CurrentView = ApplicationView.GetForCurrentView();

            if (IsMainView)
            {
                PrimaryViewScheduler = currentScheduler;
            }
            else
            {
                SecondaryViewScheduler = currentScheduler;
            }

            EventAggregator = eventAggregator;

            // プレイヤーFrameの表示状態を読み取る
            // PrimaryViewのFrameがBlankなら NowPlaying = false
            // それ以外なら NowPlayig = true にしたい


            if (IsMainView)
            {
                CurrentView.Consolidated += MainView_Consolidated;
            }


            if (!IsMainView)
            {
                SecondaryViewPlayerNavigationService = ResolveSecondaryViewPlayerNavigationService();
                SecondaryViewScheduler = App.Current.Container.Resolve<IScheduler>();
                SecondaryCoreAppView = CoreApplication.GetCurrentView();
                SecondaryAppView = CurrentView;
            }

            EventAggregator.GetEvent<Prism.Windows.Navigation.NavigationStateChangedEvent>()
                .Subscribe(args => 
                {
                    if (args.Sender.Content is Views.VideoPlayerPage || args.Sender.Content is Views.LivePlayerPage)
                    {
                        NowPlaying = true;
                    }
                    else if (args.Sender.Content is Views.BlankPage)
                    {
                        NowPlaying = false;
                    }

                    Debug.WriteLine($"IsMain:{IsMainView}, NowPlaying:{NowPlaying}, PlayerViewMode:{PlayerViewMode}");

                    ToggleFullScreenWhenApplicationViewShowWithStandalone();
                });

            EventAggregator.GetEvent<PlayerViewModeChangeEvent>()
                .Subscribe(args => 
                {
                    PlayerViewMode = args.ViewMode;
                },
                ThreadOption.BackgroundThread
                );
        }

        public bool IsMainView => MainViewId == CurrentView.Id;


        public static int MainViewId { get; } = ApplicationView.GetApplicationViewIdForWindow(CoreApplication.MainView.CoreWindow);

        public ApplicationView CurrentView { get; private set; }

        public CoreApplicationView SecondaryCoreAppView { get; private set; }
        public ApplicationView SecondaryAppView { get; private set; }
        public INavigationService SecondaryViewPlayerNavigationService { get; private set; }        
//        public IScheduler SecondaryViewScheduler { get; private set; }

        private IScheduler CurrentViewScheduler => IsMainView ? PrimaryViewScheduler : SecondaryViewScheduler;

        private HohoemaSecondaryViewFrameViewModel _SecondaryViewVM { get; set; }

        private MediaPlayer _MediaPlayer { get; set; }

        private bool _NowPlaying;
        public bool NowPlaying
        {
            get { return _NowPlaying; }
            private set
            {
                CurrentViewScheduler.Schedule(() =>
                {
                    if (SetProperty(ref _NowPlaying, value))
                    {
                        RaisePropertyChanged(nameof(IsPlayingWithPrimaryView));
                        RaisePropertyChanged(nameof(IsPlayingWithSecondaryView));
                    }
                });
            }
        }

//        public IScheduler PrimaryViewScheduler { get; }

        private INavigationService _PrimaryViewPlayerNavigationService;
        public INavigationService PrimaryViewPlayerNavigationService
        {
            get
            {
                return _PrimaryViewPlayerNavigationService
                    ?? (_PrimaryViewPlayerNavigationService = App.Current.Container.Resolve<INavigationService>(nameof(PrimaryViewPlayerNavigationService)));
            }
        }



        public INavigationService ResolveSecondaryViewPlayerNavigationService()
        {
            return App.Current.Container.Resolve<INavigationService>(SECONDARY_VIEW_PLAYER_NAVIGATION_SERVICE);
        }

        public IEventAggregator EventAggregator { get; }

        private PlayerViewMode? _PlayerViewMode;
        public PlayerViewMode PlayerViewMode
        {
            get
            {
                if (_PlayerViewMode != null) { return _PlayerViewMode.Value; }

                var localObjectStorageHelper = new LocalObjectStorageHelper();
                _PlayerViewMode = localObjectStorageHelper.Read(nameof(Services.PlayerViewMode), PlayerViewMode.PrimaryView);
                
                return _PlayerViewMode.Value;
            }
            private set
            {
                CurrentViewScheduler.Schedule(() =>
                {
                    if (SetProperty(ref _PlayerViewMode, value))
                    {
                        var localObjectStorageHelper = new LocalObjectStorageHelper();
                        localObjectStorageHelper.Save(nameof(Services.PlayerViewMode), _PlayerViewMode.Value);

                        RaisePropertyChanged(nameof(IsPlayerShowWithPrimaryView));
                        RaisePropertyChanged(nameof(IsPlayerShowWithSecondaryView));
                        RaisePropertyChanged(nameof(IsPlayingWithPrimaryView));
                        RaisePropertyChanged(nameof(IsPlayingWithSecondaryView));

                        // PlayerViewMode変更後の動画再生を再開
                        EventAggregator.GetEvent<PlayerViewModeChangeEvent>()
                            .Publish(new PlayerViewModeChangeEventArgs()
                            {
                                ViewMode = _PlayerViewMode.Value
                            });
                    }
                });
            }
        }

        public bool IsPlayerShowWithPrimaryView => PlayerViewMode == PlayerViewMode.PrimaryView;
        public bool IsPlayerShowWithSecondaryView => PlayerViewMode == PlayerViewMode.SecondaryView;

        public bool IsPlayingWithPrimaryView => NowPlaying && PlayerViewMode == PlayerViewMode.PrimaryView;
        public bool IsPlayingWithSecondaryView => NowPlaying && PlayerViewMode == PlayerViewMode.SecondaryView;

        private bool _IsPlayerSmallWindowModeEnabled;
        public bool IsPlayerSmallWindowModeEnabled
        {
            get { return _IsPlayerSmallWindowModeEnabled; }
            set
            {
                CurrentViewScheduler.Schedule(() => 
                {
                    if (SetProperty(ref _IsPlayerSmallWindowModeEnabled, value))
                    {
                        ToggleFullScreenWhenApplicationViewShowWithStandalone();
                    }
                });
            }
        }



        private void ToggleFullScreenWhenApplicationViewShowWithStandalone()
        {
            CurrentViewScheduler.Schedule(() =>
            {
            
                ApplicationView currentView = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView();

                if (Services.Helpers.DeviceTypeHelper.IsMobile || Services.Helpers.DeviceTypeHelper.IsDesktop)
                {
                    if (NowPlaying)
                    {
                        if (IsPlayerSmallWindowModeEnabled)
                        {
                            if (currentView.IsFullScreenMode)
                            {
                                currentView.ExitFullScreenMode();
                            }
                        }
                        else if (currentView.AdjacentToLeftDisplayEdge && currentView.AdjacentToRightDisplayEdge)
                        {
                            currentView.TryEnterFullScreenMode();
                        }
                    }
                    else
                    {
                        // プレイヤーを閉じた時にCompactOverlayも解除する
                        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
                        {
                            if (currentView.IsViewModeSupported(ApplicationViewMode.CompactOverlay)
                                && currentView.ViewMode == ApplicationViewMode.CompactOverlay)
                            {
                                _ = currentView.TryEnterViewModeAsync(ApplicationViewMode.Default);
                            }
                        }

                        if (!IsPlayerSmallWindowModeEnabled)
                        {
                            if (IsPlayerShowWithPrimaryView && IsMainView)
                            {
                                if (ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen)
                                {
                                    currentView.ExitFullScreenMode();
                                }
                            }
                        }
                    }
                }
            });
        }


        private Window _CurrentMediaPlayerWindow;
        public MediaPlayer GetCurrentWindowMediaPlayer()
        {
            if (Window.Current != _CurrentMediaPlayerWindow)
            {
                if (_MediaPlayer != null)
                {
                    _MediaPlayer?.Dispose();
                }

                _MediaPlayer = new MediaPlayer();
                _MediaPlayer.AutoPlay = true;
                _CurrentMediaPlayerWindow = Window.Current;
            }

            return _MediaPlayer;
        }

        
        // メインビューを閉じたらプレイヤービューも閉じる
        private async void MainView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            if (sender.Id == MainViewId)
            {
                if (SecondaryCoreAppView != null)
                {
                    await SecondaryCoreAppView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        if (SecondaryAppView != null)
                        {
                            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
                            {
                                await SecondaryAppView.TryConsolidateAsync();
                            }
                            else
                            {
                                App.Current.Exit();
                            }
                        }
                    });
                }
            }
        }

        public const string primary_view_size = "primary_view_size";
        public const string secondary_view_size = "secondary_view_size";


        private async Task<PlayerViewManager> GetEnsureSecondaryView()
        {
            if (IsMainView && SecondaryCoreAppView == null)
            {
                var playerView = CoreApplication.CreateNewView();

                
                HohoemaSecondaryViewFrameViewModel vm = null;
                int id = 0;
                ApplicationView view = null;
                INavigationService ns = null;
                await playerView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    // SecondaryViewのスケジューラをセットアップ
                    SecondaryViewScheduler = new CoreDispatcherScheduler(playerView.Dispatcher);

                    playerView.TitleBar.ExtendViewIntoTitleBar = true;

                    var content = new Views.HohoemaSecondaryViewFrame();

                    var frameFacade = new FrameFacadeAdapter(content.Frame, EventAggregator);

                    var sessionStateService = new SessionStateService();

                    //sessionStateService.RegisterFrame(frameFacade, "secondary_view_player");
                    ns = new FrameNavigationService(frameFacade
                        , (pageToken) =>
                        {
                            if (pageToken == nameof(Views.VideoPlayerPage))
                            {
                                return typeof(Views.VideoPlayerPage);
                            }
                            else if (pageToken == nameof(Views.LivePlayerPage))
                            {
                                return typeof(Views.LivePlayerPage);
                            }
                            else
                            {
                                return typeof(Views.BlankPage);
                            }
                        }, sessionStateService);

                    vm = content.DataContext as HohoemaSecondaryViewFrameViewModel;

                    Window.Current.Content = content;

                    id = ApplicationView.GetApplicationViewIdForWindow(playerView.CoreWindow);

                    view = ApplicationView.GetForCurrentView();

                    view.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                    view.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                    Window.Current.Activate();

                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(id, ViewSizePreference.UseHalf, MainViewId, ViewSizePreference.UseHalf);

                    // ウィンドウサイズの保存と復元
                    if (Services.Helpers.DeviceTypeHelper.IsDesktop)
                    {
                        var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                        if (localObjectStorageHelper.KeyExists(secondary_view_size))
                        {
                            view.TryResizeView(localObjectStorageHelper.Read<Size>(secondary_view_size));
                        }

                        view.VisibleBoundsChanged += View_VisibleBoundsChanged;
                    }

                    view.Consolidated += SecondaryAppView_Consolidated;
                });

                SecondaryAppView = view;
                _SecondaryViewVM = vm;
                SecondaryCoreAppView = playerView;
                SecondaryViewPlayerNavigationService = ns;

                App.Current.Container.RegisterInstance(SECONDARY_VIEW_PLAYER_NAVIGATION_SERVICE, SecondaryViewPlayerNavigationService);
            }

            return this;
        }

        private const string SECONDARY_VIEW_PLAYER_NAVIGATION_SERVICE = nameof(SECONDARY_VIEW_PLAYER_NAVIGATION_SERVICE);

        // アプリ終了時に正しいウィンドウサイズを保存するための一時的な箱
        private Size _PrevSecondaryViewSize;

        /// <summary>
        /// ウィンドウサイズが変更された<br />
        /// 前回表示のウィンドウサイズの保存操作を実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void View_VisibleBoundsChanged(ApplicationView sender, object args)
        {
            // ウィンドウサイズを保存する
            if (sender.Id != MainViewId)
            {
                var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                _PrevSecondaryViewSize = localObjectStorageHelper.Read<Size>(secondary_view_size);
                localObjectStorageHelper.Save(secondary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));
            }

            Debug.WriteLine($"SecondaryView VisibleBoundsChanged: {sender.VisibleBounds.ToString()}");
        }

        private async void SecondaryAppView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
             SecondaryViewPlayerNavigationService.Navigate(nameof(Views.BlankPage), null);

            // Note: 1803時点での話
            // VisibleBoundsChanged がアプリ終了前に呼ばれるが
            // この際メインウィンドウとセカンダリウィンドウのウィンドウサイズが互い違いに送られてくるため
            // 直前のウィンドウサイズの値を前々回表示のウィンドウサイズ（_PrevSecondaryViewSize）で上書きする
            if (_PrevSecondaryViewSize != default(Size))
            {
                var localObjectStorageHelper = App.Current.Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                localObjectStorageHelper.Save(secondary_view_size, _PrevSecondaryViewSize);
            }

            // セカンダリウィンドウを閉じるタイミングでキャッシュを再開する
            // プレミアム会員の場合は何もおきない
            var cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
            await cacheManager.ResumeCacheDownload();
        }

        public async Task PlayWithCurrentPlayerView(PlaylistItem item)
        {
            using (await NowPlayingLock.LockAsync())
            {
                var pageType = item.Type == PlaylistItemType.Video ? nameof(Views.VideoPlayerPage) : nameof(Views.LivePlayerPage);
                string parameter = null;
                switch (item.Type)
                {
                    case PlaylistItemType.Video:
                        parameter = new VideoPlayPayload()
                        {
                            VideoId = item.ContentId
                        }
                        .ToParameterString();

                        break;
                    case PlaylistItemType.Live:
                        parameter = new Models.Live.LiveVideoPagePayload(item.ContentId)
                        {
                            LiveTitle = item.Title
                        }
                        .ToParameterString();

                        break;
                }

                if (parameter == null) { throw new ArgumentException("PlayerViewManager failed player frame navigation"); }

                if (PlayerViewMode == PlayerViewMode.PrimaryView)
                {
                    Debug.WriteLine("Play with Primary : " + parameter);

                    PrimaryViewScheduler.Schedule(() =>
                    {
                        PrimaryViewPlayerNavigationService.Navigate(pageType, parameter);
                    //                    _ = ApplicationViewSwitcher.TryShowAsStandaloneAsync(MainViewId);
                    });
                }

                if (PlayerViewMode == PlayerViewMode.SecondaryView)
                {
                    Debug.WriteLine("Play with Secondary : " + parameter);

                    // サブウィンドウをアクティベートして、サブウィンドウにPlayerページナビゲーションを飛ばす
                    await GetEnsureSecondaryView();

                    SecondaryViewScheduler.Schedule(() =>
                    {
                        if (SecondaryViewPlayerNavigationService.Navigate(pageType, parameter))
                        {
                            SecondaryAppView.Title = !string.IsNullOrEmpty(item?.Title) ? item.Title : "Hohoema";
                        }
                        else
                        {
                            SecondaryAppView.Title = "Hohoema!";
                        }
                    });
                }

                _CurrentPlayItem = item;
            }
        }

        static PlaylistItem _CurrentPlayItem { get; set; }

        /// <summary>
        /// SecondaryViewを閉じます。
        /// MainViewから呼び出すと別スレッドアクセスでエラーになるはず
        /// </summary>
        public async void ClosePlayer()
        {
            using (await NowPlayingLock.LockAsync())
            {
                if (IsMainView)
                {
                    PrimaryViewScheduler.Schedule(() =>
                    {
                        PrimaryViewPlayerNavigationService.Navigate(nameof(Views.BlankPage), null);
                    });
                }
                else
                {
                    SecondaryViewScheduler.Schedule(async () =>
                    {
                        SecondaryViewPlayerNavigationService.Navigate(nameof(Views.BlankPage), null);

                        await ShowMainView();

                        if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4))
                        {
                            await CurrentView.TryConsolidateAsync().AsTask()
                                .ContinueWith(prevTask =>
                                {
                                //                        CoreAppView = null;
                                //                        AppView = null;
                                //                        _SecondaryViewVM = null;
                                //                        NavigationService = null;
                            });
                        }
                        else
                        {


                            SecondaryCoreAppView = null;
                            SecondaryAppView = null;
                            _SecondaryViewVM = null;
                            SecondaryViewPlayerNavigationService = null;
                        }
                    });
                }
            }
        }


        public Task ShowMainView()
        {
            var currentView = ApplicationView.GetForCurrentView();
            if (SecondaryAppView != null && currentView.Id != CurrentView.Id)
            {
                return ApplicationViewSwitcher.SwitchAsync(CurrentView.Id, MainViewId).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }




        private DelegateCommand _ClosePlayerCommand;
        public DelegateCommand ClosePlayerCommand => _ClosePlayerCommand
            ?? (_ClosePlayerCommand = new DelegateCommand(() => 
            {
                ClosePlayer();
            }));




        public async Task<bool> ChangePlayerViewModeAsync(PlayerViewMode playerViewMode)
        {
            if (playerViewMode == PlayerViewMode.SecondaryView && !Services.Helpers.DeviceTypeHelper.IsDesktop)
            {
                throw new NotSupportedException("Secondary view only Desktop. not support on current device.");
            }

            if (playerViewMode != PlayerViewMode)
            {
                ClosePlayer();

                PlayerViewMode = playerViewMode;

                await Task.Delay(100);

                await PlayWithCurrentPlayerView(_CurrentPlayItem);
            }

            return true;
        }

    }
}

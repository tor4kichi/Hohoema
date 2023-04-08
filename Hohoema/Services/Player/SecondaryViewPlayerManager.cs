using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Contracts.Services.Navigations;
using System;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;
using Hohoema.Models.Niconico.Live;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.System;
using Microsoft.Toolkit.Uwp;
using Hohoema.Helpers;
using Hohoema.Views.Pages;
using Hohoema.Views.Player;
using NiconicoToolkit.Video;
using NiconicoToolkit.Live;
using Hohoema.Models.Playlist;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Contracts.Services.Player;

namespace Hohoema.Services.Player
{
    public sealed class SecondaryViewPlayerManager : ObservableObject, IPlayerView
    {
        /* 複数ウィンドウでプレイヤーを一つだけ表示するための管理をしています
         * 
         * 前提としてUnityContainer（PrismUnityApplication App.xaml.cs を参照）による依存性解決を利用しており
         * さらに「PerThreadLifetimeManager」を指定して依存解決時にウィンドウのUIスレッドごとに
         * PlayerViewManagerが生成されるように設定しています。
         * 
         * これはObservableObjectによるINofityPropertyChangedイベントがウィンドウスレッドを越えて利用できないことが理由です。
         * 
         * PlayerViewManagerはNowPlayingとPlayerViewModeの２つを公開プロパティとして保持しています。
         * NowPlaying
         * 
        */

        // プレイヤーの表示状態を管理する
        // これまでHohoemaPlaylist、MenuNavigationViewModelBaseなどに散らばっていた

        public SecondaryViewPlayerManager(
            IScheduler scheduler,
            RestoreNavigationManager restoreNavigationManager
            )
        {
            _scheduler = scheduler;
            _restoreNavigationManager = restoreNavigationManager;
            MainViewId = ApplicationView.GetApplicationViewIdForWindow(CoreApplication.MainView.CoreWindow);
        }

        Helpers.AsyncLock _playerNavigationLock = new Helpers.AsyncLock();

        private bool _IsFullScreen;
        public bool IsFullScreen
        {
            get { return _IsFullScreen; }
            private set { SetProperty(ref _IsFullScreen, value); }
        }

        private bool _IsCompactOverlay;
        public bool IsCompactOverlay
        {
            get { return _IsCompactOverlay; }
            private set { SetProperty(ref _IsCompactOverlay, value); }
        }

        public int MainViewId { get; }

        public CoreApplicationView SecondaryCoreAppView { get; private set; }
        public ApplicationView SecondaryAppView { get; private set; }
        public INavigationService SecondaryViewPlayerNavigationService { get; private set; }
        public HohoemaPlaylistPlayer PlaylistPlayer { get; private set; }


        bool isMainViewClosed;
        // メインビューを閉じたらプレイヤービューも閉じる
        private async void MainView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            if (sender.Id == MainViewId)
            {
                isMainViewClosed = true;
                if (SecondaryCoreAppView != null)
                {
                    await SecondaryCoreAppView.DispatcherQueue.EnqueueAsync(async () =>
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

        public bool IsShowSecondaryView { get; private set; }

        public const string primary_view_size = "primary_view_size";
        public const string secondary_view_size = "secondary_view_size";
        private readonly IScheduler _scheduler;
        private readonly RestoreNavigationManager _restoreNavigationManager;

        private async Task CreateSecondaryView()
        {
            var secondaryView = CoreApplication.CreateNewView();

            var result = await secondaryView.DispatcherQueue.EnqueueAsync(async () =>
            {
                secondaryView.TitleBar.ExtendViewIntoTitleBar = true;

                var content = new SecondaryWindowCoreLayout();

                var ns = content.CreateNavigationService();
                await ns.NavigateAsync(nameof(BlankPage));

                Window.Current.Content = content;

                var id = ApplicationView.GetApplicationViewIdForWindow(secondaryView.CoreWindow);

                var view = ApplicationView.GetForCurrentView();

                view.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                view.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                await Task.Delay(250);

                Window.Current.Activate();

                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(id, ViewSizePreference.UseHalf, MainViewId, ViewSizePreference.UseHalf);

                // ウィンドウサイズの保存と復元
                if (DeviceTypeHelper.IsDesktop)
                {
                    var localObjectStorageHelper = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                    if (localObjectStorageHelper.KeyExists(secondary_view_size))
                    {
                        view.TryResizeView(localObjectStorageHelper.Read<Size>(secondary_view_size));
                    }

                    view.VisibleBoundsChanged += View_VisibleBoundsChanged;
                }

                PlaylistPlayer = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<HohoemaPlaylistPlayer>();

                view.Consolidated += SecondaryAppView_Consolidated;

                _PlayerPageNavgationTransitionInfo = new DrillInNavigationTransitionInfo();
                _BlankPageNavgationTransitionInfo = new SuppressNavigationTransitionInfo();

                return (id, view, ns);
            });

            SecondaryAppView = result.view;
            SecondaryCoreAppView = secondaryView;
            SecondaryViewPlayerNavigationService = result.ns;

            _scheduler.Schedule(() =>
            {
                var primaryView = ApplicationView.GetForCurrentView();
                primaryView.Consolidated += MainView_Consolidated;
            });
        }

        DrillInNavigationTransitionInfo _PlayerPageNavgationTransitionInfo;
        SuppressNavigationTransitionInfo _BlankPageNavgationTransitionInfo;

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
                var localObjectStorageHelper = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                _PrevSecondaryViewSize = localObjectStorageHelper.Read<Size>(secondary_view_size);
                localObjectStorageHelper.Save(secondary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));
            }

            Debug.WriteLine($"SecondaryView VisibleBoundsChanged: {sender.VisibleBounds.ToString()}");
        }

        private async void SecondaryAppView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            Debug.WriteLine($"SecondaryAppView_Consolidated: IsAppInitiated:{args.IsAppInitiated} IsUserInitiated:{args.IsUserInitiated}");

            await PlaylistPlayer.ClearAsync();
            await SecondaryViewPlayerNavigationService.NavigateAsync(nameof(BlankPage), _BlankPageNavgationTransitionInfo);

            // Note: 1803時点での話
            // VisibleBoundsChanged がアプリ終了前に呼ばれるが
            // この際メインウィンドウとセカンダリウィンドウのウィンドウサイズが互い違いに送られてくるため
            // 直前のウィンドウサイズの値を前々回表示のウィンドウサイズ（_PrevSecondaryViewSize）で上書きする
            if (_PrevSecondaryViewSize != default(Size))
            {
                var localObjectStorageHelper = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                localObjectStorageHelper.Save(secondary_view_size, _PrevSecondaryViewSize);
            }

            _scheduler.Schedule(() =>
            {
                IsShowSecondaryView = false;
            });

            LastNavigatedPageName = null;

            // プレイヤーを閉じた時に再生中情報をクリア
            if (!isMainViewClosed)
            {
                Debug.WriteLine("ClearCurrentPlayerEntry secondary view closed.");
                _restoreNavigationManager.ClearCurrentPlayerEntry();
            }
        }

        static INiconicoContent _CurrentPlayContent { get; set; }

        bool _onceSurpressActivation;
        public void OnceSurpressActivation()
        {
            _onceSurpressActivation = true;
        }

        public string LastNavigatedPageName { get; private set; }

        public void SetTitle(string title)
        {
            SecondaryAppView.Title = title;
        }

        public async Task NavigationAsync(string pageName, INavigationParameters parameters)
        {
            LastNavigatedPageName = pageName;
            using (await _playerNavigationLock.LockAsync())
            {
                if (SecondaryCoreAppView == null)
                {
                    await CreateSecondaryView();
                }

                await SecondaryCoreAppView.DispatcherQueue.EnqueueAsync(async () =>
                {
                    var result = await SecondaryViewPlayerNavigationService.NavigateAsync(pageName, parameters, _PlayerPageNavgationTransitionInfo);
                    if (!result.IsSuccess)
                    {
                        Debug.WriteLine(result.Exception?.ToString());
                        await CloseAsync();
                        throw result.Exception;
                    }

                    //Analytics.TrackEvent("PlayerNavigation", new Dictionary<string, string>
                    //{
                    //    { "PageType",  pageName },
                    //    { "ViewType", "Secondary" },
                    //    { "CompactOverlay", (SecondaryAppView.ViewMode == ApplicationViewMode.CompactOverlay).ToString() },
                    //    { "FullScreen", SecondaryAppView.IsFullScreenMode.ToString() },
                    //});
                });

                await ShowAsync();

                IsShowSecondaryView = true;
            }
        }




        /// <summary>
        /// SecondaryViewを閉じます。
        /// </summary>
        public async Task CloseAsync()
        {
            if (!IsShowSecondaryView) { return; }

            await SecondaryCoreAppView.DispatcherQueue.EnqueueAsync(async () =>
            {
                await PlaylistPlayer.ClearAsync();

                SecondaryAppView.Title = "Hohoema";

                await SecondaryViewPlayerNavigationService.NavigateAsync(nameof(BlankPage), _BlankPageNavgationTransitionInfo);

                await Task.Delay(250);

                await SecondaryAppView.TryConsolidateAsync();
            });

            await ShowMainViewAsync();
        }


        public async Task ClearVideoPlayerAsync()
        {
            if (PlaylistPlayer == null) { return; }

            await SecondaryCoreAppView.DispatcherQueue.EnqueueAsync(async () =>
            {
                await PlaylistPlayer.ClearAsync();

                SecondaryAppView.Title = "Hohoema";
            });
        }

        public async Task ShowMainViewAsync()
        {
            var currentView = ApplicationView.GetForCurrentView();
            _scheduler.Schedule(async () => 
            {
                if (SecondaryAppView != null && currentView.Id == SecondaryAppView.Id)
                {
                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(MainViewId).AsTask();
                }
            });
        }

        public Task ShowAsync()
        {
            if (SecondaryAppView == null) { return Task.CompletedTask; }
            if (_onceSurpressActivation)
            {
                _onceSurpressActivation = false;
                return ShowMainViewAsync();
            }

            if (SecondaryAppView.ViewMode == ApplicationViewMode.CompactOverlay
                    || SecondaryAppView.IsFullScreenMode
                    )
            {
                return Task.CompletedTask;
            }

            return this.SecondaryCoreAppView.DispatcherQueue.EnqueueAsync(async () =>
            {
                //Window.Current.Activate();
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(SecondaryAppView.Id, ViewSizePreference.Default).AsTask();
            });
        }


        public async Task ToggleCompactOverlayAsync()
        {
            if (!IsShowSecondaryView) { return; }

            if (!SecondaryAppView.IsViewModeSupported(ApplicationViewMode.CompactOverlay)) { return; }

            await SecondaryCoreAppView.DispatcherQueue.EnqueueAsync(async () =>
            {
                if (SecondaryAppView.ViewMode == ApplicationViewMode.Default)
                {
                    var opt = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                    opt.CustomSize = new Windows.Foundation.Size(500, 280);
                    await SecondaryAppView.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, opt);
                    IsCompactOverlay = true;
                    IsFullScreen = false;
                }
                else
                {
                    await SecondaryAppView.TryEnterViewModeAsync(ApplicationViewMode.Default);
                    IsCompactOverlay = false;
                    IsFullScreen = false;
                }
            });
        }

        public async Task ToggleFullScreenAsync()
        {
            if (!IsShowSecondaryView) { return; }

            await SecondaryCoreAppView.DispatcherQueue.EnqueueAsync(() =>
            {
                if (SecondaryAppView.IsFullScreenMode)
                {
                    SecondaryAppView.ExitFullScreenMode();
                    IsCompactOverlay = false;
                    IsFullScreen = false;
                }
                else
                {
                    SecondaryAppView.TryEnterFullScreenMode();
                    IsCompactOverlay = false;
                    IsFullScreen = true;
                }
            });
        }


        ICommand IPlayerView.ToggleFullScreenCommand => ToggleFullScreenCommand;

        private RelayCommand _ToggleFullScreenCommand;
        public RelayCommand ToggleFullScreenCommand =>
            _ToggleFullScreenCommand ?? (_ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreenCommand));

        void ExecuteToggleFullScreenCommand()
        {
            _ = ToggleFullScreenAsync();
        }

        ICommand IPlayerView.ToggleCompactOverlayCommand => ToggleCompactOverlayCommand;

        private RelayCommand _ToggleCompactOverlayCommand;
        public RelayCommand ToggleCompactOverlayCommand =>
            _ToggleCompactOverlayCommand ??= new RelayCommand(ExecuteToggleCompactOverlayCommand);

        void ExecuteToggleCompactOverlayCommand()
        {
            _ = ToggleCompactOverlayAsync();
        }

        public Task<bool> TrySetDisplayModeAsync(PlayerDisplayMode mode)
        {
            throw new NotImplementedException();
        }

        public Task<PlayerDisplayMode> GetDisplayModeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsWindowFilledScreenAsync()
        {
            throw new NotImplementedException();
        }
    }
}

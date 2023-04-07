using Hohoema.Models.Application;
using Hohoema.Models.Playlist;
using Hohoema.Services;
using Hohoema.Views.Pages;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;
using Hohoema.Helpers;
using Hohoema.Navigations;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class AppWindowSecondaryViewPlayerManager : ObservableObject, IPlayerView
    {
        private readonly AppearanceSettings _appearanceSettings;
        private readonly HohoemaPlaylistPlayer _playlistPlayer;
        private readonly CurrentActiveWindowUIContextService _currentActiveWindowUIContextService;
        AppWindow _appWindow;
        private Border _rootBorder;
        private INavigationService _navigationService;
        private readonly DrillInNavigationTransitionInfo _PlayerPageNavgationTransitionInfo;
        private readonly SuppressNavigationTransitionInfo _BlankPageNavgationTransitionInfo;
        private readonly DispatcherQueue _dispatcherQueue;
        AsyncLock _appWindowUpdateLock = new AsyncLock();

        CancellationTokenSource _appWindowCloseCts;

        public AppWindowSecondaryViewPlayerManager(
            AppearanceSettings appearanceSettings,
            HohoemaPlaylistPlayer playlistPlayer,
            Services.CurrentActiveWindowUIContextService currentActiveWindowUIContextService
            )
        {
            _appearanceSettings = appearanceSettings;
            _playlistPlayer = playlistPlayer;
            _currentActiveWindowUIContextService = currentActiveWindowUIContextService;
            _PlayerPageNavgationTransitionInfo = new DrillInNavigationTransitionInfo();
            _BlankPageNavgationTransitionInfo = new SuppressNavigationTransitionInfo();

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            ApplicationView.GetForCurrentView().Consolidated += AppWindowSecondaryViewPlayerManager_Consolidated;
        }

        private void AppWindowSecondaryViewPlayerManager_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            _ = CloseAsync();
        }

        PlayerDisplayMode _displayMode;

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

        public string LastNavigatedPageName { get; private set; }

        public HohoemaPlaylistPlayer PlaylistPlayer => _playlistPlayer;

        ICommand _ToggleFullScreenCommand;
        public ICommand ToggleFullScreenCommand => _ToggleFullScreenCommand ??= new RelayCommand(() => _ = ToggleFullScreenAsync());

        public async Task ToggleFullScreenAsync()
        {
            if (!_appWindow.Presenter.IsPresentationSupported(AppWindowPresentationKind.FullScreen)) { return; }

            var currentConfig = _appWindow.Presenter.GetConfiguration();
            if (currentConfig.Kind is AppWindowPresentationKind.FullScreen)
            {
                _appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.Default);
                IsCompactOverlay = false;
                IsFullScreen = false;
                _displayMode = PlayerDisplayMode.FillWindow;
            }
            else
            {
                _appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.FullScreen);
                IsCompactOverlay = false;
                IsFullScreen = true;
                _displayMode = PlayerDisplayMode.FullScreen;
            }
        }


        ICommand _ToggleCompactOverlayCommand;
        private InputActivationListener _activationListener;

        public ICommand ToggleCompactOverlayCommand => _ToggleCompactOverlayCommand ??= new RelayCommand(() => _ = ToggleCompactOverlayAsync());

        public async Task ToggleCompactOverlayAsync()
        {
            if (!_appWindow.Presenter.IsPresentationSupported(AppWindowPresentationKind.CompactOverlay)) { return; }

            var currentConfig = _appWindow.Presenter.GetConfiguration();
            if (currentConfig.Kind is AppWindowPresentationKind.CompactOverlay)
            {
                _appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.Default);                
                IsCompactOverlay = false;
                IsFullScreen = false;
                _displayMode = PlayerDisplayMode.FillWindow;
            }
            else
            {
                _appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);
                _appWindow.RequestSize(new Size(500, 282));
                IsCompactOverlay = true;
                IsFullScreen = false;
                _displayMode = PlayerDisplayMode.CompactOverlay;
            }
        }

        public async Task ClearVideoPlayerAsync()
        {
            await PlaylistPlayer.ClearAsync();

            SetTitle("Hohoema");
        }

        public async Task CloseAsync()
        {
            using var _ = await _appWindowUpdateLock.LockAsync(default);

            if (_appWindow == null) { return; }
            _closingTaskCompletionSource = new TaskCompletionSource<int>();
            await _appWindow.CloseAsync();
            await _closingTaskCompletionSource.Task;
            _closingTaskCompletionSource = null;
        }

        TaskCompletionSource<int> _closingTaskCompletionSource;
        private DispatcherQueueTimer _displaySettingSaveTimer;

        private async Task EnsureCreateSecondaryView()
        {
            await _dispatcherQueue.EnqueueAsync(async () => 
            {
                using (await _appWindowUpdateLock.LockAsync(_appWindowCloseCts?.Token ?? default))
                {
                    if (_appWindow != null) { return; }

                    _appWindowCloseCts = new CancellationTokenSource();
                    _appWindow = await AppWindow.TryCreateAsync();
                    _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                    _appWindow.TitleBar.BackgroundColor = Colors.Transparent;
                    _appWindow.TitleBar.ButtonHoverBackgroundColor = Colors.Transparent;
                    _appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                    _appWindow.TitleBar.ButtonPressedBackgroundColor = Colors.Transparent;
                    _appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;

                    

                    SecondaryWindowCoreLayout secondaryWindowCoreLayout = new();
                    _rootBorder = new Border();
                    _rootBorder.Child = secondaryWindowCoreLayout;
                    _navigationService = secondaryWindowCoreLayout.CreateNavigationService();
                    ElementCompositionPreview.SetAppWindowContent(_appWindow, _rootBorder);

                    _displaySettingSaveTimer = _dispatcherQueue.CreateTimer();
                    _displaySettingSaveTimer.Interval = TimeSpan.FromSeconds(1);
                    _displaySettingSaveTimer.Tick += (s, e) => 
                    {
                        if (!_appWindow.IsVisible) { return; }

                        var config = _appWindow.Presenter.GetConfiguration();
                        _appearanceSettings.IsSecondaryViewPrefferedCompactOverlay = config.Kind == AppWindowPresentationKind.CompactOverlay;

                        var windowPlacement = _appWindow.GetPlacement();

                        var regions = _appWindow.GetDisplayRegions();
                        _appearanceSettings.SecondaryViewDisplayRegionMonitorDeviceId = windowPlacement.DisplayRegion.DisplayMonitorDeviceId;
                        
                        if (windowPlacement.DisplayRegion.WorkAreaSize.Width > windowPlacement.Offset.X)
                        {

                        }

                        _appearanceSettings.SecondaryViewLastWindowPosition = windowPlacement.Offset;
                        _appearanceSettings.SecondaryViewLastWindowSize = windowPlacement.Size;

                        Debug.WriteLine($"{windowPlacement.Offset.X}, {windowPlacement.Offset.Y}");
                    };
                    
                    _appWindow.Closed += async (s, e) =>
                    {
                        _displaySettingSaveTimer.Stop();
                        _displaySettingSaveTimer = null;

                        _appWindowCloseCts.Cancel();
                        _appWindowCloseCts.Dispose();
                        _appWindowCloseCts = null;

                        _activationListener.InputActivationChanged -= ActivationListener_InputActivationChanged;
                        _activationListener.Dispose();
                        _rootBorder.Child = null;
                        _rootBorder = null;
                        _appWindow = null;
                        LastNavigatedPageName = null;
                        _displayMode = PlayerDisplayMode.Close;

                        await PlaylistPlayer.ClearAsync();
                        await _navigationService.NavigateAsync(nameof(BlankPage));

                        _closingTaskCompletionSource?.SetResult(0);
                    };

                    _appWindow.Changed += async (s, e) => 
                    {
                        await Task.Delay(500);

                        if (e.DidWindowPresentationChange || e.DidSizeChange)
                        {
                            var config = s.Presenter.GetConfiguration();
                            if (config.Kind == AppWindowPresentationKind.FullScreen)
                            {
                                _displayMode = PlayerDisplayMode.FullScreen;
                                IsFullScreen = true;
                                IsCompactOverlay = false;
                            }
                            else if (config.Kind == AppWindowPresentationKind.CompactOverlay)
                            {
                                _displayMode = PlayerDisplayMode.FullScreen;
                                IsFullScreen = false;
                                IsCompactOverlay = true;
                            }
                            else
                            {
                                _displayMode = PlayerDisplayMode.FillWindow;
                                IsFullScreen = false;
                                IsCompactOverlay = false;
                            }
                        }
                    };

                    SetupListenersForWindow(_appWindow);

                    _appWindow.Title = "Secondary Window!";
                    if (_appearanceSettings.SecondaryViewLastWindowPosition is not null and Point lastPos
                    && _appearanceSettings.SecondaryViewLastWindowSize is not null and Size lastSize
                    )
                    {

                        //_appWindow.RequestMoveToDisplayRegion(lastRegion);
//                        _appWindow.RequestSize(lastSize);
                    }

                    _displaySettingSaveTimer.Start();

                    await _appWindow.TryShowAsync();

                    await Task.Delay(1000);

                    var regions = _appWindow.GetDisplayRegions();
                    foreach (var regionDeviceId in regions)
                    {
                        Debug.WriteLine($"{regionDeviceId.DisplayMonitorDeviceId}");
                        Debug.WriteLine($"{regionDeviceId.WorkAreaOffset}");
                        Debug.WriteLine($"{regionDeviceId.WorkAreaSize}");
                    }

                    DisplayRegion lastRegion = regions.FirstOrDefault(x => x.DisplayMonitorDeviceId == _appearanceSettings.SecondaryViewDisplayRegionMonitorDeviceId)
                        ?? regions.First();

                    Debug.WriteLine("target monitorId: " + lastRegion.DisplayMonitorDeviceId);

                    if (_appearanceSettings.IsSecondaryViewPrefferedCompactOverlay)
                    {
                        _appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);
                        _appWindow.RequestSize(new Size(500, 282));
                        IsCompactOverlay = true;
                    }

                    _appWindow.RequestMoveRelativeToDisplayRegion(lastRegion, lastPos);
                }
            });
        }

        void SetupListenersForWindow(AppWindow window)
        {
            _activationListener = InputActivationListenerPreview.CreateForApplicationWindow(window);
            _activationListener.InputActivationChanged += ActivationListener_InputActivationChanged;
        }

        private void ActivationListener_InputActivationChanged(InputActivationListener sender, InputActivationListenerActivationChangedEventArgs args)
        {
            switch (args.State)
            {
                case InputActivationState.ActivatedInForeground:
                    // The user will be interacting with this window, so make sure the full user experience is running
                    CurrentActiveWindowUIContextService.SetUIContext(_currentActiveWindowUIContextService, _rootBorder.UIContext, _rootBorder.XamlRoot);
                    break;
                case InputActivationState.ActivatedNotForeground:
                    // The window is showing, but the user is interacting with another window, adjust accordingly
                    break;
                case InputActivationState.Deactivated:
                    // The user moved on, they have switched to another window, time to go back to inactive state.
                    break;
                default:
                    break;
            }
        }

        public async Task NavigationAsync(string pageName, INavigationParameters parameters)
        {
            await EnsureCreateSecondaryView();
            
            try
            {
                await _dispatcherQueue.EnqueueAsync(async () =>
                {
                    using (var _ = await _appWindowUpdateLock.LockAsync(_appWindowCloseCts?.Token ?? default))
                    {
                        var result = await _navigationService.NavigateAsync(pageName, parameters, _PlayerPageNavgationTransitionInfo);
                        if (!result.IsSuccess)
                        {
                            Debug.WriteLine(result.Exception?.ToString());
                            throw result.Exception;
                        }
                    }
                });

                LastNavigatedPageName = pageName;

                await ShowAsync();
            }
            catch
            {
                await CloseAsync();
            }            
        }

        public void SetTitle(string title)
        {
            _dispatcherQueue.TryEnqueue(async () => 
            {
                using (var _ = await _appWindowUpdateLock.LockAsync(_appWindowCloseCts?.Token ?? default))
                {
                    if (_appWindow != null)
                    {
                        _appWindow.Title = title;
                    }
                }
            });
        }

        public async Task ShowAsync()
        {
            using (var _ = await _appWindowUpdateLock.LockAsync(_appWindowCloseCts?.Token ?? default))
            {
                if (_appWindow == null) { return; }

                await _dispatcherQueue.EnqueueAsync(async () =>
                {
                    await _appWindow.TryShowAsync();
                });
            }        
        }

        public async Task<bool> TrySetDisplayModeAsync(PlayerDisplayMode mode)
        {
            if (_displayMode == PlayerDisplayMode.Close && mode != PlayerDisplayMode.Close)
            {
                await ShowAsync();
            }

            switch (mode)
            {
                case PlayerDisplayMode.Close:
                    await CloseAsync();
                    break;
                case PlayerDisplayMode.FillWindow:
                    if (IsCompactOverlay)
                    {
                        await ToggleCompactOverlayAsync();
                    }
                    else if (IsFullScreen)
                    {
                        await ToggleFullScreenAsync();
                    }
                    break;
                case PlayerDisplayMode.FullScreen:
                    if (IsFullScreen is false)
                    {
                        await ToggleFullScreenAsync();
                    }
                    break;
                case PlayerDisplayMode.WindowInWindow:
                    return false;
                case PlayerDisplayMode.CompactOverlay:
                    if (IsCompactOverlay is false)
                    {
                        await ToggleCompactOverlayAsync();
                    }
                    break;
            }

            return true;
        }

        public Task<PlayerDisplayMode> GetDisplayModeAsync()
        {
            return Task.FromResult(_displayMode);
        }

        public Task<bool> IsWindowFilledScreenAsync()
        {
            var windowPlacement = _appWindow.GetPlacement();
            var region = windowPlacement.DisplayRegion;
            bool isFilled = windowPlacement.Offset == region.WorkAreaOffset
                && windowPlacement.Size == region.WorkAreaSize;

            return Task.FromResult(isFilled);
        }
        
    }
}

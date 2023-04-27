#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Contracts.Services.Player;
using Hohoema.Helpers;
using Hohoema.Models.Application;
using Hohoema.Models.Playlist;
using Hohoema.Views.Pages;
using Microsoft.Toolkit.Uwp;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
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

namespace Hohoema.Services.Player;

public sealed partial class AppWindowSecondaryViewPlayerManager : ObservableObject, IPlayerView
{
    private readonly AppearanceSettings _appearanceSettings;
    private readonly HohoemaPlaylistPlayer _playlistPlayer;
    private readonly CurrentActiveWindowUIContextService _currentActiveWindowUIContextService;
    private AppWindow _appWindow;
    private Border _rootBorder;
    private INavigationService _navigationService;
    private readonly DrillInNavigationTransitionInfo _PlayerPageNavgationTransitionInfo;
    private readonly SuppressNavigationTransitionInfo _BlankPageNavgationTransitionInfo;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly AsyncLock _appWindowUpdateLock = new AsyncLock();

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

    private InputActivationListener _activationListener;

    public HohoemaPlaylistPlayer PlaylistPlayer => _playlistPlayer;

    ICommand IPlayerView.ToggleFullScreenCommand => ToggleFullScreenCommand;

    [RelayCommand]
    public void ToggleFullScreen()
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


    ICommand IPlayerView.ToggleCompactOverlayCommand => ToggleCompactOverlayCommand;

    [RelayCommand]
    public void ToggleCompactOverlay()
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
                
                _appWindow.Closed += async (s, e) =>
                {
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

                _appWindow.Changed += (s, e) => 
                {                    
                    Debug.WriteLine($"{nameof(e.DidFrameChange)}: {e.DidFrameChange}");
                    Debug.WriteLine($"{nameof(e.DidWindowPresentationChange)}: {e.DidWindowPresentationChange}");
                    Debug.WriteLine($"{nameof(e.DidAvailableWindowPresentationsChange)}: {e.DidAvailableWindowPresentationsChange}");
                    Debug.WriteLine($"{nameof(e.DidTitleBarChange)}: {e.DidTitleBarChange}");
                    Debug.WriteLine($"{nameof(e.DidSizeChange)}: {e.DidSizeChange}");
                    Debug.WriteLine($"{nameof(e.DidVisibilityChange)}: {e.DidVisibilityChange}");
                    Debug.WriteLine($"{nameof(e.DidDisplayRegionsChange)}: {e.DidDisplayRegionsChange}");

                    var config = s.Presenter.GetConfiguration();

                    Debug.WriteLine($"Config{nameof(config.Kind)}: {config.Kind}");
                    if (e.DidWindowPresentationChange || e.DidSizeChange)
                    {
                        
                        if (config.Kind == AppWindowPresentationKind.FullScreen)
                        {                            
                            _displayMode = PlayerDisplayMode.FullScreen;
                            IsFullScreen = true;
                            IsCompactOverlay = false;
                        }
                        else if (config.Kind == AppWindowPresentationKind.CompactOverlay)
                        {
                            _displayMode = PlayerDisplayMode.CompactOverlay;
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

                    Debug.WriteLine($"{nameof(_appWindow.IsVisible)}: {_appWindow.IsVisible}");
                    if (!_appWindow.IsVisible) { return; }

                    var windowPlacement = _appWindow.GetPlacement();

                    Debug.WriteLine($"{nameof(windowPlacement.Size)}: {windowPlacement.Size}");
                    Debug.WriteLine($"{nameof(windowPlacement.Offset)}: {windowPlacement.Offset}");
                    Debug.WriteLine($"{nameof(windowPlacement.DisplayRegion.DisplayMonitorDeviceId)}: {windowPlacement.DisplayRegion.DisplayMonitorDeviceId}");
                    Debug.WriteLine($"WindowingEnvironment{nameof(windowPlacement.DisplayRegion.WindowingEnvironment.Kind)}: {windowPlacement.DisplayRegion.WindowingEnvironment.Kind}");
                    Debug.WriteLine($"{nameof(windowPlacement.DisplayRegion.WorkAreaSize)}: {windowPlacement.DisplayRegion.WorkAreaSize}");
                    Debug.WriteLine($"{nameof(windowPlacement.DisplayRegion.WorkAreaOffset)}: {windowPlacement.DisplayRegion.WorkAreaOffset}");
                    Debug.WriteLine($"{nameof(windowPlacement.DisplayRegion.IsVisible)}: {windowPlacement.DisplayRegion.IsVisible}");

                    if (e.DidDisplayRegionsChange || e.DidWindowPresentationChange || e.DidSizeChange || e.DidVisibilityChange)
                    {
                        _appearanceSettings.IsSecondaryViewPrefferedCompactOverlay = config.Kind == AppWindowPresentationKind.CompactOverlay;

                        var regions = _appWindow.GetDisplayRegions();
                        _appearanceSettings.SecondaryViewDisplayRegionMonitorDeviceId = windowPlacement.DisplayRegion.DisplayMonitorDeviceId;

                        if (windowPlacement.DisplayRegion.WorkAreaSize.Width > windowPlacement.Offset.X)
                        {

                        }

                        _appearanceSettings.SecondaryViewLastWindowPosition = windowPlacement.Offset;
                        _appearanceSettings.SecondaryViewLastWindowSize = windowPlacement.Size;

                        Debug.WriteLine($"{windowPlacement.Offset.X}, {windowPlacement.Offset.Y}");
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

                await _appWindow.TryShowAsync();

                var regions = _appWindow.GetDisplayRegions();

#if DEBUG
                foreach (var regionDeviceId in regions)
                {
                    Debug.WriteLine($"{regionDeviceId.DisplayMonitorDeviceId}");
                    Debug.WriteLine($"{regionDeviceId.WorkAreaOffset}");
                    Debug.WriteLine($"{regionDeviceId.WorkAreaSize}");
                }
#endif

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
                    ToggleCompactOverlay();
                }
                else if (IsFullScreen)
                {
                    ToggleFullScreen();
                }
                break;
            case PlayerDisplayMode.FullScreen:
                if (IsFullScreen is false)
                {
                    ToggleFullScreen();
                }
                break;
            case PlayerDisplayMode.WindowInWindow:
                return false;
            case PlayerDisplayMode.CompactOverlay:
                if (IsCompactOverlay is false)
                {
                    ToggleCompactOverlay();
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

#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DryIoc;
using Hohoema.Contracts.Services.Player;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Views.Pages;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Animation;
using ZLogger;

namespace Hohoema.Services.Player;

public sealed partial class PrimaryViewPlayerManager : ObservableObject, IPlayerView
{
    INavigationService? _navigationService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ApplicationView _view;
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private readonly RestoreNavigationManager _restoreNavigationManager;
    PlayerDisplayMode _prevDisplayMode;

    Helpers.AsyncLock _navigationLock = new Helpers.AsyncLock();

    public PrimaryViewPlayerManager(
        ILoggerFactory loggerFactory,
        IScheduler scheduler,
        RestoreNavigationManager restoreNavigationManager,
        HohoemaPlaylistPlayer hohoemaPlaylistPlayer
        )
    {
        _dispatcherQueue =  DispatcherQueue.GetForCurrentThread();
        _view = ApplicationView.GetForCurrentView();
        _logger = loggerFactory.CreateLogger<PrimaryViewPlayerManager>();
        _scheduler = scheduler;
        _restoreNavigationManager = restoreNavigationManager;
        PlaylistPlayer = hohoemaPlaylistPlayer;
        _navigationService = null;

        this.ObserveProperty(x => x.DisplayMode, isPushCurrentValueAtFirst: false)
            .Subscribe(x => 
            {
                _prevDisplayMode = x;
                IsFullScreen = x == PlayerDisplayMode.FullScreen;
                IsCompactOverlay = x == PlayerDisplayMode.CompactOverlay;
            });

        _view.VisibleBoundsChanged += _view_VisibleBoundsChanged;
    }

    private void RefreshCurrentDisplayModeWhenExitFullScreenFromEmbedButton()
    {
        if (_view.IsFullScreenMode)
        {
            DisplayMode = PlayerDisplayMode.FullScreen;
        }
        else if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
        {
            DisplayMode = PlayerDisplayMode.CompactOverlay;
        }
        else if (DisplayMode is not PlayerDisplayMode.WindowInWindow and not PlayerDisplayMode.Close)
        {
            DisplayMode = PlayerDisplayMode.FillWindow;
        }
    }

    private void _view_VisibleBoundsChanged(ApplicationView sender, object args)
    {
        RefreshCurrentDisplayModeWhenExitFullScreenFromEmbedButton();
    }

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

    public void SetTitle(string title)
    {
        _view.Title = title;
    }

    public string LastNavigatedPageName { get; private set; } = string.Empty;

    public async Task ShowAsync()
    {
        await _dispatcherQueue.EnqueueAsync(async () => 
        {
            if (DisplayMode == PlayerDisplayMode.Close)
            {
                if (_view.IsFullScreenMode)
                {
                    DisplayMode = PlayerDisplayMode.FullScreen;
                }
                else if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
                {
                    DisplayMode = PlayerDisplayMode.CompactOverlay;
                }
                else
                {
                    if (_lastDisplayMode == PlayerDisplayMode.WindowInWindow)
                    {
                        DisplayMode = PlayerDisplayMode.WindowInWindow;
                    }
                    else
                    {
                        DisplayMode = PlayerDisplayMode.FillWindow;
                    }                        
                }                
            }

            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(_view.Id, ViewSizePreference.Default);
        });
    }



    private readonly DrillInNavigationTransitionInfo _playerTransisionAnimation = new DrillInNavigationTransitionInfo();
    public async Task NavigationAsync(string pageName, INavigationParameters parameters)
    {
        await _dispatcherQueue.EnqueueAsync(async () =>
        {
            using var _ = await _navigationLock.LockAsync();

            _navigationService ??= App.Current.Container.Resolve<INavigationService>("PrimaryPlayerNavigationService");

            try
            {
                var result = await _navigationService.NavigateAsync(pageName, parameters, _playerTransisionAnimation);
                if (!result.IsSuccess)
                {
                    DisplayMode = PlayerDisplayMode.Close;
                    _view.Title = string.Empty;
                    throw result.Exception ?? new Infra.HohoemaException("unknown navigation error.");
                }

                LastNavigatedPageName = pageName;

                _logger.ZLogInformationWithPayload(new Dictionary<string, string>
                {
                    { "PageType",  pageName },
                    { "DisplayMode", DisplayMode.ToString() },
                    { "ViewType", "Primary" },
                    { "CompactOverlay", (_view.ViewMode == ApplicationViewMode.CompactOverlay).ToString() },
                    { "FullScreen", _view.IsFullScreenMode.ToString() }
                }, "PlayerNavigation");
            }
            catch (Exception e)
            {
                _logger.ZLogErrorWithPayload(e, new Dictionary<string, string>()
                {
                    { "PageName", pageName },
                    { "Parameters", JsonSerializer.Serialize(parameters) },
                }
                , "PrimaryViewPlayer navigation failed."
                );
            }
            
        });
    }


    private PlayerDisplayMode _DisplayMode;
    public PlayerDisplayMode DisplayMode
    {
        get { return _DisplayMode; }
        private set { SetProperty(ref _DisplayMode, value); }
    }

    PlayerDisplayMode _lastDisplayMode;

    [RelayCommand]
    public async Task CloseAsync()
    {
        if (DisplayMode == PlayerDisplayMode.Close) { return; }

        _lastDisplayMode = DisplayMode;
        if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
        {
            await _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
        }

        // 先に表示を更新することでユーザーから応答性がよく見える
        // 特に低スペック端末では処理順による差が顕著に表れます
        DisplayMode = PlayerDisplayMode.Close;
        LastNavigatedPageName = string.Empty;
        IsFullScreen = false;
        IsCompactOverlay = false;
        _view.Title = string.Empty;
        await PlaylistPlayer.ClearAsync();
        _restoreNavigationManager.ClearCurrentPlayerEntry();
    }

    [RelayCommand]
    public async Task ClearVideoPlayerAsync()
    {
        await PlaylistPlayer.ClearAsync();
    }

    [RelayCommand]
    public async Task ShowWithFillAsync()
    {
        if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
        {
            await _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
        }        

        DisplayMode = PlayerDisplayMode.FillWindow;
    }
    
    [RelayCommand]
    public async Task ShowWithWindowInWindowAsync()
    {
        if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
        {
            await _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
        }

        if (_view.IsFullScreenMode
            && ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen
            )
        {
            _view.ExitFullScreenMode();
        }

        DisplayMode = PlayerDisplayMode.WindowInWindow;
    }

    [RelayCommand]
    public Task ShowWithFullScreenAsync()
    {
        if (!_view.IsFullScreenMode)
        {
            _view.TryEnterFullScreenMode();
        }

        DisplayMode = PlayerDisplayMode.FullScreen;

        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task ExistFullScreenAsync()
    {
        if (_view.IsFullScreenMode)
        {
            _view.ExitFullScreenMode();
        }

        DisplayMode = PlayerDisplayMode.FillWindow;

        return Task.CompletedTask;
    }

    [RelayCommand]
    public async Task ShowWithCompactOverlayAsync()
    {
        if (_view.IsViewModeSupported(ApplicationViewMode.CompactOverlay))
        {
            if (_view.ViewMode == ApplicationViewMode.Default)
            {
                var opt = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                opt.CustomSize = new Windows.Foundation.Size(500, 280);
                await _view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, opt);
            }
            else
            {
                await _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }
        }

        DisplayMode = PlayerDisplayMode.CompactOverlay;
    }

    [RelayCommand]
    public async Task ExitCompactOverlayAsync()
    {
        if (_view.IsViewModeSupported(ApplicationViewMode.CompactOverlay))
        {
            if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                await _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }
        }

        DisplayMode = PlayerDisplayMode.FillWindow;
    }

    //[RelayCommand]
    public async Task SetCloseAsync()
    {
        if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
        {
            await _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
        }

        DisplayMode = PlayerDisplayMode.Close;

        _navigationService?.NavigateAsync(nameof(BlankPage));
    }


    [RelayCommand]
    async Task ToggleFillOrWindowInWindow()
    {
        if (DisplayMode == PlayerDisplayMode.WindowInWindow)
        {
            await ShowWithFillAsync();
        }
        else
        {
            await ShowWithWindowInWindowAsync();
        }
    }

    ICommand IPlayerView.ToggleFullScreenCommand => ToggleFullScreenCommand;

    [RelayCommand]
    async Task ToggleFullScreen()
    {
        if (DisplayMode == PlayerDisplayMode.Close) { return; }

        if (DisplayMode == PlayerDisplayMode.FullScreen)
        {
            await ExistFullScreenAsync();
        }
        else
        {
            await ShowWithFullScreenAsync();
        }
    }

    ICommand IPlayerView.ToggleCompactOverlayCommand => ToggleCompactOverlayCommand;

    [RelayCommand]
    async Task ToggleCompactOverlay()
    {
        if (DisplayMode == PlayerDisplayMode.Close) { return; }

        if (DisplayMode == PlayerDisplayMode.CompactOverlay)
        {
            await ExitCompactOverlayAsync();
        }
        else
        {
            await ShowWithCompactOverlayAsync();
        }
    }

    public async Task<bool> TrySetDisplayModeAsync(PlayerDisplayMode mode)
    {
        switch (mode)
        {
            case PlayerDisplayMode.Close:
                await SetCloseAsync();
                break;
            case PlayerDisplayMode.FillWindow:
                await ShowWithFillAsync();
                break;
            case PlayerDisplayMode.FullScreen:
                await ShowWithFullScreenAsync();
                break;
            case PlayerDisplayMode.WindowInWindow:
                await ShowWithWindowInWindowAsync();
                break;
            case PlayerDisplayMode.CompactOverlay:
                await ShowWithCompactOverlayAsync();
                break;
        }

        return true;
    }

    public Task<PlayerDisplayMode> GetDisplayModeAsync()
    {
        return Task.FromResult(DisplayMode);
    }

    public Task<bool> IsWindowFilledScreenAsync()
    {
        return Task.FromResult(ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default);
    }

    public HohoemaPlaylistPlayer PlaylistPlayer { get; }
}

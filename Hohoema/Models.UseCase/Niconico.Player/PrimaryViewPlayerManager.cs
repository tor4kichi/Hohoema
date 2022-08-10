using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Animation;
using CommunityToolkit.Mvvm.Input;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Diagnostics;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Presentation.Views.Player;
using Hohoema.Presentation.Views.Pages;
using NiconicoToolkit.Video;
using NiconicoToolkit.Live;
using Hohoema.Models.Domain.Playlist;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using ZLogger;
using System.Text.Json;
using Hohoema.Presentation.Navigations;
using DryIoc;
using Windows.System;
using Microsoft.Toolkit.Uwp;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class PrimaryViewPlayerManager : ObservableObject, IPlayerView
    {
        INavigationService _navigationService;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly ApplicationView _view;
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
 //       private readonly Lazy<INavigationService> _navigationServiceLazy;
        private readonly RestoreNavigationManager _restoreNavigationManager;
        PlayerDisplayMode _prevDisplayMode;

        Models.Helpers.AsyncLock _navigationLock = new Models.Helpers.AsyncLock();

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
            //_navigationServiceLazy = navigationServiceLazy;
            _restoreNavigationManager = restoreNavigationManager;
            PlaylistPlayer = hohoemaPlaylistPlayer;
            _navigationService = null;

            this.ObserveProperty(x => x.DisplayMode, isPushCurrentValueAtFirst: false)
                .Subscribe(x => 
                {
                    SetDisplayMode(_prevDisplayMode, x);
                    _prevDisplayMode = x;
                    IsFullScreen = x == PlayerDisplayMode.FullScreen;
                    IsCompactOverlay = x == PlayerDisplayMode.CompactOverlay;
                });
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

        public string LastNavigatedPageName { get; private set; }

        public async Task ShowAsync()
        {
            await _dispatcherQueue.EnqueueAsync(async () => 
            {
                if (DisplayMode == PlayerDisplayMode.Close)
                {
                    DisplayMode = PlayerDisplayMode.FillWindow;
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
                
                if (_navigationService == null)
                {
                    _navigationService = App.Current.Container.Resolve<INavigationService>("PrimaryPlayerNavigationService");
                }

                try
                {
                    var result = await _navigationService.NavigateAsync(pageName, parameters, _playerTransisionAnimation);
                    if (!result.IsSuccess)
                    {
                        DisplayMode = PlayerDisplayMode.Close;
                        _view.Title = string.Empty;
                        throw result.Exception ?? new Models.Infrastructure.HohoemaExpception("unknown navigation error.");
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

            await Task.Delay(50);

            using (await _navigationLock.LockAsync()) { }
        }


        private PlayerDisplayMode _DisplayMode;
        public PlayerDisplayMode DisplayMode
        {
            get { return _DisplayMode; }
            set { SetProperty(ref _DisplayMode, value); }
        }

        public async Task CloseAsync()
        {
            if (DisplayMode == PlayerDisplayMode.Close) { return; }

            await PlaylistPlayer.ClearAsync();
            LastNavigatedPageName = null;
            DisplayMode = PlayerDisplayMode.Close;
            IsFullScreen = false;
            IsCompactOverlay = false;
            _view.Title = string.Empty;
            _restoreNavigationManager.ClearCurrentPlayerEntry();
        }

        public async Task ClearVideoPlayerAsync()
        {
            await PlaylistPlayer.ClearAsync();
        }

        public void ShowWithFill()
        {
            DisplayMode = PlayerDisplayMode.FillWindow;
        }

        public void ShowWithWindowInWindow()
        {
            DisplayMode = PlayerDisplayMode.WindowInWindow;
        }

        public void ShowWithFullScreen()
        {
            DisplayMode = PlayerDisplayMode.FullScreen;
        }

        public void ShowWithCompactOverlay()
        {
            DisplayMode = PlayerDisplayMode.CompactOverlay;
        }

        void SetDisplayMode(PlayerDisplayMode old, PlayerDisplayMode mode)
        {
            var currentMode = old;
            switch (mode)
            {
                case PlayerDisplayMode.Close:
                    SetClose(currentMode);
                    break;
                case PlayerDisplayMode.FillWindow:
                    SetFill(currentMode);
                    break;
                case PlayerDisplayMode.WindowInWindow:
                    SetWindowInWindow(currentMode);
                    break;
                case PlayerDisplayMode.FullScreen:
                    SetFullScreen(currentMode);
                    break;
                case PlayerDisplayMode.CompactOverlay:
                    SetCompactOverlay(currentMode);
                    break;
                default:
                    break;
            }
        }

        void SetClose(PlayerDisplayMode currentMode)
        {
            if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }

            if (_view.IsFullScreenMode 
                && ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen)
            {
                _view.ExitFullScreenMode();
            }            

            _navigationService.NavigateAsync(nameof(BlankPage));
        }

        void SetFill(PlayerDisplayMode currentMode)
        {
            if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }

            if (_view.IsFullScreenMode
                && ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen
                )
            {
                _view.ExitFullScreenMode();
            }
        }

        void SetWindowInWindow(PlayerDisplayMode currentMode)
        {
            if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }

            if (_view.IsFullScreenMode
                && ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen
                )
            {
                //_view.ExitFullScreenMode();
            }
        }

        void SetFullScreen(PlayerDisplayMode currentMode)
        {
            if (!_view.IsFullScreenMode)
            {
                _view.TryEnterFullScreenMode();
            }
        }

        void SetCompactOverlay(PlayerDisplayMode currentMode)
        {
            if (_view.IsViewModeSupported(ApplicationViewMode.CompactOverlay))
            {
                if (_view.ViewMode == ApplicationViewMode.Default)
                {
                    var opt = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);
                    opt.CustomSize = new Windows.Foundation.Size(500, 280);
                    _ = _view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay, opt);
                }
                else
                {
                    _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                }
            }
        }


        RelayCommand _closeCommand;
        public RelayCommand CloseCommand => _closeCommand 
            ?? (_closeCommand = new RelayCommand(async () => await CloseAsync()));

        RelayCommand _WindowInWindowCommand;
        public RelayCommand WindowInWindowCommand => _WindowInWindowCommand
            ?? (_WindowInWindowCommand = new RelayCommand(ShowWithWindowInWindow));

        RelayCommand _FillCommand;
        public RelayCommand FillCommand => _FillCommand
            ?? (_FillCommand = new RelayCommand(ShowWithFill));


        RelayCommand _ToggleFillOrWindowInWindowCommand;
        public RelayCommand ToggleFillOrWindowInWindowCommand => _ToggleFillOrWindowInWindowCommand
            ?? (_ToggleFillOrWindowInWindowCommand = new RelayCommand(() =>
            {
                if (DisplayMode == PlayerDisplayMode.FillWindow)
                {
                    ShowWithWindowInWindow();
                }
                else if (DisplayMode == PlayerDisplayMode.WindowInWindow)
                {
                    ShowWithFill();
                }
            }));

        ICommand IPlayerView.ToggleFullScreenCommand => ToggleFullScreenCommand;

        private RelayCommand _ToggleFullScreenCommand;
        public RelayCommand ToggleFullScreenCommand =>
            _ToggleFullScreenCommand ?? (_ToggleFullScreenCommand = new RelayCommand(ExecuteToggleFullScreenCommand));

        void ExecuteToggleFullScreenCommand()
        {
            if (DisplayMode == PlayerDisplayMode.Close) { return; }

            if (DisplayMode == PlayerDisplayMode.FullScreen)
            {                
                ShowWithFill();
            }
            else
            {
                ShowWithFullScreen();
            }
        }

        ICommand IPlayerView.ToggleCompactOverlayCommand => ToggleCompactOverlayCommand;

        private RelayCommand _ToggleCompactOverlayCommand;
        public RelayCommand ToggleCompactOverlayCommand =>
            _ToggleCompactOverlayCommand ??= new RelayCommand(ExecuteToggleCompactOverlayCommand);

        void ExecuteToggleCompactOverlayCommand()
        {
            if (DisplayMode == PlayerDisplayMode.Close) { return; }

            if (DisplayMode == PlayerDisplayMode.CompactOverlay)
            {
                ShowWithFill();
            }
            else
            {
                ShowWithCompactOverlay();
            }
        }

        public Task<bool> TrySetDisplayModeAsync(PlayerDisplayMode mode)
        {
            SetDisplayMode(DisplayMode, mode);

            return Task.FromResult(true);
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
}

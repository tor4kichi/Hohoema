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

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public enum PrimaryPlayerDisplayMode
    {
        Close,
        Fill,
        WindowInWindow,
        FullScreen,
        CompactOverlay,        
    }


    public sealed class PrimaryViewPlayerManager : ObservableObject, IPlayerView
    {
        INavigationService _navigationService;

        private ApplicationView _view;
        private readonly ILogger _logger;
        IScheduler _scheduler;
 //       private readonly Lazy<INavigationService> _navigationServiceLazy;
        private readonly RestoreNavigationManager _restoreNavigationManager;
        PrimaryPlayerDisplayMode _prevDisplayMode;

        Models.Helpers.AsyncLock _navigationLock = new Models.Helpers.AsyncLock();

        public PrimaryViewPlayerManager(
            ILoggerFactory loggerFactory,
            IScheduler scheduler,
            RestoreNavigationManager restoreNavigationManager,
            HohoemaPlaylistPlayer hohoemaPlaylistPlayer
            )
        {
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
                    IsFullScreen = x == PrimaryPlayerDisplayMode.FullScreen;
                    IsCompactOverlay = x == PrimaryPlayerDisplayMode.CompactOverlay;
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
            _scheduler.Schedule(async () => 
            {
                await ApplicationViewSwitcher.TryShowAsStandaloneAsync(_view.Id, ViewSizePreference.Default);
            });
        }

        private readonly DrillInNavigationTransitionInfo _playerTransisionAnimation = new DrillInNavigationTransitionInfo();
        public async Task NavigationAsync(string pageName, INavigationParameters parameters)
        {
            _scheduler.Schedule(async () =>
            {
                using var _ = await _navigationLock.LockAsync();
                
                if (DisplayMode == PrimaryPlayerDisplayMode.Close)
                {
                    DisplayMode = _lastPlayedDisplayMode;
                }

                if (_navigationService == null)
                {
                    _navigationService = App.Current.Container.Resolve<INavigationService>("PrimaryPlayerNavigationService");
                }

                try
                {
                    var result = await _navigationService.NavigateAsync(pageName, parameters, _playerTransisionAnimation);
                    if (!result.IsSuccess)
                    {
                        DisplayMode = PrimaryPlayerDisplayMode.Close;
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

        PrimaryPlayerDisplayMode _lastPlayedDisplayMode = PrimaryPlayerDisplayMode.Fill;


        private PrimaryPlayerDisplayMode _DisplayMode;
        public PrimaryPlayerDisplayMode DisplayMode
        {
            get { return _DisplayMode; }
            set { SetProperty(ref _DisplayMode, value); }
        }

        public async Task CloseAsync()
        {
            if (DisplayMode == PrimaryPlayerDisplayMode.Close) { return; }

            await PlaylistPlayer.ClearAsync();
            LastNavigatedPageName = null;
            _lastPlayedDisplayMode = DisplayMode == PrimaryPlayerDisplayMode.Close ? _lastPlayedDisplayMode : DisplayMode;
            DisplayMode = PrimaryPlayerDisplayMode.Close;
            _view.Title = string.Empty;
            _restoreNavigationManager.ClearCurrentPlayerEntry();
        }

        public async Task ClearVideoPlayerAsync()
        {
            await PlaylistPlayer.ClearAsync();
        }

        public void ShowWithFill()
        {
            DisplayMode = PrimaryPlayerDisplayMode.Fill;
        }

        public void ShowWithWindowInWindow()
        {
            DisplayMode = PrimaryPlayerDisplayMode.WindowInWindow;
        }

        public void ShowWithFullScreen()
        {
            DisplayMode = PrimaryPlayerDisplayMode.FullScreen;
        }

        public void ShowWithCompactOverlay()
        {
            DisplayMode = PrimaryPlayerDisplayMode.CompactOverlay;
        }

        void SetDisplayMode(PrimaryPlayerDisplayMode old, PrimaryPlayerDisplayMode mode)
        {
            var currentMode = old;
            switch (mode)
            {
                case PrimaryPlayerDisplayMode.Close:
                    SetClose(currentMode);
                    break;
                case PrimaryPlayerDisplayMode.Fill:
                    SetFill(currentMode);
                    break;
                case PrimaryPlayerDisplayMode.WindowInWindow:
                    SetWindowInWindow(currentMode);
                    break;
                case PrimaryPlayerDisplayMode.FullScreen:
                    SetFullScreen(currentMode);
                    break;
                case PrimaryPlayerDisplayMode.CompactOverlay:
                    SetCompactOverlay(currentMode);
                    break;
                default:
                    break;
            }
        }

        void SetClose(PrimaryPlayerDisplayMode currentMode)
        {
            if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }

            if (_view.IsFullScreenMode 
                && ApplicationView.PreferredLaunchWindowingMode != ApplicationViewWindowingMode.FullScreen)
            {
                //_view.ExitFullScreenMode();
            }

            _navigationService.NavigateAsync(nameof(BlankPage));
        }

        void SetFill(PrimaryPlayerDisplayMode currentMode)
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

        void SetWindowInWindow(PrimaryPlayerDisplayMode currentMode)
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

        void SetFullScreen(PrimaryPlayerDisplayMode currentMode)
        {
            if (!_view.IsFullScreenMode)
            {
                _view.TryEnterFullScreenMode();
            }
        }

        void SetCompactOverlay(PrimaryPlayerDisplayMode currentMode)
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
                if (DisplayMode == PrimaryPlayerDisplayMode.Fill)
                {
                    ShowWithWindowInWindow();
                }
                else if (DisplayMode == PrimaryPlayerDisplayMode.WindowInWindow)
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
            if (DisplayMode == PrimaryPlayerDisplayMode.Close) { return; }

            if (DisplayMode == PrimaryPlayerDisplayMode.FullScreen)
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
            if (DisplayMode == PrimaryPlayerDisplayMode.Close) { return; }

            if (DisplayMode == PrimaryPlayerDisplayMode.CompactOverlay)
            {
                ShowWithFill();
            }
            else
            {
                ShowWithCompactOverlay();
            }
        }


        public HohoemaPlaylistPlayer PlaylistPlayer { get; }
    }
}

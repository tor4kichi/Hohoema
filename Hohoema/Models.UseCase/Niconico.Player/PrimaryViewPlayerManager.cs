using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using System.Reactive.Concurrency;
using Prism.Mvvm;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media.Animation;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Diagnostics;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Live;
using Microsoft.AppCenter.Analytics;
using Hohoema.Presentation.Views.Player;
using Hohoema.Presentation.Views.Pages;
using NiconicoToolkit.Video;
using NiconicoToolkit.Live;
using Hohoema.Models.Domain.Playlist;
using System.Windows.Input;

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


    public sealed class PrimaryViewPlayerManager : Prism.Mvvm.BindableBase, IPlayerView
    {
        INavigationService _navigationService;

        private ApplicationView _view;
        IScheduler _scheduler;
        private readonly Lazy<INavigationService> _navigationServiceLazy;
        private readonly RestoreNavigationManager _restoreNavigationManager;
        PrimaryPlayerDisplayMode _prevDisplayMode;

        Models.Helpers.AsyncLock _navigationLock = new Models.Helpers.AsyncLock();

        public PrimaryViewPlayerManager(IScheduler scheduler,
            [Unity.Attributes.Dependency("PrimaryPlayerNavigationService")] Lazy<INavigationService> navigationServiceLazy,
            RestoreNavigationManager restoreNavigationManager,
            HohoemaPlaylistPlayer hohoemaPlaylistPlayer
            )
        {
            _view = ApplicationView.GetForCurrentView();
            _scheduler = scheduler;
            _navigationServiceLazy = navigationServiceLazy;
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

        public async Task NavigationAsync(string pageName, INavigationParameters parameters)
        {
            _scheduler.Schedule(async () =>
            {
                using (await _navigationLock.LockAsync())
                {
                    if (_navigationService == null)
                    {
                        _navigationService = _navigationServiceLazy.Value;
                    }

                    if (DisplayMode == PrimaryPlayerDisplayMode.Close)
                    {
                        DisplayMode = _lastPlayedDisplayMode;
                    }

                    try
                    {
                        var result = await _navigationService.NavigateAsync(pageName, parameters, new DrillInNavigationTransitionInfo());
                        if (!result.Success)
                        {
                            Debug.WriteLine(result.Exception?.ToString());
                            DisplayMode = PrimaryPlayerDisplayMode.Close;
                            _view.Title = string.Empty;
                            throw result.Exception ?? new Models.Infrastructure.HohoemaExpception("unknown navigation error.");
                        }

                        LastNavigatedPageName = pageName;

                        Analytics.TrackEvent("PlayerNavigation", new Dictionary<string, string>
                        {
                            { "PageType",  pageName },
                            { "DisplayMode", DisplayMode.ToString() },
                            { "ViewType", "Primary" },
                            { "CompactOverlay", (_view.ViewMode == ApplicationViewMode.CompactOverlay).ToString() },
                            { "FullScreen", _view.IsFullScreenMode.ToString() }
                        });
                    }
                    catch (Exception e)
                    {
                        ErrorTrackingManager.TrackError(e);
                    }
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
                //_view.ExitFullScreenMode();
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
                    _ = _view.TryEnterViewModeAsync(ApplicationViewMode.CompactOverlay);
                }
                else
                {
                    _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                }
            }
        }


        DelegateCommand _closeCommand;
        public DelegateCommand CloseCommand => _closeCommand 
            ?? (_closeCommand = new DelegateCommand(async () => await CloseAsync()));

        DelegateCommand _WindowInWindowCommand;
        public DelegateCommand WindowInWindowCommand => _WindowInWindowCommand
            ?? (_WindowInWindowCommand = new DelegateCommand(ShowWithWindowInWindow));

        DelegateCommand _FillCommand;
        public DelegateCommand FillCommand => _FillCommand
            ?? (_FillCommand = new DelegateCommand(ShowWithFill));


        DelegateCommand _ToggleFillOrWindowInWindowCommand;
        public DelegateCommand ToggleFillOrWindowInWindowCommand => _ToggleFillOrWindowInWindowCommand
            ?? (_ToggleFillOrWindowInWindowCommand = new DelegateCommand(() =>
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

        private DelegateCommand _ToggleFullScreenCommand;
        public DelegateCommand ToggleFullScreenCommand =>
            _ToggleFullScreenCommand ?? (_ToggleFullScreenCommand = new DelegateCommand(ExecuteToggleFullScreenCommand));

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

        private DelegateCommand _ToggleCompactOverlayCommand;
        public DelegateCommand ToggleCompactOverlayCommand =>
            _ToggleCompactOverlayCommand ??= new DelegateCommand(ExecuteToggleCompactOverlayCommand);

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

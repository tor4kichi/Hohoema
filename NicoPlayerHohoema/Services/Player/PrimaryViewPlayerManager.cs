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

namespace NicoPlayerHohoema.Services.Player
{
    public enum PrimaryPlayerDisplayMode
    {
        Close,
        Fill,
        WindowInWindow,
        FullScreen,
        CompactOverlay,        
    }


    public sealed class PrimaryViewPlayerManager : BindableBase
    {
        INavigationService _navigationService;

        private ApplicationView _view;
        IScheduler _scheduler;
        private readonly Lazy<INavigationService> _navigationServiceLazy;
        PrimaryPlayerDisplayMode _prevDisplayMode;

        Models.Helpers.AsyncLock _navigationLock = new Models.Helpers.AsyncLock();

        public PrimaryViewPlayerManager(IScheduler scheduler,
            [Unity.Attributes.Dependency("PrimaryPlayerNavigationService")] Lazy<INavigationService> navigationServiceLazy
            )
        {
            _view = ApplicationView.GetForCurrentView();
            _scheduler = scheduler;
            _navigationServiceLazy = navigationServiceLazy;
            _navigationService = null;

            _displayMode = new ReactiveProperty<PrimaryPlayerDisplayMode>(_scheduler, mode:ReactivePropertyMode.DistinctUntilChanged);
            DisplayMode = _displayMode.ToReadOnlyReactiveProperty(eventScheduler: _scheduler);

            _displayMode.Subscribe(x => 
            {
                SetDisplayMode(_prevDisplayMode, x);
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

                    if (DisplayMode.Value == PrimaryPlayerDisplayMode.Close)
                    {
                        _displayMode.Value = PrimaryPlayerDisplayMode.Fill;
                    }

                    await _navigationService.NavigateAsync(pageName, parameters, new DrillInNavigationTransitionInfo());
                }
            });

            await Task.Delay(50);

            using (await _navigationLock.LockAsync()) { }
        }


        ReactiveProperty<PrimaryPlayerDisplayMode> _displayMode;
        public IReadOnlyReactiveProperty<PrimaryPlayerDisplayMode> DisplayMode { get; }

        public void Close()
        {
            _displayMode.Value = PrimaryPlayerDisplayMode.Close;
        }

        public void ShowWithFill()
        {
            _displayMode.Value = PrimaryPlayerDisplayMode.Fill;
        }

        public void ShowWithWindowInWindow()
        {
            _displayMode.Value = PrimaryPlayerDisplayMode.WindowInWindow;
        }

        public void ShowWithFullScreen()
        {
            _displayMode.Value = PrimaryPlayerDisplayMode.FullScreen;
        }

        public void ShowWithCompactOverlay()
        {
            _displayMode.Value = PrimaryPlayerDisplayMode.CompactOverlay;
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

            if (_view.IsFullScreenMode)
            {
                _view.ExitFullScreenMode();
            }

            _navigationService.NavigateAsync(nameof(Views.BlankPage));
        }

        void SetFill(PrimaryPlayerDisplayMode currentMode)
        {
            if (_view.ViewMode == ApplicationViewMode.CompactOverlay)
            {
                _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
            }

            if (_view.IsFullScreenMode)
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

            if (_view.IsFullScreenMode)
            {
                _view.ExitFullScreenMode();
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
                    _ = _view.TryEnterViewModeAsync(ApplicationViewMode.Default);
                }
            }
        }


        DelegateCommand _closeCommand;
        public DelegateCommand CloseCommand => _closeCommand 
            ?? (_closeCommand = new DelegateCommand(Close));

        DelegateCommand _WindowInWindowCommand;
        public DelegateCommand WindowInWindowCommand => _WindowInWindowCommand
            ?? (_WindowInWindowCommand = new DelegateCommand(ShowWithWindowInWindow));

        DelegateCommand _FillCommand;
        public DelegateCommand FillCommand => _FillCommand
            ?? (_FillCommand = new DelegateCommand(ShowWithFill));
    }
}

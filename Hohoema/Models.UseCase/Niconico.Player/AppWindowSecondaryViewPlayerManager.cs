using Hohoema.Models.Domain.Playlist;
using Hohoema.Presentation.Services;
using Hohoema.Presentation.Views.Pages;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Prism.Mvvm;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Uno.Threading;
using Windows.ApplicationModel.Core;
using Windows.System;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Input.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Animation;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class AppWindowSecondaryViewPlayerManager : BindableBase, IPlayerView
    {
        private readonly HohoemaPlaylistPlayer _playlistPlayer;
        private readonly CurrentActiveWindowUIContextService _currentActiveWindowUIContextService;
        AppWindow _appWindow;
        private Border _rootBorder;
        private INavigationService _navigationService;
        private readonly DrillInNavigationTransitionInfo _PlayerPageNavgationTransitionInfo;
        private readonly SuppressNavigationTransitionInfo _BlankPageNavgationTransitionInfo;
        private readonly DispatcherQueue _dispatcherQueue;
        FastAsyncLock _appWindowUpdateLock = new FastAsyncLock();

        CancellationTokenSource _appWindowCloseCts;

        public AppWindowSecondaryViewPlayerManager(
            HohoemaPlaylistPlayer playlistPlayer,
            Presentation.Services.CurrentActiveWindowUIContextService currentActiveWindowUIContextService
            )
        {
            _playlistPlayer = playlistPlayer;
            _currentActiveWindowUIContextService = currentActiveWindowUIContextService;
            _PlayerPageNavgationTransitionInfo = new DrillInNavigationTransitionInfo();
            _BlankPageNavgationTransitionInfo = new SuppressNavigationTransitionInfo();

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            ApplicationView.GetForCurrentView().Consolidated += AppWindowSecondaryViewPlayerManager_Consolidated; ;
        }

        private void AppWindowSecondaryViewPlayerManager_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            _ = CloseAsync();
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
            }
            else
            {
                _appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.FullScreen);
                IsCompactOverlay = false;
                IsFullScreen = true;
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
            }
            else
            {
                _appWindow.Presenter.RequestPresentation(AppWindowPresentationKind.CompactOverlay);
                IsCompactOverlay = true;
                IsFullScreen = false;
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

        private async Task EnsureCreateSecondaryView()
        {
            await _dispatcherQueue.EnqueueAsync(async () => 
            {
                using (await _appWindowUpdateLock.LockAsync(_appWindowCloseCts?.Token ?? default))
                {
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

                        await PlaylistPlayer.ClearAsync();
                        await _navigationService.NavigateAsync(nameof(BlankPage));

                        _closingTaskCompletionSource?.SetResult(0);
                    };

                    SetupListenersForWindow(_appWindow);

                    _appWindow.Title = "Secondary Window!";
                    await _appWindow.TryShowAsync();
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
                        if (!result.Success)
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
            finally
            {
                _dispatcherQueue.TryEnqueue(() => 
                {
                    var presenterConfig = _appWindow.Presenter.GetConfiguration();
                    Analytics.TrackEvent("PlayerNavigation", new Dictionary<string, string>
                    {
                        { "PageType",  pageName },
                        { "ViewType", "Secondary" },
                        { "CompactOverlay", (presenterConfig.Kind == AppWindowPresentationKind.CompactOverlay).ToString() },
                        { "FullScreen", (presenterConfig.Kind == AppWindowPresentationKind.FullScreen).ToString()},
                    });
                });
            }
        }

        public void SetTitle(string title)
        {
            if (_appWindow != null)
            {
                _appWindow.Title = title;
            }
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
    }
}

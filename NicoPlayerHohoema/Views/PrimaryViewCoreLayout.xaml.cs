using I18NPortable;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.ViewModels;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace NicoPlayerHohoema.Views
{
    public sealed partial class PrimaryWindowCoreLayout : UserControl
    {
        private readonly PrimaryWindowCoreLayoutViewModel _viewModel;

        CoreDispatcher _dispatcher;
        public PrimaryWindowCoreLayout(PrimaryWindowCoreLayoutViewModel viewModel)
        {
            DataContext = _viewModel = viewModel;
            this.InitializeComponent();

            _dispatcher = Dispatcher;

            ContentFrame.NavigationFailed += (_, e) =>
            {
                Debug.WriteLine("Page navigation failed!!");
                Debug.WriteLine(e.SourcePageType.AssemblyQualifiedName);
                Debug.WriteLine(e.Exception.ToString());

                _ = (App.Current as App).OutputErrorFile(e.Exception, e.SourcePageType?.AssemblyQualifiedName);
            };


            ContentFrame.Navigated += (_, e) =>
            {
                var pageNameRaw = e.SourcePageType.FullName.Split('.').LastOrDefault();
                var pageName = pageNameRaw.Split('_').FirstOrDefault();
                if (Enum.TryParse(pageName.Substring(0, pageName.Length - 4), out HohoemaPageType pageType))
                {
                    PageTitle = pageType.Translate();
                }
            };

            ContentFrame.Navigating += ContentFrame_Navigating;

            _viewModel.EventAggregator.GetEvent<PageNavigationEvent>()
                .Subscribe(args =>
                {
                    _ = ContentFrameNavigation(args);
                });


            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;


            if (Services.Helpers.DeviceTypeHelper.IsDesktop)
            {
                Window.Current.SetTitleBar(DraggableContent as UIElement);
            }

            PlayerFrame.Navigated += PlayerFrame_Navigated;

        }

        // 狭い画面の時にメニュー項目を選択したらペインを閉じるようにする
        private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (ContentSplitView.DisplayMode == SplitViewDisplayMode.CompactOverlay ||
                ContentSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
            {
                ContentSplitView.IsPaneOpen = false;
            }
        }

        private void PlayerFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(VideoPlayerPage))
            {
                
            }
            else if (e.SourcePageType == typeof(LivePlayerPage))
            {

            }
            else
            {

            }
        }

        AsyncLock _navigationLock = new AsyncLock();

        async Task ContentFrameNavigation(PageNavigationEventArgs args)
        {
            var pageType = args.PageName;
            var parameter = args.Paramter;
            var behavior = args.Behavior;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                using (var releaser = await _navigationLock.LockAsync())
                {
                    // メインウィンドウでウィンドウ全体で再生している場合は
                    // 強制的に小窓モードに切り替えてページを表示する
                    //if (!PlayerViewManager.IsPlayerSmallWindowModeEnabled
                    //   && PlayerViewManager.IsPlayingWithPrimaryView)
                    //{
                    //    PlayerViewManager.IsPlayerSmallWindowModeEnabled = true;
                    //}

                    var prefix = behavior == NavigationStackBehavior.Root ? "/" : String.Empty;
                    var result = await _contentFrameNavigationService.NavigateAsync($"{prefix}{pageType.ToString()}", parameter);
                    if (result.Success)
                    {
                        if (behavior == NavigationStackBehavior.NotRemember /*|| IsIgnoreRecordPageType(oldPageType)*/)
                        {
                            // TODO: NavigationStackBehavior.NotRemember
                        }

                        Window.Current.Activate();

                        GoBackCommand.RaiseCanExecuteChanged();
                    }

                    Debug.WriteLineIf(!result.Success, result.Exception?.ToString());
                }

                if (_viewModel.PrimaryViewPlayerManager.DisplayMode == Services.Player.PrimaryPlayerDisplayMode.Fill)
                {
                    _viewModel.PrimaryViewPlayerManager.ShowWithWindowInWindow();
                }
            });
        }

        

        INavigationService _contentFrameNavigationService;

        public INavigationService CreateNavigationService()
        {
            return _contentFrameNavigationService ?? (_contentFrameNavigationService = NavigationService.Create(ContentFrame, new Prism.Services.Gesture[] { Prism.Services.Gesture.Refresh }));
        }

        INavigationService _playerNavigationService;
        public INavigationService CreatePlayerNavigationService()
        {
            return _playerNavigationService ?? (_playerNavigationService = NavigationService.Create(PlayerFrame, new Prism.Services.Gesture[] { }));
        }



        #region Back Navigation

        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.KeyModifiers == Windows.System.VirtualKeyModifiers.None
                && args.CurrentPoint.Properties.IsXButton1Pressed
                )
            {
                if (HandleBackRequest())
                {
                    args.Handled = true;
                    Debug.WriteLine("back navigated with VirtualKey.Back pressed");
                }
            }
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.GoBack)
            {
                if (HandleBackRequest())
                {
                    args.Handled = true;
                    Debug.WriteLine("back navigated with VirtualKey.Back pressed");
                }
            }
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (HandleBackRequest())
            {
                e.Handled = true;
                Debug.WriteLine("back navigated with SystemNavigationManager.BackRequested");
            }
        }

        bool HandleBackRequest()
        {
            var displayMode = _viewModel.PrimaryViewPlayerManager.DisplayMode;
            if (displayMode == Services.Player.PrimaryPlayerDisplayMode.Fill
                || displayMode == Services.Player.PrimaryPlayerDisplayMode.FullScreen
                || displayMode == Services.Player.PrimaryPlayerDisplayMode.CompactOverlay
                )
            {
                Debug.WriteLine("BackNavigation canceled. priority player UI.");
                return false;
            }
            else
            {
                if (_contentFrameNavigationService.CanGoBack())
                {
                    _ = _contentFrameNavigationService.GoBackAsync();
                    return true;
                }
            }

            return false;
        }




        #endregion




        public string PageTitle
        {
            get { return (string)GetValue(PageTitleProperty); }
            set { SetValue(PageTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PageTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register("PageTitle", typeof(string), typeof(PrimaryWindowCoreLayout), new PropertyMetadata(string.Empty));





        DelegateCommand _togglePageOpenCommand;
        DelegateCommand TogglePageOpenCommand => _togglePageOpenCommand
            ?? (_togglePageOpenCommand = new DelegateCommand(ToggelPaneOpen));

        void ToggelPaneOpen()
        {
            ContentSplitView.IsPaneOpen = !ContentSplitView.IsPaneOpen;
        }


        DelegateCommand _AddPinCurrentPageCommand;
        DelegateCommand AddPinCurrentPageCommand => _AddPinCurrentPageCommand
            ?? (_AddPinCurrentPageCommand = new DelegateCommand(TryAddPinWithCurrentFrameContent));

        void TryAddPinWithCurrentFrameContent()
        {
            if ((ContentFrame.Content as FrameworkElement)?.DataContext is IPinablePage page)
            {
                var pin = page.GetPin();
                _viewModel.AddPin(pin);
            }
        }



        private DelegateCommand _GoBackCommand;
        public DelegateCommand GoBackCommand =>
            _GoBackCommand ?? (_GoBackCommand = new DelegateCommand(ExecuteGoBackCommand, CanExecuteGoBackCommand));

        private bool CanExecuteGoBackCommand()
        {
            return _contentFrameNavigationService.CanGoBack();
        }

        async void ExecuteGoBackCommand()
        {
            if (_contentFrameNavigationService.CanGoBack())
            {
                using (await _navigationLock.LockAsync())
                {
                    var result = await _contentFrameNavigationService.GoBackAsync();
                }
            }
        }


        private DelegateCommand _toggleFullScreenCommand;
        public DelegateCommand ToggleFullScreenCommand =>
            _toggleFullScreenCommand ?? (_toggleFullScreenCommand = new DelegateCommand(ExecuteToggleFullScreenCommand));

        void ExecuteToggleFullScreenCommand()
        {
            var appView = ApplicationView.GetForCurrentView();

            if (!appView.IsFullScreenMode)
            {
                appView.TryEnterFullScreenMode();
            }
            else
            {
                appView.ExitFullScreenMode();
            }
        }





        public string SearchInputText
        {
            get { return (string)GetValue(SearchInputTextProperty); }
            set { SetValue(SearchInputTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SearchInputText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SearchInputTextProperty =
            DependencyProperty.Register("SearchInputText", typeof(string), typeof(PrimaryWindowCoreLayout), new PropertyMetadata(string.Empty));





        public bool NowMobileSearchTextInput
        {
            get { return (bool)GetValue(NowMobileSearchTextInputProperty); }
            set { SetValue(NowMobileSearchTextInputProperty, value); }
        }

        // Using a DependencyProperty as the backing store for NowMobileSearchTextInput.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NowMobileSearchTextInputProperty =
            DependencyProperty.Register("NowMobileSearchTextInput", typeof(bool), typeof(PrimaryWindowCoreLayout), new PropertyMetadata(false));



    }



}

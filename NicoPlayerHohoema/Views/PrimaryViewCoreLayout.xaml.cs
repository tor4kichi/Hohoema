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
                if (e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.Back
                    || e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.Forward)
                {
                    var pageNameRaw = e.SourcePageType.FullName.Split('.').LastOrDefault();
                    var pageName = pageNameRaw.Split('_').FirstOrDefault();
                    if (Enum.TryParse(pageName.Substring(0, pageName.Length - 4), out HohoemaPageType pageType))
                    {
                        PageTitle = pageType.Translate();
                    }
                }
            };

            _viewModel.EventAggregator.GetEvent<PageNavigationEvent>()
                .Subscribe(args =>
                {
                    _ = Navigation(args);
                });

            SystemNavigationManager.GetForCurrentView().BackRequested += (_, e) =>
            {
                // ウィンドウ全体で再生している場合 → バックキーで小窓表示へ移行
                // それ以外の場合 → ページのバック処理
                //if (PlayerViewManager.IsPlayingWithPrimaryView
                //    && !PlayerViewManager.IsPlayerSmallWindowModeEnabled)
                //{
                //    //PlayerViewManager.IsPlayerSmallWindowModeEnabled = true;
                //    e.Handled = true;
                //}
                //else if (NavigationService.CanGoBack())
                //{
                //    _ = NavigationService.GoBackAsync();
                //    e.Handled = true;
                //}
            };


            PlayerFrame.Navigated += PlayerFrame_Navigated;
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

        async Task Navigation(PageNavigationEventArgs args)
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
                    var result = await _navigationService.NavigateAsync($"{prefix}{pageType.ToString()}", parameter);
                    if (result.Success)
                    {
                        PageTitle = pageType.Translate();

                        if (behavior == NavigationStackBehavior.NotRemember /*|| IsIgnoreRecordPageType(oldPageType)*/)
                        {
                            // TODO: NavigationStackBehavior.NotRemember
                        }

                        Window.Current.Activate();

                        GoBackCommand.RaiseCanExecuteChanged();
                    }

                    Debug.WriteLineIf(!result.Success, result.Exception?.ToString());
                }
            });
        }

        

        INavigationService _navigationService;

        public INavigationService CreateNavigationService()
        {
            return _navigationService ?? (_navigationService = NavigationService.Create(ContentFrame, new Prism.Services.Gesture[] { }));
        }


        INavigationService _playerNavigationService;
        public INavigationService CreatePlayerNavigationService()
        {
            return _playerNavigationService ?? (_playerNavigationService = NavigationService.Create(PlayerFrame, new Prism.Services.Gesture[] { }));
        }


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
            if (ContentFrame.Content is IPinablePage page)
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
            return _navigationService.CanGoBack();
        }

        async void ExecuteGoBackCommand()
        {
            if (_navigationService.CanGoBack())
            {
                using (await _navigationLock.LockAsync())
                {
                    var result = await _navigationService.GoBackAsync();
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
    }
}

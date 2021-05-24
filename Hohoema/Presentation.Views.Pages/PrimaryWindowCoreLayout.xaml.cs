using I18NPortable;
using Hohoema.Models.Helpers;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Presentation.ViewModels;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Hohoema.Models.UseCase.Player;
using Hohoema.Models.Domain.Application;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System.Windows.Input;
using Hohoema.Models.Domain.Notification;
using Windows.UI;
using Windows.UI.Xaml.Media;
using System.Threading;
using Microsoft.Toolkit.Mvvm.Messaging;
using Windows.System;
using Microsoft.Toolkit.Uwp;
using Hohoema.Presentation.Views.Pages.Niconico;
using Hohoema.Presentation.Views.Pages.Niconico.LoginUser;
using Hohoema.Presentation.Services.UINavigation;
using Hohoema.Models.Infrastructure;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Presentation.Views.Pages
{
    public sealed partial class PrimaryWindowCoreLayout : UserControl
    {
        private readonly PrimaryWindowCoreLayoutViewModel _viewModel;

        private readonly DispatcherQueue _dispatcherQueue;
        public PrimaryWindowCoreLayout(PrimaryWindowCoreLayoutViewModel viewModel)
        {
            DataContext = _viewModel = viewModel;
            this.InitializeComponent();

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            ContentFrame.NavigationFailed += (_, e) =>
            {
                Debug.WriteLine("Page navigation failed!!");
                Debug.WriteLine(e.SourcePageType.AssemblyQualifiedName);
                Debug.WriteLine(e.Exception.ToString());

                Crashes.TrackError(e.Exception);
            };
            
            // Resolve Page Title 
            ContentFrame.Navigated += (_, e) =>
            {
                _navigationDisposable?.Dispose();
                _navigationDisposable = new CompositeDisposable();

                PageTitle = string.Empty;
                Action<Page, NavigationEventArgs> UpdateOptionalTitleAction = (page, args) =>
                {
                    if (page.DataContext is ITitleUpdatablePage pageVM)
                    {
                        pageVM.GetTitleObservable()
                        .Subscribe(title =>
                        {
                            PageTitle = title;
                            if (pageVM is HohoemaPageViewModelBase vm)
                            {
                                vm.Title = title;
                            }
                        })
                        .AddTo(_navigationDisposable);
                    }
                    else if (page.DataContext is HohoemaPageViewModelBase vm)
                    {
                        var pageNameRaw = e.SourcePageType.FullName.Split('.').LastOrDefault();
                        var pageName = pageNameRaw.Split('_').FirstOrDefault();
                        if (Enum.TryParse(pageName.Substring(0, pageName.Length - 4), out HohoemaPageType pageType))
                        {
                            PageTitle = vm.Title = pageType.Translate();
                        }
                        else
                        {
                            PageTitle = vm.Title = pageType.ToString();
                        }
                    }                    
                };

                var page = e.Content as Page;
                if (page.DataContext == null)
                {
                    page.ObserveDependencyProperty(DataContextProperty)
                        .Subscribe(_ =>
                        {
                            UpdateOptionalTitleAction(page, e);
                        })
                        .AddTo(_navigationDisposable);
                }
                else
                {
                    UpdateOptionalTitleAction(page, e);
                }
            };

            ContentFrame.Navigating += ContentFrame_Navigating;

            ContentFrame.Navigated += ContentFrame_Navigated;

            StrongReferenceMessenger.Default.Register<PageNavigationEvent>(this, (r, m) => 
            {
                ContentFrameNavigation(m.Value);
            });

            // Back Navigation Handling            
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;


            if (DeviceTypeHelper.IsDesktop)
            {
                Window.Current.SetTitleBar(DraggableContent as UIElement);
            }

            ContentFrame.Navigated += TVModeContentFrame_Navigated;
            UINavigationManager.Pressed += UINavigationManager_Pressed;
            this.GettingFocus += PrimaryWindowCoreLayout_GettingFocus;


            _viewModel.AppearanceSettings.ObserveProperty(x => x.ApplicationTheme)
                .Subscribe(theme => 
                {
                    if (theme == ElementTheme.Default)
                    {
                        var appTheme = Helpers.SystemThemeHelper.GetSystemTheme();
                        if (appTheme == ApplicationTheme.Dark)
                        {
                            theme = ElementTheme.Dark;
                        }
                        else
                        {
                            theme = ElementTheme.Light;
                        }
                    }

                    this.RequestedTheme = theme;
                });

            CoreNavigationView.ObserveDependencyProperty(Microsoft.UI.Xaml.Controls.NavigationView.IsPaneOpenProperty)
                .Subscribe(_ => 
                {
                    if (CoreNavigationView.IsPaneOpen)
                    {
                        var pinsNVItem = CoreNavigationView.ContainerFromMenuItem(viewModel._pinsMenuSubItemViewModel);
                        if (pinsNVItem is Microsoft.UI.Xaml.Controls.NavigationViewItem nvi)
                        {
                            nvi.IsExpanded = true;
                        }
                    }

                });

            var currentContext = SynchronizationContext.Current;
            StrongReferenceMessenger.Default.Register<LiteNotificationMessage>(this, (r, m) => 
            {
                var payload = m.Value;
                if (currentContext != SynchronizationContext.Current)
                {
                    return;
                }

                TimeSpan duration = payload.Duration ?? payload.DisplayDuration switch
                {
                    DisplayDuration.Default => TimeSpan.FromSeconds(1.25),
                    DisplayDuration.MoreAttention => TimeSpan.FromSeconds(1.25 * 3),
                    _ => TimeSpan.FromSeconds(1.25),
                };

                LiteInAppNotification.Show(payload, duration);
            });

        }


        #region Debug

        ImmutableArray<ApplicationInteractionMode?> IntaractionModeList { get; } = new List<ApplicationInteractionMode?>()
        {
            default,
            ApplicationInteractionMode.Controller,
            ApplicationInteractionMode.Mouse,
            ApplicationInteractionMode.Touch,
        }.ToImmutableArray();

        bool IsDebug =>
#if DEBUG
            true;
#else
			false;
#endif

        #endregion

        #region TV Mode


        private void UINavigationManager_Pressed(UINavigationManager sender, UINavigationButtons buttons)
        {
            if (buttons.HasFlag(UINavigationButtons.View))
            {
                if (_viewModel.PrimaryViewPlayerManager.DisplayMode == PrimaryPlayerDisplayMode.Fill)
                {
                    _viewModel.PrimaryViewPlayerManager.ShowWithWindowInWindow();
                }
                else if (_viewModel.PrimaryViewPlayerManager.DisplayMode == PrimaryPlayerDisplayMode.WindowInWindow)
                {
                    _viewModel.PrimaryViewPlayerManager.ShowWithFill();
                }
            }
        }

        private void PrimaryWindowCoreLayout_GettingFocus(UIElement sender, GettingFocusEventArgs e)
        {
//            var isFocusOnMenu = e.NewFocusedElement?.FindAscendantByName(Core.Name) != null;
//            _isFocusMenu.Value = isFocusOnMenu;
//            Debug.WriteLine("Focus on Menu : " + _isFocusMenu.Value);
        }

        ReactiveProperty<bool> _isFocusMenu = new ReactiveProperty<bool>();

        private void TVModeContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ContentFrame.Focus(FocusState.Programmatic);
        }

        #endregion

        CompositeDisposable _navigationDisposable;

        private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            if (_viewModel.ApplicationLayoutManager.AppLayout == ApplicationLayout.TV)
            {
                _isFocusMenu.Value = false;
            }

            // 選択状態を解除
            _viewModel.VideoItemsSelectionContext.EndSelectioin();
        }

        NavigationTransitionInfo _contentFrameDefaultTransitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };
        NavigationTransitionInfo _contentFrameTransitionInfo = new DrillInNavigationTransitionInfo() {};
        void ContentFrameNavigation(PageNavigationEventArgs args)
        {
            var pageType = args.PageName;
            var parameter = args.Paramter;
            var behavior = args.Behavior;

            _dispatcherQueue.TryEnqueue(async () =>
            {
                // メインウィンドウでウィンドウ全体で再生している場合は
                // 強制的に小窓モードに切り替えてページを表示する
                //if (!PlayerViewManager.IsPlayerSmallWindowModeEnabled
                //   && PlayerViewManager.IsPlayingWithPrimaryView)
                //{
                //    PlayerViewManager.IsPlayerSmallWindowModeEnabled = true;
                //}

                var prefix = behavior == NavigationStackBehavior.Root ? "/" : String.Empty;
                var pageName = $"{prefix}{pageType}";

                try
                {
                    var result = behavior is NavigationStackBehavior.Push
                        ? await _contentFrameNavigationService.NavigateAsync(pageName, parameter, infoOverride: _contentFrameDefaultTransitionInfo)
                        : await _contentFrameNavigationService.NavigateAsync(pageName, parameter, infoOverride: _contentFrameTransitionInfo)
                        ;
                    if (result.Success)
                    {
                        if (behavior == NavigationStackBehavior.NotRemember /*|| IsIgnoreRecordPageType(oldPageType)*/)
                        {
                            // TODO: NavigationStackBehavior.NotRemember
                        }

                        Window.Current.Activate();

                        GoBackCommand.RaiseCanExecuteChanged();
                    }
                    else
                    {
                        throw result.Exception ?? new HohoemaExpception("navigation error");
                    }


                    Analytics.TrackEvent("PageNavigation", new Dictionary<string, string>
                    {
                        { "PageType",  pageName },
                    });

                    Debug.WriteLineIf(!result.Success, result.Exception?.ToString());


                    if (_viewModel.PrimaryViewPlayerManager.DisplayMode == PrimaryPlayerDisplayMode.Fill)
                    {
                        _viewModel.PrimaryViewPlayerManager.ShowWithWindowInWindow();
                    }

                    CoreNavigationView.IsBackEnabled = _contentFrameNavigationService.CanGoBack();
                }
                catch (Exception e)
                {
                    var errorPageParam = parameter.Select(x => (x.Key, x.Value.ToString())).ToDictionary(x => x.Key, x => x.Item2);
                    errorPageParam.Add("PageType", pageName);
                    Crashes.TrackError(e, errorPageParam);
                }
            });
        }

        

        IPlatformNavigationService _contentFrameNavigationService;

        public INavigationService CreateNavigationService()
        {
            return _contentFrameNavigationService ??= NavigationService.Create(ContentFrame, new Gestures[] { Gestures.Refresh });
        }

        IPlatformNavigationService _playerNavigationService;
        public INavigationService CreatePlayerNavigationService()
        {
            return _playerNavigationService ??= NavigationService.Create(PlayerFrame, new Gestures[] { });
        }



        #region Back Navigation

        public static bool IsPreventSystemBackNavigation { get; set; }

        private Type[] PreventGoBackPageTypes = new Type[]
        {
            typeof(RankingCategoryListPage),
            typeof(FollowManagePage),
        };

        private Type[] ForgetOwnNavigationPageTypes = new Type[]
        {
            typeof(Views.Player.LivePlayerPage),
            typeof(Views.Player.VideoPlayerPage),
        };

        static readonly Type FallbackPageType = typeof(RankingCategoryListPage);


        private List<INavigationParameters> BackParametersStack = new List<INavigationParameters>();
        private List<INavigationParameters> ForwardParametersStack = new List<INavigationParameters>();

        static INavigationParameters MakeNavigationParameter(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var np = new NavigationParameters();
            if (parameters == null) { return np; }
            foreach (var item in parameters)
            {
                np.Add(item.Key, item.Value);
            }

            return np;
        }

        public static void SetCurrentNavigationParameters(INavigationParameters parameters)
        {
            if (parameters.GetNavigationMode() == Prism.Navigation.NavigationMode.Refresh) { return; }

            CurrentNavigationParameters = parameters;
        }

        public static INavigationParameters CurrentNavigationParameters { get; private set; }

        public static NavigationParameters GetCurrentNavigationParameter()
        {
            return CurrentNavigationParameters?.Clone() ?? new NavigationParameters();
        }

        private INavigationParameters _Prev;
        bool _isFirstNavigation = true;
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.Refresh) { return; }

            var frame = (Frame)sender;

            GoBackCommand.RaiseCanExecuteChanged();

            /*
            MyNavigtionView.IsPaneVisible = !MenuPaneHiddenPageTypes.Any(x => x == e.SourcePageType);
            if (MyNavigtionView.IsPaneVisible)
            {
                var sourcePageTypeName = e.SourcePageType.Name;
                if (e.SourcePageType == typeof(FolderListupPage))
                {
                    sourcePageTypeName = nameof(Views.SourceStorageItemsPage);
                }
                var selectedMeuItemVM = ((List<object>)MyNavigtionView.MenuItemsSource).FirstOrDefault(x => (x as MenuItemViewModel)?.PageType == sourcePageTypeName);
                if (selectedMeuItemVM != null)
                {
                    MyNavigtionView.SelectedItem = selectedMeuItemVM;
                }
            }
            */


            // 戻れない設定のページではバックナビゲーションボタンを非表示に切り替え

            var isCanGoBackPage = !PreventGoBackPageTypes.Contains(e.SourcePageType);
            /*SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                isCanGoBackPage
                ? AppViewBackButtonVisibility.Visible
                : AppViewBackButtonVisibility.Collapsed
                ;
            */
            // 戻れない設定のページに到達したら Frame.BackStack から不要なPageEntryを削除する
            if (!isCanGoBackPage)
            {
                ContentFrame.BackStack.Clear();
                BackParametersStack.Clear();

                if (e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.New)
                {
                    ContentFrame.ForwardStack.Clear();
                    ForwardParametersStack.Clear();
                }

                _ = StoreNaviagtionParameterDelayed();
            }
            else if (!_isFirstNavigation)
            {
                // ここのFrame_Navigatedが呼ばれた後にViewModel側のNavigatingToが呼ばれる
                // 順序重要
                _Prev = PrimaryWindowCoreLayout.CurrentNavigationParameters;


                // ビューワー系ページはバックスタックに積まれないようにする
                // ビューワー系ページを開いてる状態でアプリ外部からビューワー系ページを開く操作があり得る
                bool rememberBackStack = true;
                if (ForgetOwnNavigationPageTypes.Any(type => type == e.SourcePageType))
                {
                    var lastNavigatedPageEntry = frame.BackStack.LastOrDefault();
                    if (ForgetOwnNavigationPageTypes.Any(type => type == lastNavigatedPageEntry?.SourcePageType)
                        && e.SourcePageType == lastNavigatedPageEntry?.SourcePageType
                        )
                    {
                        frame.BackStack.RemoveAt(frame.BackStackDepth - 1);
                        rememberBackStack = false;
                    }
                }

                if (e.NavigationMode != Windows.UI.Xaml.Navigation.NavigationMode.New)
                {
                    rememberBackStack = false;
                }

                if (rememberBackStack)
                {
                    ForwardParametersStack.Clear();
                    var parameters = new NavigationParameters();
                    if (_Prev != null)
                    {
                        foreach (var pair in _Prev)
                        {
//                            if (pair.Key == PageNavigationConstants.Restored) { continue; }

                            parameters.Add(pair.Key, pair.Value);
                        }
                    }
                    BackParametersStack.Add(parameters);
                }

                _ = StoreNaviagtionParameterDelayed();
            }

            _isFirstNavigation = false;
        }




        bool HandleBackRequest()
        {
            var currentPageType = ContentFrame.Content?.GetType();
            if (PreventGoBackPageTypes.Contains(ContentFrame.Content.GetType()))
            {
                Debug.WriteLine($"{currentPageType.Name} からの戻る操作をブロック");
                return false;
            }

            var displayMode = _viewModel.PrimaryViewPlayerManager.DisplayMode;
            if (displayMode == PrimaryPlayerDisplayMode.Fill
                || displayMode == PrimaryPlayerDisplayMode.FullScreen
                || displayMode == PrimaryPlayerDisplayMode.CompactOverlay
                )
            {
                Debug.WriteLine("BackNavigation canceled. priority player UI.");
                return false;
            }
            else
            {
                if (_contentFrameNavigationService.CanGoBack())
                {
                    var backNavigationParameters = BackParametersStack.ElementAtOrDefault(BackParametersStack.Count - 1);
                    {
                        var last = BackParametersStack.Last();
                        var parameters = GetCurrentNavigationParameter();    // GoBackAsyncを呼ぶとCurrentNavigationParametersが入れ替わる。呼び出し順に注意。
                        BackParametersStack.Remove(last);
                        ForwardParametersStack.Add(parameters);
                    }
                    _ = backNavigationParameters == null
                        ? _contentFrameNavigationService.GoBackAsync()
                        : _contentFrameNavigationService.GoBackAsync(backNavigationParameters)
                        ;
                    return true;
                }
            }

            return false;
        }




        bool HandleForwardRequest()
        {
            if (_contentFrameNavigationService.CanGoForward())
            {
                var forwardNavigationParameters = ForwardParametersStack.Last();
                {
                    var last = ForwardParametersStack.Last();
                    var parameters = GetCurrentNavigationParameter(); // GoForwardAsyncを呼ぶとCurrentNavigationParametersが入れ替わる。呼び出し順に注意。
                    ForwardParametersStack.Remove(last);
                    BackParametersStack.Add(parameters);
                }
                _ = forwardNavigationParameters == null
                   ? _contentFrameNavigationService.GoForwardAsync()
                   : _contentFrameNavigationService.GoForwardAsync(forwardNavigationParameters)
                   ;

                return true;
            }

            return false;
        }


        private void CoreWindow_PointerPressed(CoreWindow sender, PointerEventArgs args)
        {
            if (args.KeyModifiers == Windows.System.VirtualKeyModifiers.None
                && args.CurrentPoint.Properties.IsXButton1Pressed
                )
            {
                if (HandleBackRequest())
                {
                    args.Handled = true;
                    Debug.WriteLine("back navigated with Pointer Back pressed");
                }
            }
            else if (args.KeyModifiers == Windows.System.VirtualKeyModifiers.None
                && args.CurrentPoint.Properties.IsXButton2Pressed
                )
            {
                if (HandleForwardRequest())
                {
                    args.Handled = true;
                    Debug.WriteLine("forward navigated with Pointer Forward pressed");
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
            else if (args.VirtualKey == Windows.System.VirtualKey.GoForward)
            {
                if (HandleForwardRequest())
                {
                    args.Handled = true;
                    Debug.WriteLine("forward navigated with VirtualKey.Back pressed");
                }
            }
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            if (IsPreventSystemBackNavigation) { return; }

            if (HandleBackRequest())
            {
                Debug.WriteLine("back navigated with SystemNavigationManager.BackRequested");
            }

            // Note: 強制的にハンドルしないとXboxOneやタブレットでアプリを閉じる動作に繋がってしまう
            e.Handled = true;
        }





        async Task StoreNaviagtionParameterDelayed()
        {
            await Task.Delay(50);

            // ナビゲーション状態の保存
            
            Debug.WriteLine("[NavvigationRestore] Save CurrentPage: " + ContentFrame.CurrentSourcePageType.Name);
            _viewModel.RestoreNavigationManager.SetCurrentNavigationEntry(MakePageEnetry(ContentFrame.CurrentSourcePageType, CurrentNavigationParameters));
            {
                PageEntry[] backNavigationPageEntries = new PageEntry[BackParametersStack.Count];
                for (var backStackIndex = 0; backStackIndex < BackParametersStack.Count; backStackIndex++)
                {
                    var parameters = BackParametersStack[backStackIndex];
                    var stackEntry = ContentFrame.BackStack[backStackIndex];
                    backNavigationPageEntries[backStackIndex] = MakePageEnetry(stackEntry.SourcePageType, parameters);
                    Debug.WriteLine("[NavvigationRestore] Save BackStackPage: " + backNavigationPageEntries[backStackIndex].PageName);
                }
                await _viewModel.RestoreNavigationManager.SetBackNavigationEntriesAsync(backNavigationPageEntries);
            }
            
            /*
            {
                PageEntry[] forwardNavigationPageEntries = new PageEntry[ForwardParametersStack.Count];
                for (var forwardStackIndex = 0; forwardStackIndex < ForwardParametersStack.Count; forwardStackIndex++)
                {
                    var parameters = ForwardParametersStack[forwardStackIndex];
                    var stackEntry = ContentFrame.ForwardStack[forwardStackIndex];
                    forwardNavigationPageEntries[forwardStackIndex] = MakePageEnetry(stackEntry.SourcePageType, parameters);
                    Debug.WriteLine("[NavvigationRestore] Save ForwardStackPage: " + forwardNavigationPageEntries[forwardStackIndex].PageName);
                }
                await _viewModel.RestoreNavigationManager.SetForwardNavigationEntriesAsync(forwardNavigationPageEntries);
            }
            */
        }

        static PageEntry MakePageEnetry(Type pageType, INavigationParameters parameters)
        {
            return new PageEntry(pageType.Name, parameters);
        }

        public async Task RestoreNavigationStack()
        {
            var navigationManager = _viewModel.RestoreNavigationManager;
            try
            {
                var currentEntry = navigationManager.GetCurrentNavigationEntry();
                if (currentEntry == null)
                {
                    Debug.WriteLine("[NavvigationRestore] skip restore page.");
                    await _contentFrameNavigationService.NavigateAsync(FallbackPageType.Name);
                    return;
                }

                var parameters = MakeNavigationParameter(currentEntry.Parameters);
                /*
                if (!parameters.ContainsKey(PageNavigationConstants.Restored))
                {
                    parameters.Add(PageNavigationConstants.Restored, string.Empty);
                }
                */
                var result = await _contentFrameNavigationService.NavigateAsync(currentEntry.PageName, parameters, new SuppressNavigationTransitionInfo());
                if (!result.Success)
                {
                    await Task.Delay(50);
                    Debug.WriteLine("[NavvigationRestore] Failed restore CurrentPage: " + currentEntry.PageName);
                    await _contentFrameNavigationService.NavigateAsync(FallbackPageType.Name);
                    return;
                }

                Debug.WriteLine("[NavvigationRestore] Restored CurrentPage: " + currentEntry.PageName);

                if (currentEntry.PageName == FallbackPageType.Name)
                {
                    return;
                }
            }
            catch
            {
                Debug.WriteLine("[NavvigationRestore] failed restore current page. ");

                BackParametersStack.Clear();
                ForwardParametersStack.Clear();
                ContentFrame.BackStack.Clear();
                ContentFrame.ForwardStack.Clear();

                await StoreNaviagtionParameterDelayed();
                await _contentFrameNavigationService.NavigateAsync(FallbackPageType.Name);
                return;
            }

            try
            {
                var backStack = await navigationManager.GetBackNavigationEntriesAsync();
                foreach (var backNavItem in backStack)
                {
                    var pageType = Type.GetType($"Hohoema.Presentation.Views.{backNavItem.PageName}");
                    var parameters = MakeNavigationParameter(backNavItem.Parameters);
                    
                    ContentFrame.BackStack.Add(new PageStackEntry(pageType, parameters, new SuppressNavigationTransitionInfo()));
                    BackParametersStack.Add(parameters);
                    Debug.WriteLine("[NavvigationRestore] Restored BackStackPage: " + backNavItem.PageName);
                }
            }
            catch 
            {
                _ = navigationManager.SetBackNavigationEntriesAsync(Enumerable.Empty<PageEntry>());
                ContentFrame.BackStack.Clear();
                BackParametersStack.Clear();
                Debug.WriteLine("[NavvigationRestore] failed restore BackStack");
            }
            /*
            {
                var forwardStack = await navigationManager.GetForwardNavigationEntriesAsync();
                foreach (var forwardNavItem in forwardStack)
                {
                    var pageType = Type.GetType($"Hohoema.Presentation.Views.{forwardNavItem.PageName}");
                    var parameters = MakeNavigationParameter(forwardNavItem.Parameters);
                    ContentFrame.ForwardStack.Add(new PageStackEntry(pageType, parameters, new SuppressNavigationTransitionInfo()));
                    ForwardParametersStack.Add(parameters);
                    Debug.WriteLine("[NavvigationRestore] Restored BackStackPage: " + forwardNavItem.PageName);
                }
            }
            */
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




        public string OptionalPageTitle
        {
            get { return (string)GetValue(OptionalPageTitleProperty); }
            set { SetValue(OptionalPageTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OptionalPageTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OptionalPageTitleProperty =
            DependencyProperty.Register("OptionalPageTitle", typeof(string), typeof(PrimaryWindowCoreLayout), new PropertyMetadata(string.Empty));




        DelegateCommand _togglePageOpenCommand;
        DelegateCommand TogglePageOpenCommand => _togglePageOpenCommand
            ?? (_togglePageOpenCommand = new DelegateCommand(ToggelPaneOpen));

        void ToggelPaneOpen()
        {
            CoreNavigationView.IsPaneOpen = !CoreNavigationView.IsPaneOpen;
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
            return ContentFrame.BackStack.Any();
        }

        void ExecuteGoBackCommand()
        {
            _dispatcherQueue.TryEnqueue(() => HandleBackRequest());
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



        private void SearchTextBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            sender.IsSuggestionListOpen = !string.IsNullOrWhiteSpace(sender.Text);
        }

        private void SearchTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is SearchAutoSuggestItemViewModel tag)
            {
                tag.SearchAction(args.QueryText);
            }
            else
            {
                (_viewModel.SearchCommand as ICommand).Execute(args.QueryText);
            }
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var asb = (sender as AutoSuggestBox);
            asb.IsSuggestionListOpen = !string.IsNullOrWhiteSpace(asb.Text);
        }

        private void CoreNavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer?.DataContext is IPageNavigatable menuItemVM)
            {
                _viewModel.PageManager.OpenPageCommand.Execute(menuItemVM);
            }
            else if (args.InvokedItemContainer?.DataContext is LiveContentMenuItemViewModel live)
            {
                (_viewModel.OpenLiveContentCommand as ICommand).Execute(live);
            }
            else if (args.InvokedItem is IPageNavigatable menuItem)
            {
                _viewModel.PageManager.OpenPageCommand.Execute(menuItem);
            }
            else if ((args.InvokedItem as FrameworkElement)?.DataContext is IPageNavigatable item)
            {
                _viewModel.PageManager.OpenPageCommand.Execute(item);
            }
            else if (args.IsSettingsInvoked)
            {
                _viewModel.PageManager.OpenPage(HohoemaPageType.Settings);
            }
        }

        private void CoreNavigationView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
        {
            if (_GoBackCommand.CanExecute())
            {
                _GoBackCommand.Execute();
            }
        }





        public bool IsDebugModeEnabled
        {
            get { return (bool)GetValue(IsDebugModeEnabledProperty); }
            set { SetValue(IsDebugModeEnabledProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsDebugModeEnabled.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDebugModeEnabledProperty =
            DependencyProperty.Register("IsDebugModeEnabled", typeof(bool), typeof(PrimaryWindowCoreLayout), new PropertyMetadata(false));





        public void OpenErrorTeachingTip(ICommand sentErrorCommand, Action onClosing)
        {
            AppErrorTeachingTip.ActionButtonCommand = sentErrorCommand;
            AppErrorTeachingTip.IsOpen = true;

            _onErrorTeachingTipClosed = onClosing;
        }

        Action _onErrorTeachingTipClosed;


        public void CloseErrorTeachingTip()
        {
            AppErrorTeachingTip.IsOpen = false;
        }

        private void AppErrorTeachingTip_Closed(Microsoft.UI.Xaml.Controls.TeachingTip sender, Microsoft.UI.Xaml.Controls.TeachingTipClosedEventArgs args)
        {
            _onErrorTeachingTipClosed?.Invoke();
            _onErrorTeachingTipClosed = null;
            AppErrorTeachingTip.ActionButtonCommand = null;
        }

        private void AddShortLiteInAppNotification_Click(object sender, RoutedEventArgs e)
        {
            StrongReferenceMessenger.Default.Send(new LiteNotificationMessage(new () 
            {
                Content = "あああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああ",
                Symbol = Symbol.Accept,
                DisplayDuration = DisplayDuration.Default,
            }));
        }

        private void AddLongLiteInAppNotification_Click(object sender, RoutedEventArgs e)
        {
            StrongReferenceMessenger.Default.Send(new LiteNotificationMessage(new()
            {
                Content = "もっと表示",
                DisplayDuration = DisplayDuration.MoreAttention,
            }));
        }
    }

    public static class NavigationParametersExtensions
    {
        public static NavigationParameters Clone(this INavigationParameters parameters)
        {
            var clone = new NavigationParameters();
            foreach (var pair in parameters)
            {
                clone.Add(pair.Key, pair.Value);
            }

            return clone;
        }
    }

}

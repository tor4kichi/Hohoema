#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services.Player;
using Hohoema.Helpers;
using Hohoema.Infra;
using Hohoema.Models.Application;
using Hohoema.Models.Notification;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.ViewModels;
using Hohoema.ViewModels.PrimaryWindowCoreLayout;
using Hohoema.Views.Pages.Niconico.Follow;
using Hohoema.Views.Pages.Niconico.VideoRanking;
using I18NPortable;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using ZLogger;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Pages;

public sealed partial class PrimaryWindowCoreLayout : UserControl
{
    private readonly PrimaryWindowCoreLayoutViewModel _vm;
    private readonly IScheduler _scheduler;
    private readonly Services.CurrentActiveWindowUIContextService _currentActiveWindowUIContextService;
    private readonly ILogger<PrimaryWindowCoreLayout> _logger;
    private readonly DispatcherQueue _dispatcherQueue;


    private static readonly int MaxBackStackCount = 5;

    public PrimaryWindowCoreLayout(
        IScheduler scheduler,
        PrimaryWindowCoreLayoutViewModel viewModel,
        Services.CurrentActiveWindowUIContextService currentActiveWindowUIContextService,
        ILoggerFactory loggerFactory
        )
    {
        DataContext = _vm = viewModel;

        this.InitializeComponent();
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _scheduler = scheduler;
        _currentActiveWindowUIContextService = currentActiveWindowUIContextService;
        _logger = loggerFactory.CreateLogger<PrimaryWindowCoreLayout>();

        CoreWindow.GetForCurrentThread().KeyDown += (sender, args) =>
        {                
            if (args.VirtualKey == VirtualKey.GamepadView && sender.ActivationMode == CoreWindowActivationMode.ActivatedInForeground)
            {
                CoreNavigationView.IsPaneOpen = true;
            }
        };

        ContentFrame.NavigationFailed += (_, e) =>
        {
            _logger.ZLogError(e.Exception, "Page navigation failed!!");
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

        WeakReferenceMessenger.Default.Register<NavigationAsyncRequestMessage>(this, (r, m) => 
        {
            m.Reply(ContentFrameNavigationAsync(m.NavigationRequest));
        });

        // Back Navigation Handling            
        SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
        Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        Window.Current.CoreWindow.PointerPressed += CoreWindow_PointerPressed;


        ResetTitleBarDraggableArea();


        new[]
        {
            _vm.PrimaryViewPlayerManager.ObserveProperty(x => x.DisplayMode).ToUnit(),
            Observable.FromEventPattern(ContentFrame, "Navigated").ToUnit(),
            Observable.FromEventPattern(PlayerFrame, "Navigated").Delay(TimeSpan.FromMilliseconds(1000), _scheduler).ToUnit(),
        }
        .Merge()
        .Subscribe(_ => ResetTitleBarDraggableArea());

        ContentFrame.Navigated += TVModeContentFrame_Navigated;
        this.GettingFocus += PrimaryWindowCoreLayout_GettingFocus;


        _vm.AppearanceSettings.ObserveProperty(x => x.ApplicationTheme)
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

        CoreNavigationView.ObserveDependencyProperty(Microsoft.UI.Xaml.Controls.NavigationView.PaneDisplayModeProperty)
            .Subscribe(_ => 
            {
                _vm.ApplicationLayoutManager.SetCurrentNavigationViewPaneDisplayMode(CoreNavigationView.PaneDisplayMode);                    
            });

        CoreNavigationView.ObserveDependencyProperty(Microsoft.UI.Xaml.Controls.NavigationView.IsBackButtonVisibleProperty)
            .Subscribe(_ =>
            {
                _vm.ApplicationLayoutManager.SetCurrentNavigationViewIsBackButtonVisible(CoreNavigationView.IsBackButtonVisible);
            });

        WeakReferenceMessenger.Default.Register<LiteNotificationMessage>(this, (r, m) => 
        {
            var payload = m.Value;
            if (_currentActiveWindowUIContextService.UIContext == null)
            {
                return;
            }

            if (_currentActiveWindowUIContextService.UIContext != UIContext)
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

        Window.Current.Activated += Current_Activated;
        
        // Xbox向けのメニュー表示、下部のマージンを追加する
        if (_vm.ApplicationLayoutManager.AppLayout == ApplicationLayout.TV)
        {
            Resources["NavigationViewPaneContentGridMargin"] = new Thickness(0, 27, 0, 27);
        }
        
        Loaded += PrimaryWindowCoreLayout_Loaded;
    }

    private void PrimaryWindowCoreLayout_Loaded(object sender, RoutedEventArgs e)
    {
        Services.CurrentActiveWindowUIContextService.SetUIContext(_currentActiveWindowUIContextService, UIContext, XamlRoot);
    }

    void ResetTitleBarDraggableArea()
    {
        if (DeviceTypeHelper.IsDesktop)
        {
            try
            {
                if (_vm.PrimaryViewPlayerManager.DisplayMode is PlayerDisplayMode.FillWindow or PlayerDisplayMode.FullScreen or PlayerDisplayMode.CompactOverlay)
                {
                    if (PlayerFrame.Content is IDraggableAreaAware draggableArea)
                    {
                        if (draggableArea.GetDraggableArea() is not null and var area)
                        {
                            Window.Current.SetTitleBar(area);
                            DraggableContent.Visibility = Visibility.Collapsed;
                            return;
                        }                            
                    }
                }

                if (ContentFrame.Content is IDraggableAreaAware contentDraggableArea)
                {
                    if (contentDraggableArea.GetDraggableArea() is not null and var area)
                    {
                        Window.Current.SetTitleBar(area);
                        DraggableContent.Visibility = Visibility.Collapsed;
                        return;
                    }
                }

                DraggableContent.Visibility = Visibility.Visible;
                Window.Current.SetTitleBar(DraggableContent);
            }
            catch
            {
                DraggableContent.Visibility = Visibility.Visible;
                Window.Current.SetTitleBar(DraggableContent);
                throw;
            }
        }
    }

    private void Current_Activated(object sender, WindowActivatedEventArgs e)
    {
        Services.CurrentActiveWindowUIContextService.SetUIContext(_currentActiveWindowUIContextService, UIContext, XamlRoot);
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


    public void TogglePlayerFillBtwWindowInWindow()
    {
        if (_vm.PrimaryViewPlayerManager.DisplayMode == PlayerDisplayMode.FillWindow)
        {
            _vm.PrimaryViewPlayerManager.ShowWithWindowInWindowAsync();
        }
        else if (_vm.PrimaryViewPlayerManager.DisplayMode == PlayerDisplayMode.WindowInWindow)
        {
            _vm.PrimaryViewPlayerManager.ShowWithFillAsync();
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
        if (_vm.ApplicationLayoutManager.AppLayout == ApplicationLayout.TV)
        {
            _isFocusMenu.Value = false;
        }

        // 選択状態を解除
        _vm.VideoItemsSelectionContext.EndSelectioin();
    }

    NavigationTransitionInfo _contentFrameDefaultTransitionInfo = new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight };
    NavigationTransitionInfo _contentFrameTransitionInfo = new DrillInNavigationTransitionInfo() {};
    Task<INavigationResult> ContentFrameNavigationAsync(PageNavigationEventArgs args)
    {
        var pageType = args.PageName;
        var parameter = args.Paramter;
        var behavior = args.Behavior;

        return _dispatcherQueue.EnqueueAsync(async () =>
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
                if (result.IsSuccess)
                {
                    if (behavior == NavigationStackBehavior.NotRemember /*|| IsIgnoreRecordPageType(oldPageType)*/)
                    {
                        // TODO: NavigationStackBehavior.NotRemember
                    }

                    //await _viewModel.PrimaryViewPlayerManager.ShowAsync();
                    //Window.Current.Activate();                        
                    await ApplicationViewSwitcher.TryShowAsStandaloneAsync(ApplicationView.GetForCurrentView().Id, ViewSizePreference.Default);

                    GoBackCommand.NotifyCanExecuteChanged();
                }
                else
                {
                    throw result.Exception ?? new HohoemaException("navigation error");
                }

                Debug.WriteLineIf(!result.IsSuccess, result.Exception?.ToString());

                if (_vm.PrimaryViewPlayerManager.DisplayMode is PlayerDisplayMode.FillWindow or PlayerDisplayMode.FullScreen)
                {
                    _vm.PrimaryViewPlayerManager.ShowWithWindowInWindowAsync();
                }

                CoreNavigationView.IsBackEnabled = _contentFrameNavigationService.CanGoBack();

                return result;
            }
            catch (Exception e)
            {
                _logger.ZLogError(e, "ContentFrame Navigation failed: {0}", pageName);
                throw;
            }
        });
    }

    

    NavigationService _contentFrameNavigationService;

    public INavigationService CreateNavigationService()
    {
        return _contentFrameNavigationService ??= NavigationService.Create(ContentFrame);
    }

    NavigationService _playerNavigationService;
    public INavigationService CreatePlayerNavigationService()
    {
        return _playerNavigationService ??= NavigationService.CreateWithoutHistory(PlayerFrame);
    }



    #region Back Navigation

    public static bool IsPreventSystemBackNavigation { get; set; }

    private readonly ImmutableHashSet<Type> PreventGoBackPageTypes = new Type[]
    {
        typeof(RankingCategoryListPage),
        typeof(FollowManagePage),
    }.ToImmutableHashSet();

    private readonly ImmutableHashSet<Type> ForgetOwnNavigationPageTypes = new Type[]
    {
        typeof(Views.Player.LivePlayerPage),
        typeof(Views.Player.VideoPlayerPage),
        typeof(Views.Player.LegacyVideoPlayerPage),
    }.ToImmutableHashSet();

    


    static readonly Type FallbackPageType = typeof(RankingCategoryListPage);


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

    bool _isFirstNavigation = true;
    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.Refresh) { return; }

        var frame = (Frame)sender;

        GoBackCommand.NotifyCanExecuteChanged();

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

        // 戻れない設定のページに到達したら Frame.BackStack から不要なPageEntryを削除する
        if (!isCanGoBackPage)
        {
            ContentFrame.BackStack.Clear();

            if (e.NavigationMode == Windows.UI.Xaml.Navigation.NavigationMode.New)
            {
                ContentFrame.ForwardStack.Clear();
            }

            _ = StoreNaviagtionParameterDelayed();
        }
        else if (!_isFirstNavigation)
        {
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
                }
            }

            if (ContentFrame.BackStack.Count >= MaxBackStackCount)
            {
                ContentFrame.BackStack.RemoveAt(0);
            }

            _ = StoreNaviagtionParameterDelayed();
        }

        _isFirstNavigation = false;
    }




    bool HandleBackRequest()
    {
        var currentPageType = ContentFrame.Content?.GetType();
        if (currentPageType == null)
        {
            return false;
        }

        if (PreventGoBackPageTypes.Contains(currentPageType))
        {
            Debug.WriteLine($"{currentPageType.Name} からの戻る操作をブロック");
            return false;
        }

        var displayMode = _vm.PrimaryViewPlayerManager.DisplayMode;
        if (displayMode == PlayerDisplayMode.FillWindow
            || displayMode == PlayerDisplayMode.FullScreen
            || displayMode == PlayerDisplayMode.CompactOverlay
            )
        {
            Debug.WriteLine("BackNavigation canceled. priority player UI.");
            return false;
        }
        else
        {
            _ = _contentFrameNavigationService.GoBackAsync();
            return true;
        }
    }




    bool HandleForwardRequest()
    {
        if (_contentFrameNavigationService.CanGoForward())
        {
            _ = _contentFrameNavigationService.GoForwardAsync();

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
        var (currentPage, currentPageParameters) = _contentFrameNavigationService.GetCurrentPage();
        Debug.WriteLine($"[NavvigationRestore] Save CurrentPage: {currentPage.Name}");
        _vm.RestoreNavigationManager.SetCurrentNavigationEntry(MakePageEnetry(currentPage, currentPageParameters));

        await _vm.RestoreNavigationManager.SetBackNavigationEntriesAsync(
            _contentFrameNavigationService.GetBackStackPages().Select(x => MakePageEnetry(x.PageType, x.Parameters))
        );
        
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
        var navigationManager = _vm.RestoreNavigationManager;
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
            if (!result.IsSuccess)
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
                var pageType = Type.GetType($"Hohoema.Views.{backNavItem.PageName}");
                var parameters = MakeNavigationParameter(backNavItem.Parameters);
                
                ContentFrame.BackStack.Add(new PageStackEntry(pageType, parameters, new SuppressNavigationTransitionInfo()));
                Debug.WriteLine("[NavvigationRestore] Restored BackStackPage: " + backNavItem.PageName);
            }
        }
        catch 
        {
            _ = navigationManager.SetBackNavigationEntriesAsync(Enumerable.Empty<PageEntry>());
            ContentFrame.BackStack.Clear();
            Debug.WriteLine("[NavvigationRestore] failed restore BackStack");
        }
        /*
        {
            var forwardStack = await navigationManager.GetForwardNavigationEntriesAsync();
            foreach (var forwardNavItem in forwardStack)
            {
                var pageType = Type.GetType($"Hohoema.Views.{forwardNavItem.PageName}");
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




    RelayCommand _togglePageOpenCommand;
    RelayCommand TogglePageOpenCommand => _togglePageOpenCommand
        ?? (_togglePageOpenCommand = new RelayCommand(ToggelPaneOpen));

    void ToggelPaneOpen()
    {
        CoreNavigationView.IsPaneOpen = !CoreNavigationView.IsPaneOpen;
    }


    RelayCommand _AddPinCurrentPageCommand;
    RelayCommand AddPinCurrentPageCommand => _AddPinCurrentPageCommand
        ?? (_AddPinCurrentPageCommand = new RelayCommand(TryAddPinWithCurrentFrameContent));

    void TryAddPinWithCurrentFrameContent()
    {
        if (GetCurrentPagePin() is not null and var pin)
        {
            _vm.AddPin(pin);
        }
    }

    HohoemaPin GetCurrentPagePin()
    {
        if ((ContentFrame.Content as FrameworkElement)?.DataContext is IPinablePage page)
        {
            return page.GetPin();
        }
        else { return null; }
    }

    [RelayCommand]
    void AddBookmarkFolder()
    {
        var pinGroup = new HohoemaPin { PinType = Models.Pins.BookmarkType.Folder, Label = "PinFolder_DefaultName".Translate() };
        _vm.AddPin(pinGroup);
    }


    private RelayCommand _GoBackCommand;
    public RelayCommand GoBackCommand =>
        _GoBackCommand ?? (_GoBackCommand = new RelayCommand(ExecuteGoBackCommand, CanExecuteGoBackCommand));

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
        //sender.IsSuggestionListOpen = !string.IsNullOrWhiteSpace(sender.Text);
    }

    private void SearchTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is SearchAutoSuggestItemViewModel tag)
        {
            tag.SearchAction(args.QueryText);
        }
        else
        {
            (_vm.SearchCommand as ICommand).Execute(args.QueryText);
        }

        if (CoreNavigationView.IsPaneOpen && 
            CoreNavigationView.DisplayMode is Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Compact or Microsoft.UI.Xaml.Controls.NavigationViewDisplayMode.Minimal)
        {
            CoreNavigationView.IsPaneOpen = false;
        }
    }

    private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        var asb = (sender as AutoSuggestBox);
        //asb.IsSuggestionListOpen = !string.IsNullOrWhiteSpace(asb.Text);
    }

    private void CoreNavigationView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer?.DataContext is IPageNavigatable menuItemVM)
        {
            _vm.PageManager.OpenPageCommand.Execute(menuItemVM);                
        }
        else if (args.InvokedItemContainer?.DataContext is LiveContentMenuItemViewModel live)
        {
            (_vm.OpenLiveContentCommand as ICommand).Execute(live);
        }
        else if (args.InvokedItem is IPageNavigatable menuItem)
        {
            _vm.PageManager.OpenPageCommand.Execute(menuItem);
        }
        else if ((args.InvokedItem as FrameworkElement)?.DataContext is IPageNavigatable item)
        {
            _vm.PageManager.OpenPageCommand.Execute(item);
        }
        else if (args.IsSettingsInvoked)
        {
            _vm.PageManager.OpenPage(HohoemaPageType.Settings);
        }
    }

    private void CoreNavigationView_BackRequested(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewBackRequestedEventArgs args)
    {
        if (_GoBackCommand.CanExecute(null))
        {
            _GoBackCommand.Execute(null);
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
        WeakReferenceMessenger.Default.Send(new LiteNotificationMessage(new () 
        {
            Content = "あああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああああ",
            Symbol = Symbol.Accept,
            DisplayDuration = DisplayDuration.Default,
        }));
    }

    private void AddLongLiteInAppNotification_Click(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new LiteNotificationMessage(new()
        {
            Content = "もっと表示",
            DisplayDuration = DisplayDuration.MoreAttention,
        }));
    }

    private void PinItemMenuFlyout_Opening(object sender, object e)
    {            
        var menuFlyout = sender as MenuFlyout;
        var itemVM = menuFlyout.Target.DataContext as PinMenuItemViewModel;
        var moveToFolderSubItem = menuFlyout.Items.First(x => x.Name == "MoveToFolderItem") as MenuFlyoutSubItem;

        moveToFolderSubItem.Items.Clear();

        var folderItems = _vm.GetPinFolders();

        var parentFolderVM = _vm.GetParentPinFolder(itemVM);
        // TODO: 今いるフォルダをDisableに
        moveToFolderSubItem.Items.Add(new MenuFlyoutItem { Text = "PinMoveToRoot".Translate(), Command = itemVM.MoveToRootFolderCommand, IsEnabled = parentFolderVM != null });
        moveToFolderSubItem.Items.Add(new MenuFlyoutSeparator());
        foreach (var folder in folderItems)
        {
            moveToFolderSubItem.Items.Add(new MenuFlyoutItem { Text = folder.Pin.OverrideLabel ?? folder.Label, Command = itemVM.MoveToFolderCommand, CommandParameter = folder, IsEnabled = folder != parentFolderVM });
        }
    }

    private void PinCurrentPageMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetCurrentPagePin() is not null and var pin)
        {
            var item = sender as MenuFlyoutItem;
            if (item == null) { return; }

            var folderVM = item.DataContext as PinFolderMenuItemViewModel;
            if (folderVM == null) { return; }
            _vm.AddPinToFolder(pin, folderVM);
        }
    }

    private void CoreNavigationView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Handled) { return; }

        if (e.Key == VirtualKey.GamepadView)
        {
            CoreNavigationView.IsPaneOpen = !CoreNavigationView.IsPaneOpen;
            e.Handled = true;
        }
    }

    private void CoreNavigationView_DisplayModeChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewDisplayModeChangedEventArgs args)
    {
        // TODO: Top の場合でもMinimalになってしまうためページヘッダー向けの条件分岐としては不十分
        _vm.ApplicationLayoutManager.SetCurrentNavigationViewDisplayMode(args.DisplayMode);
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

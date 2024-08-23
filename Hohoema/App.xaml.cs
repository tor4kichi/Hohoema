#nullable enable
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Text;
using DryIoc;
using Hohoema.Contracts.AppLifecycle;
using Hohoema.Contracts.Maintenances;
using Hohoema.Contracts.Migrations;
using Hohoema.Contracts.Navigations;
using Hohoema.Contracts.Navigations;
using Hohoema.Contracts.Services.Player;
using Hohoema.Helpers;
using Hohoema.Infra;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Pins;
using Hohoema.Models.Player;
using Hohoema.Models.Player.Comment;
using Hohoema.Models.Player.Video;
using Hohoema.Models.Playlist;
using Hohoema.Models.Subscriptions;
using Hohoema.Services;
using Hohoema.Services.LocalMylist;
using Hohoema.Services.Maintenance;
using Hohoema.Services.Migrations;
using Hohoema.Services.Niconico;
using Hohoema.Services.Niconico.Account;
using Hohoema.Services.Player;
using Hohoema.Services.Player.Videos;
using Hohoema.Services.Playlist;
using Hohoema.Services.Subscriptions;
using Hohoema.Services.VideoCache;
using Hohoema.ViewModels;
using Hohoema.ViewModels.Niconico.Video;
using Hohoema.Views.Pages;
using I18NPortable;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using ZLogger;
using ValueTaskSupplement;
using Hohoema.Contracts.Subscriptions;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI;
using Windows.Services.Store;
using CommunityToolkit.Diagnostics;
using Hohoema.Models.Notification;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Views.Pages.Hohoema;

namespace Hohoema;

internal sealed class ViewLocator : IViewLocator
{
    public Type ResolveView(string viewName)
    {
        return _registory[viewName];
    }

    public void RegisterForNavigation<T>()
    {
        Type type = typeof(T);
        _registory.Add(type.Name, type);
    }

    private readonly Dictionary<string, Type> _registory = new();

    internal Type ResolveViewType(string viewName)
    {
        return _registory[viewName];
    }
}

/// <summary>
/// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
/// </summary>
public sealed partial class App : Application
{
    private const bool _DEBUG_XBOX_RESOURCE = true;

    public SplashScreen SplashScreen { get; private set; }

    private bool _IsPreLaunch;

    public const string ACTIVATION_WITH_ERROR = "error";
    public const string ACTIVATION_WITH_ERROR_OPEN_LOG = "error_open_log";
    public const string ACTIVATION_WITH_ERROR_COPY_LOG = "error_copy_log";

    internal const string IS_COMPLETE_INTRODUCTION = "is_first_launch";


    public static new App Current => (App)Application.Current;

    public Container Container { get; private set; }

    /// <summary>
    /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
    ///最初の行であるため、main() または WinMain() と論理的に等価です。
    /// </summary>
    public App()
    {
        UnhandledException += PrismUnityApplication_UnhandledException;

        // XboxOne向けの設定
        // 基本カーソル移動で必要なときだけポインターを出現させる
        RequiresPointerMode = Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;

        // テーマ設定
        // ThemeResourceの切り替えはアプリの再起動が必要
        RequestedTheme = GetTheme();

        InitializeComponent();

        Suspending += async (s, e) => 
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            try
            {
                Views.UINavigation.UINavigationManager.OnSuspeding();
                await Container.ResolveMany<ISuspendAndResumeAware>(behavior: ResolveManyBehavior.AsFixedArray).Select(x => x.OnSuspendingAsync());
            }
            catch (Exception ex) 
            {
                _loggerFactory!.CreateLogger<App>().ZLogError(ex, "error in Suspending operation.");
            }
            finally
            {
                deferral.Complete();
            }
        };

        Resuming += async (s, e) => 
        {
            try
            {
                Views.UINavigation.UINavigationManager.OnResuming();
                await Container.ResolveMany<ISuspendAndResumeAware>(behavior: ResolveManyBehavior.AsFixedArray).Select(x => x.OnSuspendingAsync());
            }
            catch (Exception ex)
            {
                _loggerFactory!.CreateLogger<App>().ZLogError(ex, "error in Resuming operation.");
            }
        };
    }

    [Obsolete]
    private Container ConfigureService()
    {
        Rules rules = Rules.Default
            .WithConcreteTypeDynamicRegistrations((serviceType, serviceKey) => true, Reuse.Singleton)
            .WithAutoConcreteTypeResolution()
            .With(Made.Of(FactoryMethod.ConstructorWithResolvableArguments))
            .WithoutThrowOnRegisteringDisposableTransient()
            .WithFuncAndLazyWithoutRegistration()
            .WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Replace)
            .WithoutThrowOnRegisteringDisposableTransient()
            ;

        Container container = new(rules);

        RegisterRequiredTypes(container);
        RegisterTypes(container);

        Ioc.Default.ConfigureServices(container);
        return container;
    }


    private ILoggerFactory _loggerFactory;

    public class DebugOutputStream : Stream
    {
        public DebugOutputStream(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        private long _length;
        private readonly IScheduler _scheduler;

        public override long Length => _length;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {

        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {

        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _length = count;
            Debug.Write(System.Text.Encoding.UTF8.GetString(buffer, offset, count));
        }
    }


    private void RegisterRequiredTypes(IContainer container)
    {
        SynchronizationContextScheduler mainWindowsScheduler = new(SynchronizationContext.Current);
        container.RegisterInstance<IScheduler>(mainWindowsScheduler);

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
#if DEBUG
                .AddZLoggerStream(new DebugOutputStream(mainWindowsScheduler), "debug-plain", opt => { })
#endif                    
                ;

            if (IsDebugModeEnabled)
            {
                FileStream _logFileStream = new(ApplicationData.Current.TemporaryFolder.CreateSafeFileHandle("_log.txt", System.IO.FileMode.OpenOrCreate, FileAccess.Write), FileAccess.Write, 65536);
                _logFileStream.SetLength(0);
                builder.AddFilter("Hohoema.App", DebugLogLevel)
                    .AddZLoggerStream(_logFileStream, "file-plain", opt => { opt.EnableStructuredLogging = false; })
                    ;
            }
            else
            {
#if DEBUG
                builder.AddFilter("Hohoema.App", LogLevel.Debug);
#else
                if (Debugger.IsAttached)
                {
                    builder.AddFilter("Hohoema.App", LogLevel.Debug);
                }
                else 
                {
                    builder.AddFilter("Hohoema.App", LogLevel.Error);
                }
                
#endif
            }
        });

        ILogger<App> logger = _loggerFactory.CreateLogger<App>();
        container.RegisterInstance<ILoggerFactory>(_loggerFactory);
        container.RegisterInstance<ILogger>(logger);
        container.RegisterInstance<ILogger<App>>(logger);

        ViewLocator viewLocator = new();
        viewLocator.RegisterForNavigation<Views.Pages.BlankPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.DebugPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.SettingsPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.LocalMylist.LocalPlaylistPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.LocalMylist.LocalPlaylistManagePage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.Queue.VideoQueuePage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.Subscription.SubscriptionManagementPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.Subscription.SubscVideoListPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Hohoema.VideoCache.CacheManagementPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Activity.WatchHistoryPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Channel.ChannelVideoPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Community.CommunityPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Community.CommunityVideoPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Follow.FollowManagePage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Live.LiveInfomationPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Live.TimeshiftPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Mylist.MylistPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Mylist.OwnerMylistManagePage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Mylist.UserMylistPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.FollowingsActivity.FollowingsActivityPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Search.SearchPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Search.SearchResultTagPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Search.SearchResultKeywordPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Search.SearchResultLivePage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Series.SeriesPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Series.UserSeriesPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.User.UserInfoPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.User.UserVideoPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.Video.VideoInfomationPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.VideoRanking.RankingCategoryListPage>();
        viewLocator.RegisterForNavigation<Views.Pages.Niconico.VideoRanking.RankingCategoryPage>();

        viewLocator.RegisterForNavigation<Views.Player.LivePlayerPage>();
        viewLocator.RegisterForNavigation<Views.Player.LegacyVideoPlayerPage>();
        viewLocator.RegisterForNavigation<Views.Player.VideoPlayerPage>();
        container.UseInstance<IViewLocator>(viewLocator);

        NavigationService.ViewTypeResolver = viewLocator.ResolveViewType;
    }
    
    public void RegisterTypes(IContainer container)
    {
        //            unityContainer.Register<PrimaryViewPlayerManager>(made: Made.Of().Parameters.Name("navigationServiceLazy", x => new Lazy<INavigationService>(() => unityContainer.Resolve<INavigationService>(serviceKey: "PrimaryPlayerNavigationService"))));

        container.UseInstance<LocalObjectStorageHelper>(new LocalObjectStorageHelper(new SystemTextJsonSerializer()));

        container.UseInstance<IMessenger>(WeakReferenceMessenger.Default);

        string listDbUpgradeConnectionStringParam = SystemInformation.Instance.IsAppUpdated
            ? "Upgrade=true;"
            : ""
            ;

        container.UseInstance<LiteDatabase>(new LiteDatabase($"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, "hohoema.db")};{listDbUpgradeConnectionStringParam}"));

        LiteDatabase tempDb = new($"Filename={Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "thumbnail_cache.db")};{listDbUpgradeConnectionStringParam}");
        container.UseInstance<LiteDatabase>(tempDb, serviceKey: "TempDb");
        container.Register<ThumbnailCacheManager>(reuse: Reuse.Singleton, made: Made.Of(() => new ThumbnailCacheManager(tempDb)));
        
        container.RegisterDelegate<NicoVideoCacheRepository>((c) => new NicoVideoCacheRepository(tempDb), Reuse.Singleton);
        container.RegisterDelegate(c => new SubscFeedVideoRepository(tempDb));
        container.RegisterDelegate<IPlayerView>(c =>
        {
            AppearanceSettings appearanceSettings = c.Resolve<AppearanceSettings>();
            return appearanceSettings.PlayerDisplayView == PlayerDisplayView.PrimaryView
                ? c.Resolve<PrimaryViewPlayerManager>()
                : c.Resolve<AppWindowSecondaryViewPlayerManager>(args: new object[] { (IPlayerView)Container.Resolve<AppWindowSecondaryViewPlayerManager>() });
        });

        // MediaPlayerを各ウィンドウごとに一つずつ作るように
        container.Register<MediaPlayer>(reuse: Reuse.Singleton);

        // 再生プレイリスト管理のクラスは各ウィンドウごとに一つずつ作成
        container.Register<HohoemaPlaylistPlayer>(reuse: Reuse.Singleton);

        // Service
        container.Register<PrimaryViewPlayerManager>(reuse: Reuse.Singleton);
        container.Register<SecondaryViewPlayerManager>(reuse: Reuse.Singleton);
        container.Register<AppWindowSecondaryViewPlayerManager>(reuse: Reuse.Singleton);
        container.Register<NiconicoLoginService>(reuse: Reuse.Singleton);
        container.Register<DialogService>(reuse: Reuse.Singleton);
        container.RegisterMapping<IDialogService, DialogService>();
        container.RegisterMapping<IMylistGroupDialogService, DialogService>();
        container.RegisterMapping<ISelectionDialogService, DialogService>();
        container.Register<INotificationService, NotificationService>(reuse: Reuse.Singleton);
        container.Register<NoUIProcessScreenContext>(reuse: Reuse.Singleton);
        container.Register<CurrentActiveWindowUIContextService>(reuse: Reuse.Singleton);
        container.Register<SubscriptionUpdateManager>(reuse: Reuse.Singleton);
        container.RegisterMapping<IToastActivationAware, SubscriptionUpdateManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.RegisterMapping<ISuspendAndResumeAware, SubscriptionUpdateManager>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.Register<NavigationTriggerFromExternal>(reuse: Reuse.Singleton);
        container.RegisterMapping<IToastActivationAware, NavigationTriggerFromExternal>(ifAlreadyRegistered: IfAlreadyRegistered.AppendNotKeyed);
        container.Register<ISubscriptionDialogService, SubscriptionDialogService>();

        // container.Register<ILocalizeService, LocalizeService>(); とした場合に
        // - System.PlatformNotSupportedException
        // - Dynamic code generation is not supported on this platform.
        // と例外が出てしまうのでインスタンス登録で代用している
        container.RegisterInstance<ILocalizeService>(new LocalizeService());


        // Models
        container.Register<AppearanceSettings>(reuse: Reuse.Singleton);
        container.Register<PinSettings>(reuse: Reuse.Singleton);
        container.Register<PlayerSettings>(reuse: Reuse.Singleton);
        container.Register<VideoFilteringSettings>(reuse: Reuse.Singleton);
        container.Register<VideoRankingSettings>(reuse: Reuse.Singleton);
        container.Register<CommentFliteringRepository>(reuse: Reuse.Singleton);
        container.Register<QueuePlaylist>(reuse: Reuse.Singleton);

        container.Register<NicoVideoProvider>(reuse: Reuse.Singleton);


        container.Register<NiconicoSession>(reuse: Reuse.Singleton);
        container.Register<NicoVideoSessionOwnershipManager>(reuse: Reuse.Singleton);

        container.Register<LoginUserOwnedMylistManager>(reuse: Reuse.Singleton);

        container.Register<SubscriptionManager>(reuse: Reuse.Singleton);

        container.Register<Models.VideoCache.VideoCacheManager>(reuse: Reuse.Singleton);
        container.Register<Models.VideoCache.VideoCacheSettings>(reuse: Reuse.Singleton);

        // UseCase
        container.Register<VideoCommentPlayer>(reuse: Reuse.Singleton);
        container.Register<CommentFilteringFacade>(reuse: Reuse.Singleton);
        container.Register<MediaPlayerSoundVolumeManager>(reuse: Reuse.Singleton);
        container.Register<LocalMylistManager>(reuse: Reuse.Singleton);
        container.Register<VideoItemsSelectionContext>(reuse: Reuse.Singleton);
        container.Register<WatchHistoryManager>(reuse: Reuse.Singleton);
        container.Register<ApplicationLayoutManager>(reuse: Reuse.Singleton);

        container.Register<VideoCacheFolderManager>(reuse: Reuse.Singleton);

        container.Register<IPlaylistFactoryResolver, PlaylistItemsSourceResolver>(reuse: Reuse.Singleton);

    }
    
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _IsPreLaunch = args.PrelaunchActivated;

        Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.TrackAppUse(args);

        await EnsureInitializeAsync();        
    }
    
    protected override async void OnActivated(IActivatedEventArgs args)
    {
        // 外部から起動した場合にサインイン動作と排他的動作にさせたい
        // こうしないと再生処理を正常に開始できない
        if (args.Kind == ActivationKind.ToastNotification)
        {
            await EnsureInitializeAsync();

            using (await Container.Resolve<NiconicoSession>().SigninLock.LockAsync())
            {
                await Task.Delay(50);
            }
            var toastArgs = (args as ToastNotificationActivatedEventArgs)!;
            await ProcessToastActivation(toastArgs.Argument, toastArgs.UserInput);
        }
    }

    protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
    {
        BackgroundTaskDeferral deferral = args.TaskInstance.GetDeferral();

        await EnsureInitializeAsync();

        try
        {
            switch (args.TaskInstance.Task.Name)
            {
                case "ToastBackgroundTask":
                    if (args.TaskInstance.TriggerDetails is Windows.UI.Notifications.ToastNotificationActionTriggerDetail details)
                    {
                        await ProcessToastActivation(details.Argument, details.UserInput);
                    }
                    break;
            }
        }
        finally
        {
            deferral.Complete();
        }
    }


    private async ValueTask ProcessToastActivation(string arguments, ValueSet userInput)
    {
        ToastArguments parsed = ToastArguments.Parse(arguments);
        foreach (var toastProcesser in Container.ResolveMany<IToastActivationAware>(behavior: ResolveManyBehavior.AsFixedArray))
        {
            if (await toastProcesser.TryHandleActivationAsync(parsed, userInput))
            {
                break;
            }
        }
    }


    private async Task EnsureInitializeAsync()
    {
        using IDisposable initializeLock = await InitializeLock.LockAsync();

        if (isInitialized) { return; }
        isInitialized = true;

        Container = ConfigureService();

        if (Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.IsAppUpdated)
        {
            await MigrationProcessAsync();
        }

        MaintenanceProcess();
        // 機能切り替え管理クラスをDIコンテナに登録
        // Xaml側で扱いやすくするためApp.xaml上でインスタンス生成させている
        {
            Container.UseInstance(Resources["FeatureFlags"] as FeatureFlags);
        }

        // ローカリゼーション用のライブラリを初期化
        try
        {
            I18NPortable.I18N.Current
#if DEBUG
            //.SetLogger(text => System.Diagnostics.Debug.WriteLine(text))
            .SetNotFoundSymbol("🍣")
#endif
            .SetFallbackLocale("ja")
            .Init(GetType().Assembly);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }

        Resources["Strings"] = I18NPortable.I18N.Current;

        AppearanceSettings appearanceSettings = Container.Resolve<Models.Application.AppearanceSettings>();
        I18NPortable.I18N.Current.Locale = appearanceSettings.Locale ?? I18NPortable.I18N.Current.Languages.FirstOrDefault(x => x.Locale.StartsWith(CultureInfo.CurrentCulture.TwoLetterISOLanguageName)).Locale ?? I18NPortable.I18N.Current.Locale;

        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(I18NPortable.I18N.Current.Locale);

        //Console.WriteLine(settings.AppearanceSettings.Locale);
        //Console.WriteLine(I18N.Current.Locale);
        //Console.WriteLine(CultureInfo.CurrentCulture.Name);


        Resources["IsXbox"] = DeviceTypeHelper.IsXbox;
        Resources["IsMobile"] = DeviceTypeHelper.IsMobile;


        try
        {
#if DEBUG
            if (_DEBUG_XBOX_RESOURCE)
#else
                if (DeviceTypeHelper.IsXbox)
#endif
            {
                Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri("ms-appx:///Styles/TVSafeColor.xaml")
                });
                Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri("ms-appx:///Styles/TVStyle.xaml")
                });
            }
        }
        catch
        {

        }

#if DEBUG
        Resources["IsDebug"] = true;
        Resources["IsDebug_XboxLayout"] = _DEBUG_XBOX_RESOURCE;
#else
        Resources["IsDebug"] = false;
        Resources["IsDebug_XboxLayout"] = false;
#endif
        Resources["TitleBarCustomized"] = IsTitleBarCustomized;
        Resources["TitleBarDummyHeight"] = IsTitleBarCustomized ? 32.0 : 0.0;


        if (IsTitleBarCustomized)
        {
            CoreApplicationView coreApp = CoreApplication.GetCurrentView();
            coreApp.TitleBar.ExtendViewIntoTitleBar = true;

            ApplicationView appView = ApplicationView.GetForCurrentView();
            appView.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appView.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            if (RequestedTheme == ApplicationTheme.Light)
            {
                appView.TitleBar.ButtonForegroundColor = Colors.Black;
                appView.TitleBar.ButtonHoverBackgroundColor = Colors.DarkGray;
                appView.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                appView.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
            }
            else
            {
                appView.TitleBar.ButtonForegroundColor = Colors.White;
                appView.TitleBar.ButtonHoverBackgroundColor = Colors.DimGray;
                appView.TitleBar.ButtonHoverForegroundColor = Colors.White;
                appView.TitleBar.ButtonInactiveForegroundColor = Colors.DarkGray;
            }
        }

        // 
        VideoCacheSettings_Legacy cacheSettings = Container.Resolve<VideoCacheSettings_Legacy>();
        Resources["IsCacheEnabled"] = cacheSettings.IsEnableCache;

        // ウィンドウコンテンツを作成
        Window.Current.Content = CreateShell();

        // ウィンドウサイズの保存と復元
        if (DeviceTypeHelper.IsDesktop)
        {
            LocalObjectStorageHelper localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
            if (localObjectStorageHelper.KeyExists(SecondaryViewPlayerManager.primary_view_size))
            {
                ApplicationView view = ApplicationView.GetForCurrentView();
                MainViewId = view.Id;
                _PrevWindowSize = localObjectStorageHelper.Read<Size>(SecondaryViewPlayerManager.primary_view_size);
                view.TryResizeView(_PrevWindowSize);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
            }
        }

        // XboxOneで外枠表示を行わないように設定
        if (DeviceTypeHelper.IsXbox)
        {
            Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().SetDesiredBoundsMode
                (Windows.UI.ViewManagement.ApplicationViewBoundsMode.UseCoreWindow);
        }

        // モバイルでナビゲーションバーをアプリに被せないように設定
        if (DeviceTypeHelper.IsMobile)
        {
            // モバイルで利用している場合に、ナビゲーションバーなどがページに被さらないように指定
            ApplicationView.GetForCurrentView().SuppressSystemOverlays = true;
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
        }

        // キャッシュ機能の初期化
        {
            VideoCacheFolderManager cacheManager = Container.Resolve<VideoCacheFolderManager>();

            await cacheManager.InitializeAsync();
        }




        // 2段階認証を処理するログインサービスをインスタンス化
        NiconicoLoginService loginService = Container.Resolve<NiconicoLoginService>();

        // ログイン前にログインセッションによって状態が変化するフォローとマイリストの初期化
        LoginUserOwnedMylistManager mylitManager = Container.Resolve<LoginUserOwnedMylistManager>();

        {
            Container.Resolve<Services.Migrations.CommentFilteringNGScoreZeroFixture>().Migration();

            // アプリのユースケース系サービスを配置
            Container.RegisterInstance(Container.Resolve<NotificationCacheVideoDeletedService>());
            Container.RegisterInstance(Container.Resolve<CheckingClipboardAndNotificationService>());
            Container.RegisterInstance(Container.Resolve<FollowNotificationAndConfirmListener>());
            Container.RegisterInstance(Container.Resolve<SubscriptionUpdateManager>());
            Container.RegisterInstance(Container.Resolve<SyncWatchHistoryOnLoggedIn>());
            
            Container.RegisterInstance(Container.Resolve<VideoPlayRequestBridgeToPlayer>());
            Container.RegisterInstance(Container.Resolve<CloseToastNotificationWhenPlayStarted>());
            Container.RegisterInstance(Container.Resolve<AutoSkipToPlaylistNextVideoWhenPlayFailed>());

            Container.RegisterInstance(Container.Resolve<VideoCacheDownloadOperationManager>());
        }

        // バックグラウンドでのトースト通知ハンドリングを初期化
        await RegisterDebugToastNotificationBackgroundHandling();

        // サムネイル画像キャッシュの初期化
        ImageCache.Instance.CacheDuration = TimeSpan.FromDays(30);
        ImageCache.Instance.MaxMemoryCacheCount = appearanceSettings.VideoListThumbnailCacheMaxCount;
        ImageCache.Instance.RetryCount = 2;
        await ImageCache.Instance.InitializeAsync(folderName: "nicovideo_thumb");

        /*
        if (args.PreviousExecutionState == ApplicationExecutionState.Terminated
            || args.PreviousExecutionState == ApplicationExecutionState.ClosedByUser)
        {
            if (!ApiContractHelper.Is2018FallUpdateAvailable)
            {

            }
        }
        else
        {


#if false
        try
        {
            if (localStorge.Read(IS_COMPLETE_INTRODUCTION, false) == false)
            {
                // アプリのイントロダクションを開始
                pageManager.OpenIntroductionPage();
            }
            else
            {
                pageManager.OpenStartupPage();
            }
        }
        catch
        {
            Debug.WriteLine("イントロダクションまたはスタートアップのページ表示に失敗");
            pageManager.OpenPage(HohoemaPageType.RankingCategoryList);
        }
#else
            try
            {
                pageManager.OpenStartupPage();
            }
            catch
            {
                Debug.WriteLine("スタートアップのページ表示に失敗");
                pageManager.OpenPage(HohoemaPageType.RankingCategoryList);
            }
#endif
        }
        */

        OnInitialized();
    }



    private UIElement CreateShell()
    {

        // Grid
        //   |- HohoemaInAppNotification
        //   |- PlayerWithPageContainerViewModel
        //   |    |- MenuNavigatePageBaseViewModel
        //   |         |- rootFrame 

        Container.Register<PrimaryWindowCoreLayout>();
        Container.Register<PrimaryWindowCoreLayoutViewModel>();

        _primaryWindowCoreLayout = Ioc.Default.GetRequiredService<PrimaryWindowCoreLayout>();
        Container unityContainer = Container;
        INavigationService primaryWindowContentNavigationService = _primaryWindowCoreLayout.CreateNavigationService();
        unityContainer.UseInstance(primaryWindowContentNavigationService);

        INavigationService primaryViewPlayerNavigationService = _primaryWindowCoreLayout.CreatePlayerNavigationService();
        string name = "PrimaryPlayerNavigationService";
        unityContainer.UseInstance(primaryViewPlayerNavigationService, serviceKey: name);


#if DEBUG
        _primaryWindowCoreLayout.FocusEngaged += (__, args) => Debug.WriteLine("focus engagad: " + args.OriginalSource.ToString());
#endif

        _primaryWindowCoreLayout.IsDebugModeEnabled = IsDebugModeEnabled;

        return _primaryWindowCoreLayout;
    }



    public bool IsTitleBarCustomized { get; } = DeviceTypeHelper.IsDesktop && InputCapabilityHelper.IsMouseCapable;

    private readonly Helpers.AsyncLock InitializeLock = new();
    private bool isInitialized = false;

    private async Task MigrationProcessAsync()
    {
        Type[] migrateTypes = new Type[]
        {
            typeof(VideoCacheDatabaseMigration_V_0_29_0),
            typeof(SearchTargetMigration_V_1_1_0),            
        };

        async Task TryMigrationAsync(Type migrateType)
        {
            ILogger logger = _loggerFactory.CreateLogger(migrateType);
            try
            {
                logger.LogInformation("Try migrate: {0}", migrateType.Name);
                object migrater = Container.Resolve(migrateType);
                if (migrater is IMigrateSync migrateSycn)
                {
                    migrateSycn.Migrate();
                }
                else if (migrater is IMigrateAsync migrateAsync)
                {
                    await migrateAsync.MigrateAsync();
                }

                logger.LogInformation("Migration complete : {0}", migrateType.Name);
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                logger.LogError("Migration failed : {0}", migrateType.Name);
            }
        }

        foreach (Type migrateType in migrateTypes)
        {
            await TryMigrationAsync(migrateType);
        }
    }

    private void MaintenanceProcess()
    {
        Type[] maintenanceTypes = new Type[]
        {
            typeof(VideoThumbnailImageCacheMaintenance),
        };

        void TryMaintenance(Type maintenanceType)
        {
            ILogger logger = _loggerFactory.CreateLogger(maintenanceType);

            try
            {
                logger.LogInformation("Try maintenance: {0}", maintenanceType.Name);
                object migrater = Container.Resolve(maintenanceType);
                if (migrater is IMaintenance maintenance)
                {
                    maintenance.Maitenance();
                }

                logger.LogInformation("Maintenance complete : {0}", maintenanceType.Name);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Maintenance failed : {0}", maintenanceType.Name);
            }
        }

        foreach (Type maintenanceType in maintenanceTypes)
        {
            TryMaintenance(maintenanceType);
        }
    }

    private readonly bool _isNavigationStackRestored = false;



    private async void OnInitialized()
    {
        Window.Current.Activate();

        CurrentActiveWindowUIContextService currentActiveWindowUIContextService = Ioc.Default.GetRequiredService<CurrentActiveWindowUIContextService>();
        CurrentActiveWindowUIContextService.SetUIContext(currentActiveWindowUIContextService, Window.Current.Content.UIContext, Window.Current.Content.XamlRoot);

        // 更新通知を表示
        try
        {
            if (AppUpdateNotice.IsUpdated)
            {
                Windows.ApplicationModel.PackageVersion version = Windows.ApplicationModel.Package.Current.Id.Version;
                NotificationService notificationService = Container.Resolve<NotificationService>();
                notificationService.ShowLiteInAppNotification(
                    ZString.Format("Hohoema v{0}.{1}.{2} に更新しました", version.Major, version.Minor, version.Build),
                    TimeSpan.FromSeconds(7)
                    );
                AppUpdateNotice.UpdateLastCheckedVersionInCurrentVersion();
            }
        }
        catch { }

        // ログイン
        try
        {
            NiconicoSession niconicoSession = Container.Resolve<NiconicoSession>();
            if (AccountManager.HasPrimaryAccount())
            {
                // サインイン処理の待ちを初期化内でしないことで初期画面表示を早める
                await niconicoSession.SignInWithPrimaryAccount();
            }
        }
        catch
        {
            Container.Resolve<ILogger>().LogError("ログイン処理に失敗");
        }

#if !DEBUG
        var messenger = Container.Resolve<IMessenger>();
        var settings = Container.Resolve<AppearanceSettings>();
        await messenger.OpenPageAsync(settings.FirstAppearPageType);
#endif

#if false
        try
        {
            if (!_isNavigationStackRestored)
            {
                var niconicoSession = Container.Resolve<NiconicoSession>();

                // 外部から起動した場合にサインイン動作と排他的動作にさせたい
                // こうしないと再生処理を正常に開始できない
                using (await niconicoSession.SigninLock.LockAsync())
                {
                    await Task.Delay(50);
                }
                _isNavigationStackRestored = true;
                //                    await _primaryWindowCoreLayout.RestoreNavigationStack();
                // TODO: 前回再生中に終了したコンテンツを表示するかユーザーに確認
                var vm = _primaryWindowCoreLayout.DataContext as PrimaryWindowCoreLayoutViewModel;
                var lastPlaying = vm.RestoreNavigationManager.GetCurrentPlayerEntry();
                if (lastPlaying != null)
                {
                    _ = WeakReferenceMessenger.Default.Send(new VideoPlayRequestMessage() { VideoId = lastPlaying.ContentId, PlaylistId = lastPlaying.PlaylistId, PlaylistOrigin = lastPlaying.PlaylistOrigin, Potision = lastPlaying.Position });
                }
            }
        }
        catch { }
#endif

        var update = await CheckUpdateAsync();
        if (update.HasAppUpdate && update.AppUpdate != null)
        {
            var v = update.AppUpdate.Package.Id.Version;
            messenger.Send(new InAppNotificationMessage(new() 
            {                
                Content = $"アプリの更新が利用できます -> v{v.Major}.{v.Minor}.{v.Build}.{v.Revision}",
                Commands = { new InAppNotificationCommand() { Label = "確認する", Command = new RelayCommand(() => 
                {
                    var ns = Container.Resolve<INavigationService>();
                    ns.NavigateAsync(nameof(SettingsPage));
                })}}
            }));
        }
    }



  


    #region Multi Window Size Restoring


    private int MainViewId = -1;
    private Size _PrevWindowSize;
    private PrimaryWindowCoreLayout _primaryWindowCoreLayout;

    [Obsolete]
    protected override void OnWindowCreated(WindowCreatedEventArgs args)
    {
        base.OnWindowCreated(args);

        ApplicationView view = ApplicationView.GetForCurrentView();
        view.VisibleBoundsChanged += (sender, e) =>
        {
            if (MainViewId == sender.Id)
            {
                LocalObjectStorageHelper localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                _PrevWindowSize = localObjectStorageHelper.Read<Size>(SecondaryViewPlayerManager.primary_view_size);
                localObjectStorageHelper.Save(SecondaryViewPlayerManager.primary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));

                Debug.WriteLine("MainView VisibleBoundsChanged : " + sender.VisibleBounds.ToString());
            }
        };
        view.Consolidated += (sender, e) =>
        {
            if (sender.Id == MainViewId)
            {
                LocalObjectStorageHelper localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                if (_PrevWindowSize != default)
                {
                    localObjectStorageHelper.Save(SecondaryViewPlayerManager.primary_view_size, _PrevWindowSize);
                }
                MainViewId = -1;
            }
        };
    }


    #endregion


    #region Theme 


    private const string ThemeTypeKey = "Theme";

    public static void SetTheme(ApplicationTheme theme)
    {
        if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ThemeTypeKey))
        {
            ApplicationData.Current.LocalSettings.Values[ThemeTypeKey] = theme.ToString();
        }
        else
        {
            ApplicationData.Current.LocalSettings.Values.Add(ThemeTypeKey, theme.ToString());
        }
    }

    public static ApplicationTheme GetTheme()
    {
        try
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(ThemeTypeKey))
            {
                return (ApplicationTheme)Enum.Parse(typeof(ApplicationTheme), (string)ApplicationData.Current.LocalSettings.Values[ThemeTypeKey]);
            }
        }
        catch { }

        return ApplicationTheme.Dark;
    }

    #endregion


    #region Debug

    private const string DEBUG_MODE_ENABLED_KEY = "Hohoema_DebugModeEnabled";
    public bool IsDebugModeEnabled
    {
        get
        {
            object enabled = ApplicationData.Current.LocalSettings.Values[DEBUG_MODE_ENABLED_KEY];
            return enabled != null && (bool)enabled;
        }

        set
        {
            ApplicationData.Current.LocalSettings.Values[DEBUG_MODE_ENABLED_KEY] = value;
            _primaryWindowCoreLayout.IsDebugModeEnabled = value;
        }
    }

    private const string DEBUG_LOG_LEVEL_KEY = "Hohoema_LogLevel";
    public LogLevel DebugLogLevel
    {
        get
        {
            object enabled = ApplicationData.Current.LocalSettings.Values[DEBUG_LOG_LEVEL_KEY];
            return enabled != null ? (LogLevel)enabled : LogLevel.Debug;
        }

        set => ApplicationData.Current.LocalSettings.Values[DEBUG_LOG_LEVEL_KEY] = value;
    }

    private void PrismUnityApplication_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Debug.Write(e.Message);

        e.Handled = true;

        if (e.Exception is OperationCanceledException)
        {
            return;
        }

        if (e.Exception is ObjectDisposedException)
        {
            return;
        }

        ILogger logger = Container.Resolve<ILogger>();
        logger.LogError(e.Exception.ToString());

        if (e.Exception is HohoemaException)
        {
            return;
        }
    }

    // エラー報告用に画面のスクショを取れるように
    public async Task<Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap> GetApplicationContentImage()
    {
        Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap rtb = new();
        await rtb.RenderAsync(_primaryWindowCoreLayout);
        return rtb;
    }

    public async Task<Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap> GetApplicationContentImage(int scaledWidth, int scaledHeight)
    {
        Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap rtb = new();
        await rtb.RenderAsync(_primaryWindowCoreLayout, scaledWidth, scaledHeight);
        return rtb;
    }



    private async Task RegisterDebugToastNotificationBackgroundHandling()
    {
        try
        {
            const string taskName = "ToastBackgroundTask";

            // If background task is already registered, do nothing
            if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(taskName)))
            {
                return;
            }

            // Otherwise request access
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

            // Create the background task
            BackgroundTaskBuilder builder = new()
            {
                Name = taskName
            };

            // Assign the toast action trigger
            builder.SetTrigger(new ToastNotificationActionTrigger());

            // And register the task
            BackgroundTaskRegistration registration = builder.Register();
        }
        catch { }
    }


    #endregion

    public async Task<CheckUpdateResult> CheckUpdateAsync(CancellationToken ct = default)
    {
        var storeContext = StoreContext.GetDefault();
        IReadOnlyList<StorePackageUpdate> updates = await storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
        return new CheckUpdateResult(storeContext, updates);
    }
}


public class CheckUpdateResult
{
    private readonly StoreContext _storeContext;
    private readonly IReadOnlyList<StorePackageUpdate> _updates;

    public CheckUpdateResult(StoreContext storeContext, IReadOnlyList<StorePackageUpdate> updates)
    {
        _storeContext = storeContext;
        _updates = updates;
    }

    public bool HasAppUpdate
    {
        get
        {
            if (AppUpdate is { } appUpdate)
            {
                var currentAppVersion = Windows.ApplicationModel.AppInfo.Current.Package.Id.Version;
                var updateVer = appUpdate.Package.Id.Version;
                return currentAppVersion.Major < updateVer.Major
                || currentAppVersion.Minor < updateVer.Minor
                || currentAppVersion.Build < updateVer.Build
                || currentAppVersion.Revision < updateVer.Revision
                ;
            }
            else
            {
                return false;
            }
        }
    }

    public bool CanDownloadSilently => _storeContext.CanSilentlyDownloadStorePackageUpdates;

    public StorePackageUpdate? AppUpdate => _updates.FirstOrDefault(x => x.Package.DisplayName == "Hohoema");

    public IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> DownloadAndInstallAllUpdatesAsync()
    {
        if (CanDownloadSilently)
        {
            return _storeContext.TrySilentDownloadAndInstallStorePackageUpdatesAsync(_updates);
        }
        else
        {
            return _storeContext.RequestDownloadAndInstallStorePackageUpdatesAsync(_updates);
        }
    }
}


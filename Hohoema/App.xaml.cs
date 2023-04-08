using Hohoema.Models;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.NicoRepo;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Player;
using Hohoema.Models.Player.Video;
using Hohoema.Models.Player.Video.Cache;
using Hohoema.Models.Subscriptions;
using Hohoema.Helpers;
using Hohoema.Services;
using Hohoema.Services.Migration;
using Hohoema.Services.Player;
using Hohoema.Services.Subscriptions;
using Hohoema.Services.VideoCache;
using Hohoema.Services;
using Hohoema.Services.Navigations;
using Hohoema.ViewModels;
using LiteDB;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
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
using Windows.UI.Xaml.Controls;
using Hohoema.Views.Pages;
using Hohoema.Services.Niconico.Account;
using Hohoema.Services.Niconico.Follow;
using Hohoema.Models.Notification;
using Hohoema.Models.VideoCache;
using Windows.Storage.AccessCache;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Extensions.Logging;
using Hohoema.Infra;
using Hohoema.Services.Playlist;
using Hohoema.Services.Player.Videos;
using Hohoema.Services.Niconico.Video;
using Hohoema.ViewModels.Niconico.Video;
using Hohoema.Models.Player.Comment;
using Hohoema.Models.Playlist;
using Hohoema.Services.LocalMylist;
using DryIoc;
using ZLogger;
using Cysharp.Text;
using Windows.UI.Core;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Hohoema
{
    sealed class ViewLocator : IViewLocator
    {
        public Type ResolveView(string viewName)
        {
            return _registory[viewName];
        }

        public void RegisterForNavigation<T>()
        {
            var type = typeof(T);
            _registory.Add(type.Name, type);
        }

        private readonly Dictionary<string, Type> _registory = new Dictionary<string, Type>();

        internal Type ResolveViewType(string viewName)
        {
            return _registory[viewName];
        }
    }

    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Application
    {
        const bool _DEBUG_XBOX_RESOURCE = true;

        public SplashScreen SplashScreen { get; private set; }

        private bool _IsPreLaunch;

        public const string ACTIVATION_WITH_ERROR = "error";
        public const string ACTIVATION_WITH_ERROR_OPEN_LOG = "error_open_log";
        public const string ACTIVATION_WITH_ERROR_COPY_LOG = "error_copy_log";

        internal const string IS_COMPLETE_INTRODUCTION = "is_first_launch";


        public new static App Current => (App)Application.Current;

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
            this.RequiresPointerMode = Windows.UI.Xaml.ApplicationRequiresPointerMode.WhenRequested;

            // テーマ設定
            // ThemeResourceの切り替えはアプリの再起動が必要
            RequestedTheme = GetTheme();

            this.InitializeComponent();
        }


        private Container ConfigureService()
        {
            var rules = Rules.Default
                .WithConcreteTypeDynamicRegistrations((serviceType, serviceKey) => true, Reuse.Singleton)
                .WithAutoConcreteTypeResolution()
                .With(Made.Of(FactoryMethod.ConstructorWithResolvableArguments))
                .WithoutThrowOnRegisteringDisposableTransient()
                .WithFuncAndLazyWithoutRegistration()
                .WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Replace)
                .WithoutThrowOnRegisteringDisposableTransient()
                ;

            var container = new Container(rules);

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

            long _length;
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
            var mainWindowsScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
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
                    FileStream _logFileStream = new FileStream(ApplicationData.Current.TemporaryFolder.CreateSafeFileHandle("_log.txt", System.IO.FileMode.OpenOrCreate, FileAccess.Write), FileAccess.Write, 65536);
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

            var logger = _loggerFactory.CreateLogger<App>();
            container.RegisterInstance<ILoggerFactory>(_loggerFactory);
            container.RegisterInstance<ILogger>(logger);
            container.RegisterInstance<ILogger<App>>(logger);

            ViewLocator viewLocator = new ViewLocator();
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
            viewLocator.RegisterForNavigation<Views.Pages.Niconico.NicoRepo.NicoRepoPage>();
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

            NavigationService.ViewTypeResolver = (pageName) => viewLocator.ResolveViewType(pageName);
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

            var tempDb = new LiteDatabase($"Filename={Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "thumbnail_cache.db")};{listDbUpgradeConnectionStringParam}");
            container.UseInstance<LiteDatabase>(tempDb, serviceKey: "TempDb");
            container.Register<ThumbnailCacheManager>(reuse: new SingletonReuse(), made: Made.Of(() => new ThumbnailCacheManager(tempDb)));

            container.RegisterDelegate<NicoVideoCacheRepository>((c) => new NicoVideoCacheRepository(tempDb), new SingletonReuse());
            container.RegisterDelegate<IPlayerView>(c =>
            {
                var appearanceSettings = c.Resolve<AppearanceSettings>();
                if (appearanceSettings.PlayerDisplayView == PlayerDisplayView.PrimaryView)
                {
                    return c.Resolve<PrimaryViewPlayerManager>();
                }
                else
                {
                    return c.Resolve<AppWindowSecondaryViewPlayerManager>();
                }
            });

            // MediaPlayerを各ウィンドウごとに一つずつ作るように
            container.Register<MediaPlayer>(reuse: new SingletonReuse());

            // 再生プレイリスト管理のクラスは各ウィンドウごとに一つずつ作成
            container.Register<HohoemaPlaylistPlayer>(reuse: new SingletonReuse());

            // Service
            container.Register<PageManager>(reuse: new SingletonReuse());
            container.Register<PrimaryViewPlayerManager>(reuse: new SingletonReuse());
            container.Register<SecondaryViewPlayerManager>(reuse: new SingletonReuse());
            container.Register<AppWindowSecondaryViewPlayerManager>(reuse: new SingletonReuse());
            container.Register<NiconicoLoginService>(reuse: new SingletonReuse());
            container.Register<DialogService>(reuse: new SingletonReuse());
            container.Register<INotificationService, NotificationService>(reuse: new SingletonReuse());
            container.Register<NoUIProcessScreenContext>(reuse: new SingletonReuse());
            container.Register<CurrentActiveWindowUIContextService>(reuse: new SingletonReuse());

            // Models
            container.Register<AppearanceSettings>(reuse: new SingletonReuse());
            container.Register<PinSettings>(reuse: new SingletonReuse());
            container.Register<PlayerSettings>(reuse: new SingletonReuse());
            container.Register<VideoFilteringSettings>(reuse: new SingletonReuse());
            container.Register<VideoRankingSettings>(reuse: new SingletonReuse());
            container.Register<NicoRepoSettings>(reuse: new SingletonReuse());
            container.Register<CommentFliteringRepository>(reuse: new SingletonReuse());
            container.Register<QueuePlaylist>(reuse: new SingletonReuse());

            container.Register<NicoVideoProvider>(reuse: new SingletonReuse());


            container.Register<NiconicoSession>(reuse: new SingletonReuse());
            container.Register<NicoVideoSessionOwnershipManager>(reuse: new SingletonReuse());

            container.Register<LoginUserOwnedMylistManager>(reuse: new SingletonReuse());

            container.Register<SubscriptionManager>(reuse: new SingletonReuse());

            container.Register<Models.VideoCache.VideoCacheManager>(reuse: new SingletonReuse());
            container.Register<Models.VideoCache.VideoCacheSettings>(reuse: new SingletonReuse());

            // UseCase
            container.Register<VideoCommentPlayer>(reuse: new SingletonReuse());
            container.Register<CommentFilteringFacade>(reuse: new SingletonReuse());
            container.Register<MediaPlayerSoundVolumeManager>(reuse: new SingletonReuse());
            container.Register<LocalMylistManager>(reuse: new SingletonReuse());
            container.Register<VideoItemsSelectionContext>(reuse: new SingletonReuse());
            container.Register<WatchHistoryManager>(reuse: new SingletonReuse());
            container.Register<ApplicationLayoutManager>(reuse: new SingletonReuse());

            container.Register<VideoCacheFolderManager>(reuse: new SingletonReuse());

            container.Register<IPlaylistFactoryResolver, PlaylistItemsSourceResolver>(reuse: new SingletonReuse());

        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            _IsPreLaunch = args.PrelaunchActivated;

            Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.TrackAppUse(args);

            await EnsureInitializeAsync();
            OnInitialized();
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

                await Container.Resolve<NavigationTriggerFromExternal>().Process((args as ToastNotificationActivatedEventArgs).Argument);
            }
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var deferral = args.TaskInstance.GetDeferral();

            await EnsureInitializeAsync();

            try
            {
                switch (args.TaskInstance.Task.Name)
                {
                    case "ToastBackgroundTask":
                        var details = args.TaskInstance.TriggerDetails as Windows.UI.Notifications.ToastNotificationActionTriggerDetail;
                        if (details != null)
                        {                            
                            string arguments = details.Argument;
                            var userInput = details.UserInput;

                            await Task.Run(() => Container.Resolve<NavigationTriggerFromExternal>().Process(arguments));
                        }
                        break;
                }
            }
            finally
            {
                deferral.Complete();
            }
        }


        private async Task EnsureInitializeAsync()
        {
            using var initializeLock = await InitializeLock.LockAsync();

            if (isInitialized) { return; }
            isInitialized = true;

            Container = ConfigureService();

            if (Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.IsAppUpdated)
            {
                await MigrationProcessAsync();
            }

            await MaintenanceProcessAsync();
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

            var appearanceSettings = Container.Resolve<Models.Application.AppearanceSettings>();
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
                    this.Resources.MergedDictionaries.Add(new ResourceDictionary()
                    {
                        Source = new Uri("ms-appx:///Styles/TVSafeColor.xaml")
                    });
                    this.Resources.MergedDictionaries.Add(new ResourceDictionary()
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
                var coreApp = CoreApplication.GetCurrentView();
                coreApp.TitleBar.ExtendViewIntoTitleBar = true;

                var appView = ApplicationView.GetForCurrentView();
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
            var cacheSettings = Container.Resolve<VideoCacheSettings_Legacy>();
            Resources["IsCacheEnabled"] = cacheSettings.IsEnableCache;

            // ウィンドウコンテンツを作成
            Window.Current.Content = CreateShell();

            // ウィンドウサイズの保存と復元
            if (DeviceTypeHelper.IsDesktop)
            {
                var localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                if (localObjectStorageHelper.KeyExists(SecondaryViewPlayerManager.primary_view_size))
                {
                    var view = ApplicationView.GetForCurrentView();
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
                var cacheManager = Container.Resolve<VideoCacheFolderManager>();

                await cacheManager.InitializeAsync();
            }




            // 2段階認証を処理するログインサービスをインスタンス化
            var loginService = Container.Resolve<NiconicoLoginService>();

            // ログイン前にログインセッションによって状態が変化するフォローとマイリストの初期化
            var mylitManager = Container.Resolve<LoginUserOwnedMylistManager>();

            {
                Container.Resolve<Services.Migration.CommentFilteringNGScoreZeroFixture>().Migration();

                // アプリのユースケース系サービスを配置
                Container.RegisterInstance(Container.Resolve<NotificationCacheVideoDeletedService>());
                Container.RegisterInstance(Container.Resolve<CheckingClipboardAndNotificationService>());
                Container.RegisterInstance(Container.Resolve<FollowNotificationAndConfirmListener>());
                Container.RegisterInstance(Container.Resolve<SubscriptionUpdateManager>());
                Container.RegisterInstance(Container.Resolve<SyncWatchHistoryOnLoggedIn>());
                Container.RegisterInstance(Container.Resolve<NewFeedVideosProcesser>());

                Container.RegisterInstance(Container.Resolve<VideoPlayRequestBridgeToPlayer>());
                Container.RegisterInstance(Container.Resolve<CloseToastNotificationWhenPlayStarted>());
                Container.RegisterInstance(Container.Resolve<AutoSkipToPlaylistNextVideoWhenPlayFailed>());

                Container.RegisterInstance(Container.Resolve<VideoCacheDownloadOperationManager>());
            }

            // バックグラウンドでのトースト通知ハンドリングを初期化
            await RegisterDebugToastNotificationBackgroundHandling();




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
                var pageManager = Container.Resolve<Services.PageManager>();


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

#pragma warning disable IDISP001 // Dispose created.
            var unityContainer = Container;
#pragma warning restore IDISP001 // Dispose created.
            var primaryWindowContentNavigationService = _primaryWindowCoreLayout.CreateNavigationService();
            unityContainer.UseInstance(primaryWindowContentNavigationService);

            var primaryViewPlayerNavigationService = _primaryWindowCoreLayout.CreatePlayerNavigationService();
            var name = "PrimaryPlayerNavigationService";
            unityContainer.UseInstance(primaryViewPlayerNavigationService, serviceKey: name);


#if DEBUG
            _primaryWindowCoreLayout.FocusEngaged += (__, args) => Debug.WriteLine("focus engagad: " + args.OriginalSource.ToString());
#endif

            _primaryWindowCoreLayout.IsDebugModeEnabled = IsDebugModeEnabled;

            return _primaryWindowCoreLayout;
        }

       

        public bool IsTitleBarCustomized { get; } = DeviceTypeHelper.IsDesktop && InputCapabilityHelper.IsMouseCapable;

        Helpers.AsyncLock InitializeLock = new Helpers.AsyncLock();
        bool isInitialized = false;

        async Task MigrationProcessAsync()
        {
            Type[] migrateTypes = new Type[]
            {
                typeof(VideoCacheDatabaseMigration_V_0_29_0),
                typeof(SearchTargetMigration_V_1_1_0),
                typeof(SubscriptionMigration_1_3_13),
            };

            async Task TryMigrationAsync(Type migrateType)
            {
                var logger = _loggerFactory.CreateLogger(migrateType);
                try
                {
                    logger.ZLogInformation("Try migrate: {0}", migrateType.Name);
                    var migrater = Container.Resolve(migrateType);
                    if (migrater is IMigrateSync migrateSycn)
                    {
                        migrateSycn.Migrate();
                    }
                    else if (migrater is IMigrateAsync migrateAsync)
                    {
                        await migrateAsync.MigrateAsync();
                    }

                    logger.ZLogInformation("Migration complete : {0}", migrateType.Name);
                }
                catch (Exception e)
                {
                    logger.ZLogError(e.ToString());
                    logger.ZLogError("Migration failed : {0}", migrateType.Name);
                }
            }

            foreach (var migrateType in migrateTypes)
            {
                await TryMigrationAsync(migrateType);
            }
        }

        async Task MaintenanceProcessAsync()
        {
            Type[] maintenanceTypes = new Type[]
            {
                typeof(Services.Maintenance.VideoThumbnailImageCacheMaintenance),
            };

            async Task TryMaintenanceAsync(Type maintenanceType)
            {
                var logger = _loggerFactory.CreateLogger(maintenanceType);

                try
                {
                    logger.ZLogInformation("Try maintenance: {0}", maintenanceType.Name);
                    var migrater = Container.Resolve(maintenanceType);
                    if (migrater is Services.Maintenance.IMaintenance maintenance)
                    {
                        maintenance.Maitenance();
                    }

                    logger.ZLogInformation("Maintenance complete : {0}", maintenanceType.Name);
                }
                catch (Exception e)
                {
                    logger.ZLogError(e, "Maintenance failed : {0}", maintenanceType.Name);
                }
            }

            foreach (var maintenanceType in maintenanceTypes)
            {
                await TryMaintenanceAsync(maintenanceType);
            }
        }

       


        bool _isNavigationStackRestored = false;



        private async void OnInitialized()
        {
            Window.Current.Activate();

            var currentActiveWindowUIContextService = Ioc.Default.GetRequiredService<CurrentActiveWindowUIContextService>();
            CurrentActiveWindowUIContextService.SetUIContext(currentActiveWindowUIContextService , Window.Current.Content.UIContext, Window.Current.Content.XamlRoot);

            // 更新通知を表示
            try
            {
                if (AppUpdateNotice.IsUpdated)
                {
                    var version = Windows.ApplicationModel.Package.Current.Id.Version;
                    var notificationService = Container.Resolve<NotificationService>();
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
                var niconicoSession = Container.Resolve<NiconicoSession>();
                if (AccountManager.HasPrimaryAccount())
                {
                    // サインイン処理の待ちを初期化内でしないことで初期画面表示を早める
                    await niconicoSession.SignInWithPrimaryAccount();
                }
            }
            catch
            {
                Container.Resolve<ILogger>().ZLogError("ログイン処理に失敗");
            }

#if !DEBUG
            var navigationService = Container.Resolve<PageManager>();
            var settings = Container.Resolve<AppearanceSettings>();
            navigationService.OpenPage(settings.FirstAppearPageType);
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
        }






#region Multi Window Size Restoring


        private int MainViewId = -1;
        private Size _PrevWindowSize;
        private PrimaryWindowCoreLayout _primaryWindowCoreLayout;

        protected override void OnWindowCreated(WindowCreatedEventArgs args)
		{
			base.OnWindowCreated(args);

            var view = ApplicationView.GetForCurrentView();
            view.VisibleBoundsChanged += (sender, e) => 
            {
                if (MainViewId == sender.Id)
                {
                    var localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                    _PrevWindowSize = localObjectStorageHelper.Read<Size>(SecondaryViewPlayerManager.primary_view_size);
                    localObjectStorageHelper.Save(SecondaryViewPlayerManager.primary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));

                    Debug.WriteLine("MainView VisibleBoundsChanged : " + sender.VisibleBounds.ToString());
                }
            };
            view.Consolidated += (sender, e) => 
            {
                if (sender.Id == MainViewId)
                {
                    var localObjectStorageHelper = Container.Resolve<Microsoft.Toolkit.Uwp.Helpers.LocalObjectStorageHelper>();
                    if (_PrevWindowSize != default(Size))
                    {
                        localObjectStorageHelper.Save(SecondaryViewPlayerManager.primary_view_size, _PrevWindowSize);
                    }
                    MainViewId = -1;
                }
            };
        }


#endregion


#region Theme 


        const string ThemeTypeKey = "Theme";

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

        const string DEBUG_MODE_ENABLED_KEY = "Hohoema_DebugModeEnabled";
        public bool IsDebugModeEnabled
        {
            get
            {
                var enabled = ApplicationData.Current.LocalSettings.Values[DEBUG_MODE_ENABLED_KEY];
                if (enabled != null)
                {
                    return (bool)enabled;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                ApplicationData.Current.LocalSettings.Values[DEBUG_MODE_ENABLED_KEY] = value;
                _primaryWindowCoreLayout.IsDebugModeEnabled = value;
            }
        }
        
        const string DEBUG_LOG_LEVEL_KEY = "Hohoema_LogLevel";
        public LogLevel DebugLogLevel
        {
            get
            {
                var enabled = ApplicationData.Current.LocalSettings.Values[DEBUG_LOG_LEVEL_KEY];
                if (enabled != null)
                {
                    return (LogLevel)enabled;
                }
                else
                {
                    return LogLevel.Debug;
                }
            }

            set
            {
                ApplicationData.Current.LocalSettings.Values[DEBUG_LOG_LEVEL_KEY] = value;
            }
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

            var logger = Container.Resolve<ILogger>();
            logger.ZLogError(e.Exception.ToString());

            if (e.Exception is HohoemaException)
            {
                return;
            }
        }

        // エラー報告用に画面のスクショを取れるように
        public async Task<Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap> GetApplicationContentImage()
        {
            var rtb = new Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap();
            await rtb.RenderAsync(_primaryWindowCoreLayout);
            return rtb;
        }

        public async Task<Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap> GetApplicationContentImage(int scaledWidth, int scaledHeight)
        {
            var rtb = new Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap();
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
                    return;

                // Otherwise request access
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();

                // Create the background task
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
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

    }





}

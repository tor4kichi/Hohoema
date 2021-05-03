using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.NicoRepo;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Pins;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Migration;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.NicoVideos.Player;
using Hohoema.Models.UseCase.Subscriptions;
using Hohoema.Models.UseCase.VideoCache;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.UseCase.Player;
using Hohoema.Presentation.ViewModels;
using LiteDB;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using Prism;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;
using Prism.Unity;
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
using Unity;
using Unity.Injection;
using Unity.Lifetime;
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
using Hohoema.Presentation.Views.Pages;
using Hohoema.Models.UseCase.Niconico.Account;
using Hohoema.Models.UseCase.Niconico.Follow;
using Hohoema.Models.Domain.Notification;
using Hohoema.Models.Domain.VideoCache;
using Windows.Storage.AccessCache;

namespace Hohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : PrismApplication
    {
        const bool _DEBUG_XBOX_RESOURCE = false;

        public SplashScreen SplashScreen { get; private set; }

        private bool _IsPreLaunch;

		public const string ACTIVATION_WITH_ERROR = "error";
        public const string ACTIVATION_WITH_ERROR_OPEN_LOG = "error_open_log";
        public const string ACTIVATION_WITH_ERROR_COPY_LOG = "error_copy_log";

        internal const string IS_COMPLETE_INTRODUCTION = "is_first_launch";

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
            
            Microsoft.Toolkit.Uwp.UI.ImageCache.Instance.CacheDuration = TimeSpan.FromDays(7);
            Microsoft.Toolkit.Uwp.UI.ImageCache.Instance.MaxMemoryCacheCount = 1000;
            Microsoft.Toolkit.Uwp.UI.ImageCache.Instance.RetryCount = 3;
            
            // see@ https://www.typea.info/blog/index.php/2017/08/06/uwp_1/
            try
            {
                var appcenter_settings = new Windows.ApplicationModel.Resources.ResourceLoader(".appcenter");
                var appcenterSecrets = appcenter_settings.GetString("AppCenter_Secrets");

                Crashes.SendingErrorReport += (sender, args) => { Debug.WriteLine(args.Report.ToString()); };
                Crashes.SentErrorReport += (sender, args) => { Debug.WriteLine(args.Report.ToString()); };
                AppCenter.SetUserId(Guid.NewGuid().ToString());
                AppCenter.Start(appcenterSecrets, typeof(Analytics), typeof(Crashes));
                Crashes.NotifyUserConfirmation(UserConfirmation.AlwaysSend);
            }
            catch { }

            this.InitializeComponent();
        }


        public override async Task OnStartAsync(StartArgs args)
        {
            if (args.Arguments is LaunchActivatedEventArgs launchArgs)
            {
                SplashScreen = launchArgs.SplashScreen;
#if DEBUG
                DebugSettings.IsBindingTracingEnabled = true;
#endif
                _IsPreLaunch = launchArgs.PrelaunchActivated;

                Microsoft.Toolkit.Uwp.Helpers.SystemInformation.Instance.TrackAppUse(launchArgs);
            }

            await EnsureInitializeAsync();

            if (args.StartKind == StartKinds.Launch)
            {
                if (!_isNavigationStackRestored)
                {
#if DEBUG
                    var niconicoSession = Container.Resolve<NiconicoSession>();

                    // 外部から起動した場合にサインイン動作と排他的動作にさせたい
                    // こうしないと再生処理を正常に開始できない
                    using (await niconicoSession.SigninLock.LockAsync())
                    {
                        await Task.Delay(50);
                    }
                    _isNavigationStackRestored = true;
                    //                    await _primaryWindowCoreLayout.RestoreNavigationStack();
#else
                    var navigationService = Container.Resolve<PageManager>();
                    var settings = Container.Resolve<AppearanceSettings>();
                    navigationService.OpenPage(settings.FirstAppearPageType);
#endif
                    // TODO: 前回再生中に終了したコンテンツを表示するかユーザーに確認
#if DEBUG
                    var vm = _primaryWindowCoreLayout.DataContext as PrimaryWindowCoreLayoutViewModel;
                    var lastPlaying = vm.RestoreNavigationManager.GetCurrentPlayerEntry();
                    if (lastPlaying != null)
                    {
                        // TODO: 前回再生中コンテンツ復帰時に再生リストを取得できるようにする（ローカルマイリストやチャンネル動画一覧含む）
                        var hohoemaPlaylist = Container.Resolve<HohoemaPlaylist>();
                        hohoemaPlaylist.Play(lastPlaying.ContentId, position: lastPlaying.Position);
                    }
#endif
                }

            }
            else if (args.StartKind == StartKinds.Activate)
            {
                _ = OnActivateApplicationAsync(args.Arguments as IActivatedEventArgs);
            }
            else if (args.StartKind == StartKinds.Background)
            {
                BackgroundActivated(args.Arguments as BackgroundActivatedEventArgs);
            }

            

            await base.OnStartAsync(args);
        }

        UIElement CreateShell()
        {
            
            // Grid
            //   |- HohoemaInAppNotification
            //   |- PlayerWithPageContainerViewModel
            //   |    |- MenuNavigatePageBaseViewModel
            //   |         |- rootFrame 

            _primaryWindowCoreLayout = Container.Resolve<PrimaryWindowCoreLayout>();
            var hohoemaInAppNotification = new Presentation.Views.Controls.HohoemaInAppNotification()
            {
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var grid = new Grid()
            {
                Children =
                {
                    _primaryWindowCoreLayout,
                    hohoemaInAppNotification,
                    new Presentation.Views.NoUIProcessScreen()
                }
            };

            var primaryWindowContentNavigationService = _primaryWindowCoreLayout.CreateNavigationService();
            Container.GetContainer().RegisterInstance(primaryWindowContentNavigationService);

            var primaryViewPlayerNavigationService = _primaryWindowCoreLayout.CreatePlayerNavigationService();
            var name = "PrimaryPlayerNavigationService";
            Container.GetContainer().RegisterInstance(name, primaryViewPlayerNavigationService);



#if DEBUG
            _primaryWindowCoreLayout.FocusEngaged += (__, args) => Debug.WriteLine("focus engagad: " + args.OriginalSource.ToString());
#endif

            _primaryWindowCoreLayout.IsDebugModeEnabled = IsDebugModeEnabled;

            return grid;
        }

        public override void RegisterTypes(IContainerRegistry container)
        {
            var unityContainer = container.GetContainer();

            LiteDatabase db = new LiteDatabase($"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, "hohoema.db")};");
            unityContainer.RegisterInstance<LiteDatabase>(db);

            MonkeyCache.LiteDB.Barrel.ApplicationId = nameof(Hohoema);
            unityContainer.RegisterInstance<MonkeyCache.IBarrel>(MonkeyCache.LiteDB.Barrel.Current);

            var mainWindowsScheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
            // 各ウィンドウごとのスケジューラを作るように
            unityContainer.RegisterType<IScheduler>(new PerThreadLifetimeManager(), new InjectionFactory(c => SynchronizationContext.Current != null ? new SynchronizationContextScheduler(SynchronizationContext.Current) : mainWindowsScheduler));
            unityContainer.RegisterInstance<IScheduler>("MainWindowsScheduler", mainWindowsScheduler);

            // MediaPlayerを各ウィンドウごとに一つずつ作るように
            unityContainer.RegisterType<MediaPlayer>(new PerThreadLifetimeManager());

            unityContainer.RegisterType<LocalObjectStorageHelper>(new InjectionConstructor(new JsonObjectSerializer()));

            // Service
            unityContainer.RegisterSingleton<PageManager>();
            unityContainer.RegisterSingleton<PrimaryViewPlayerManager>();
            unityContainer.RegisterSingleton<ScondaryViewPlayerManager>();
            unityContainer.RegisterSingleton<NiconicoLoginService>();
            unityContainer.RegisterSingleton<DialogService>();
            unityContainer.RegisterSingleton<NoUIProcessScreenContext>();

            // Models
            unityContainer.RegisterSingleton<AppearanceSettings>();
            unityContainer.RegisterSingleton<PinSettings>();
            unityContainer.RegisterSingleton<PlayerSettings>();
            unityContainer.RegisterSingleton<VideoFilteringSettings>();
            unityContainer.RegisterSingleton<VideoRankingSettings>();
            unityContainer.RegisterSingleton<VideoCacheSettings_Legacy>();
            unityContainer.RegisterSingleton<NicoRepoSettings>();
            unityContainer.RegisterSingleton<CommentFliteringRepository>();



            unityContainer.RegisterSingleton<NiconicoSession>();
            unityContainer.RegisterSingleton<NicoVideoSessionOwnershipManager>();
            
            unityContainer.RegisterSingleton<LoginUserOwnedMylistManager>();
            unityContainer.RegisterSingleton<FollowManager>();

            unityContainer.RegisterSingleton<VideoCacheManagerLegacy>();
            unityContainer.RegisterSingleton<SubscriptionManager>();

            unityContainer.RegisterSingleton<Models.Domain.VideoCache.VideoCacheManager>();
            unityContainer.RegisterSingleton<Models.Domain.VideoCache.VideoCacheSettings>();

            unityContainer.RegisterSingleton<ErrorTrackingManager>();

            // UseCase
            unityContainer.RegisterType<VideoPlayer>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<CommentPlayer>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<CommentFilteringFacade>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<MediaPlayerSoundVolumeManager>(new PerThreadLifetimeManager());
            unityContainer.RegisterSingleton<HohoemaPlaylist>();
            unityContainer.RegisterSingleton<LocalMylistManager>();
            unityContainer.RegisterSingleton<VideoItemsSelectionContext>();
            unityContainer.RegisterSingleton<WatchHistoryManager>();
            unityContainer.RegisterSingleton<ApplicationLayoutManager>();
            




            // ViewModels
            unityContainer.RegisterSingleton<Presentation.ViewModels.Pages.Niconico.RankingCategoryListPageViewModel>();

            unityContainer.RegisterType<Presentation.ViewModels.Player.VideoPlayerPageViewModel>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<Presentation.ViewModels.Player.LivePlayerPageViewModel>(new PerThreadLifetimeManager());

#if DEBUG
            //			BackgroundUpdater.MaxTaskSlotCount = 1;
#endif
            // TODO: プレイヤーウィンドウ上で管理する
            //			var backgroundTask = MediaBackgroundTask.Create();
            //			Container.RegisterInstance(backgroundTask);

        }



        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.BlankPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Hohoema.DebugPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Hohoema.SettingsPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Hohoema.Video.CacheManagementPage >();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Hohoema.Video.LocalPlaylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Hohoema.Video.LocalPlaylistManagePage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Hohoema.Video.SubscriptionManagementPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Hohoema.Video.VideoQueuePage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Video.ChannelVideoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.CommunityPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Video.CommunityVideoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Live.LiveInfomationPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.UserMylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Video.MylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.OwnerMylistManagePage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Search.SearchPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Search.SearchResultTagPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Search.SearchResultMylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Search.SearchResultKeywordPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Search.SearchResultCommunityPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Search.SearchResultLivePage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Video.SeriesPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.UserSeriesPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.LoginPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.FollowManagePage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.NicoRepoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.RecommendPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.TimeshiftPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.LoginUser.WatchHistoryPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.UserInfoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Video.UserVideoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.RankingCategoryListPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.Video.RankingCategoryPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Pages.Niconico.VideoInfomationPage>();

            containerRegistry.RegisterForNavigation<Presentation.Views.Player.LivePlayerPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.Player.VideoPlayerPage>();

            base.RegisterRequiredTypes(containerRegistry);
        }

        public bool IsTitleBarCustomized { get; } = DeviceTypeHelper.IsDesktop && InputCapabilityHelper.IsMouseCapable;

        Models.Helpers.AsyncLock InitializeLock = new Models.Helpers.AsyncLock();
        bool isInitialized = false;
        private async Task EnsureInitializeAsync()
        {
            if (isInitialized) { return; }
            isInitialized = true;


            async Task TryMigrationAsync(Type[] migrateTypes)
            {
                foreach (var migrateType in migrateTypes)
                {
                    try
                    {
                        Debug.WriteLine($"Try migrate: {migrateType.Name}");
                        var migrater = Container.Resolve(migrateType);
                        if (migrater is IMigrate migrateSycn)
                        {
                            migrateSycn.Migrate();
                        }
                        else if (migrater is IMigrateAsync migrateAsync)
                        {
                            await migrateAsync.MigrateAsync();
                        }

                        Debug.WriteLine("Migration complete : " + migrateType.Name);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        Debug.WriteLine("Migration failed : " + migrateType.Name);
                    }
                }
            }

            await TryMigrationAsync(new Type[]
            {
                //typeof(MigrationCommentFilteringSettings),
                //typeof(CommentFilteringNGScoreZeroFixture),
                //typeof(SettingsMigration_V_0_23_0),
                //typeof(SearchPageQueryMigrate_0_26_0),
                typeof(LocalMylistThumbnailImageMigration_V_0_28_0),
                typeof(VideoCacheDatabaseMigration_V_0_29_0),
            });

            // 機能切り替え管理クラスをDIコンテナに登録
            // Xaml側で扱いやすくするためApp.xaml上でインスタンス生成させている
            {
                var unityContainer = Container.GetContainer();
                unityContainer.RegisterInstance(Resources["FeatureFlags"] as FeatureFlags);
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

            var appearanceSettings = Container.Resolve<Models.Domain.Application.AppearanceSettings>();
            I18NPortable.I18N.Current.Locale = appearanceSettings.Locale ?? I18NPortable.I18N.Current.Languages.FirstOrDefault(x => x.Locale.StartsWith(CultureInfo.CurrentCulture.TwoLetterISOLanguageName)).Locale ?? I18NPortable.I18N.Current.Locale;

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(I18NPortable.I18N.Current.Locale);

            //Console.WriteLine(settings.AppearanceSettings.Locale);
            //Console.WriteLine(I18N.Current.Locale);
            //Console.WriteLine(CultureInfo.CurrentCulture.Name);

            // ログイン前にログインセッションによって状態が変化するフォローとマイリストの初期化
            var mylitManager = Container.Resolve<LoginUserOwnedMylistManager>();
            var followManager = Container.Resolve<FollowManager>();

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
#else
            Resources["IsDebug"] = false;
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
                if (localObjectStorageHelper.KeyExists(ScondaryViewPlayerManager.primary_view_size))
                {
                    var view = ApplicationView.GetForCurrentView();
                    MainViewId = view.Id;
                    _PrevWindowSize = localObjectStorageHelper.Read<Size>(ScondaryViewPlayerManager.primary_view_size);
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
                const string folderName = "Hohoema_Videos";
                var cacheManager = Container.Resolve<VideoCacheManager>();

                // キャッシュフォルダの指定
                if (StorageApplicationPermissions.FutureAccessList.ContainsItem(folderName))
                {
                    cacheManager.VideoCacheFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderName);
                }
                else
                {
                    var folder = await DownloadsFolder.CreateFolderAsync(folderName);
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(folderName, folder);
                    cacheManager.VideoCacheFolder = folder;
                }

                // キャッシュの暗号化を初期化
                var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/VideoCache_EncryptionKey_32byte.txt"));
                var bytes = await file.ReadBytesAsync();
                cacheManager.SetXts(XTSSharp.XtsAes128.Create(bytes.Take(32).ToArray()));
                VideoCacheManager.ResolveVideoFileNameWithoutExtFromVideoId = async (id) => 
                {
                    var repo = Container.Resolve<NicoVideoProvider>();
                    var item = await repo.GetNicoVideoInfo(id);
                    return $"{item.Title.ToSafeFilePath()}[{id}]";
                };
            }




            // 2段階認証を処理するログインサービスをインスタンス化
            var loginService = Container.Resolve<NiconicoLoginService>();


            // バージョン間データ統合
            {
                var unityContainer = Container.GetContainer();

                unityContainer.Resolve<Models.UseCase.Migration.CommentFilteringNGScoreZeroFixture>().Migration();


                // アプリのユースケース系サービスを配置
                unityContainer.RegisterInstance(unityContainer.Resolve<NotificationCacheVideoDeletedService>());
                unityContainer.RegisterInstance(unityContainer.Resolve<CheckingClipboardAndNotificationService>());
                unityContainer.RegisterInstance(unityContainer.Resolve<NotificationFollowUpdatedService>());
                unityContainer.RegisterInstance(unityContainer.Resolve<NotificationCacheRequestRejectedService>());
                unityContainer.RegisterInstance(unityContainer.Resolve<SubscriptionUpdateManager>());
                unityContainer.RegisterInstance(unityContainer.Resolve<FeedResultAddToWatchLater>());
                unityContainer.RegisterInstance(unityContainer.Resolve<SyncWatchHistoryOnLoggedIn>());
                unityContainer.RegisterInstance(unityContainer.Resolve<LatestSubscriptionVideosNotifier>());

                unityContainer.RegisterInstance(unityContainer.Resolve<VideoCacheResumingObserver>());
                unityContainer.RegisterInstance(unityContainer.Resolve<VideoPlayRequestBridgeToPlayer>());
                unityContainer.RegisterInstance(unityContainer.Resolve<CloseToastNotificationWhenPlayStarted>());

                unityContainer.RegisterInstance(unityContainer.Resolve<VideoCacheDownloadOperationManager>());
            }

            // バックグラウンドでのトースト通知ハンドリングを初期化
            await RegisterDebugToastNotificationBackgroundHandling();


            // 更新通知を表示
            try
            {
                var dialogService = Container.Resolve<DialogService>();
                if (AppUpdateNotice.IsUpdated)
                {
                    var version = Windows.ApplicationModel.Package.Current.Id.Version;
                    var notificationService = Container.Resolve<NotificationService>();
                    notificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = $"Hohoema v{version.Major}.{version.Minor}.{version.Build} に更新しました",
                        ShowDuration = TimeSpan.FromSeconds(7),
                        IsShowDismissButton = true,
                        Commands =
                            {
                                new InAppNotificationCommand()
                                {
                                    Command = new DelegateCommand(async () =>
                                    {
                                        await AppUpdateNotice.ShowReleaseNotePageOnBrowserAsync();
                                    }),
                                    Label = "更新情報を確認（ブラウザで表示）"
                                }
                            }
                    });
                    AppUpdateNotice.UpdateLastCheckedVersionInCurrentVersion();
                }
            }
            catch { }


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

        

        private async Task OnActivateApplicationAsync(IActivatedEventArgs args)
		{
            var niconicoSession = Container.Resolve<NiconicoSession>();

            // 外部から起動した場合にサインイン動作と排他的動作にさせたい
            // こうしないと再生処理を正常に開始できない
            using (await niconicoSession.SigninLock.LockAsync())
            {
                await Task.Delay(50);
            }



            if (args.Kind == ActivationKind.ToastNotification)
            {
                bool isHandled = false;

                //Get the pre-defined arguments and user inputs from the eventargs;
                var toastArgs = args as IActivatedEventArgs as ToastNotificationActivatedEventArgs;
                var arguments = toastArgs.Argument;
                try
                {
                    var nicoContentId = NicoVideoIdHelper.UrlToVideoId(arguments);

                    if (nicoContentId != null)
                    {
                        if (Mntone.Nico2.NiconicoRegex.IsVideoId(nicoContentId))
                        {
                            PlayVideoFromExternal(nicoContentId);
                            isHandled = true;
                        }
                        else if (Mntone.Nico2.NiconicoRegex.IsLiveId(nicoContentId))
                        {
                            PlayLiveVideoFromExternal(nicoContentId);
                            isHandled = true;
                        }
                    }

                    var payload = JsonConvert.DeserializeObject<LoginRedirectPayload>(arguments);
                    if (payload != null)
                    {
                        if (payload.RedirectPageType == HohoemaPageType.VideoPlayer)
                        {
                            var parameter = new NavigationParameters(payload.RedirectParamter);
                            var playlistId = parameter.GetValue<string>("playlist_id");
                            if (parameter.TryGetValue("id", out string id))
                            {
                                PlayVideoFromExternal(id, playlistId);
                                isHandled = true;
                            }
                            else
                            {
                                var playlistResolver = App.Current.Container.Resolve<PlaylistResolver>();
                                var hohoemaPlaylist = App.Current.Container.Resolve<HohoemaPlaylist>();
                                var playlist = await playlistResolver.ResolvePlaylistAsync(Models.Domain.Playlist.PlaylistOrigin.Mylist, playlistId);
                                hohoemaPlaylist.Play(playlist);
                                isHandled = true;
                            }
                        }
                        else
                        {
                            var pageManager = Container.Resolve<PageManager>();
                            pageManager.OpenPage(payload.RedirectPageType, payload.RedirectParamter);
                            isHandled = true;
                            _isNavigationStackRestored = true;
                        }
                    }
                    
                    if (Uri.TryCreate(arguments, UriKind.Absolute, out var uri))
                    {
                        var pageManager = Container.Resolve<PageManager>();
                        if (pageManager.OpenPage(uri))
                        {
                            isHandled = true;
                            _isNavigationStackRestored = true;
                        }
                    }
                }
                catch { }
                
                if (!isHandled)
                {
                    if (arguments.StartsWith("cache_cancel"))
                    {
                        var query = arguments.Split('?')[1];
                        var decode = new WwwFormUrlDecoder(query);

                        var videoId = decode.GetFirstValueByName("id");
                        var quality = (NicoVideoQuality)Enum.Parse(typeof(NicoVideoQuality), decode.GetFirstValueByName("quality"));

                        var cacheManager = Container.Resolve<VideoCacheManagerLegacy>();
                        await cacheManager.CancelCacheRequest(videoId);
                    }
                    else
                    {
                        var nicoContentId = NicoVideoIdHelper.UrlToVideoId(arguments);

                        if (Mntone.Nico2.NiconicoRegex.IsVideoId(nicoContentId))
                        {
                            PlayVideoFromExternal(nicoContentId);
                        }
                        else if (Mntone.Nico2.NiconicoRegex.IsLiveId(nicoContentId))
                        {
                            PlayLiveVideoFromExternal(nicoContentId);
                        }
                    }
                }

                
            }
            else if (args.Kind == ActivationKind.Protocol)
            {
                var param = (args as IActivatedEventArgs) as ProtocolActivatedEventArgs;
                var uri = param.Uri;
                var maybeNicoContentId = new string(uri.OriginalString.Skip("niconico://".Length).TakeWhile(x => x != '?' && x != '/').ToArray());

                if (Mntone.Nico2.NiconicoRegex.IsVideoId(maybeNicoContentId)
                    || maybeNicoContentId.All(x => x >= '0' && x <= '9'))
                {
                    PlayVideoFromExternal(maybeNicoContentId);
                }
                else if (Mntone.Nico2.NiconicoRegex.IsLiveId(maybeNicoContentId))
                {
                    PlayLiveVideoFromExternal(maybeNicoContentId);
                }
            }

            
        }


        bool _isNavigationStackRestored = false;



        public override void OnInitialized()
        {
            Window.Current.Activate();

            // ログイン
            try
            {
                var niconicoSession = Container.Resolve<NiconicoSession>();
                if (AccountManager.HasPrimaryAccount())
                {
                    // サインイン処理の待ちを初期化内でしないことで初期画面表示を早める
                    _ = niconicoSession.SignInWithPrimaryAccount();
                }
            }
            catch
            {
                Debug.WriteLine("ログイン処理に失敗");
            }

            base.OnInitialized();
        }



        async void BackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var deferral = args.TaskInstance.GetDeferral();

            switch (args.TaskInstance.Task.Name)
            {
                case "ToastBackgroundTask":
                    var details = args.TaskInstance.TriggerDetails as Windows.UI.Notifications.ToastNotificationActionTriggerDetail;
                    if (details != null)
                    {
                        string arguments = details.Argument;
                        var userInput = details.UserInput;

                        await ProcessToastNotificationActivation(arguments, userInput);
                    }
                    break;
            }

            deferral.Complete();


            base.OnBackgroundActivated(args);
        }



        private async void PlayVideoFromExternal(string videoId, string playlistId = null)
        {
            var hohoemaPlaylist = Container.Resolve<HohoemaPlaylist>();

            var nicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            var videoInfo = await nicoVideoProvider.GetNicoVideoInfo(videoId);
            
            if (videoInfo == null || videoInfo.IsDeleted) { return; }

            if (playlistId != null)
            {
                var playlistResolver = App.Current.Container.Resolve<PlaylistResolver>();
                var playlist = await playlistResolver.ResolvePlaylistAsync(Models.Domain.Playlist.PlaylistOrigin.Mylist, playlistId);

                if (playlist != null)
                {
                    hohoemaPlaylist.Play(videoInfo, playlist);
                    return;
                }
            }

            hohoemaPlaylist.Play(videoInfo);
        }
        private void PlayLiveVideoFromExternal(string videoId)
        {
            StrongReferenceMessenger.Default.Send(new PlayerPlayLiveRequestMessage(new() { LiveId = videoId }));
        }


		


#region Page and Application Appiarance

        readonly static Regex ViewToViewModelNameReplaceRegex = new Regex("[^.]+$");
        public override void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver(viewType => 
            {
                var pageToken = viewType.Name;

                if (pageToken.EndsWith("_TV"))
                {
                    pageToken = pageToken.Remove(pageToken.IndexOf("_TV"));
                }
                else if (pageToken.EndsWith("_Mobile"))
                {
                    pageToken = pageToken.Remove(pageToken.IndexOf("_Mobile"));
                }

                var viewModelFullName = ViewToViewModelNameReplaceRegex.Replace(viewType.FullName, $"{pageToken}ViewModel")
                    .Replace(".Views.", ".ViewModels.");

//                var viewModelFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
                var viewModelType = Type.GetType(viewModelFullName);

                if (viewModelType == null)
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.InvariantCulture, pageToken, this.GetType().Namespace + ".ViewModels"),
                        "pageToken");
                }

                return viewModelType;

            });

            base.ConfigureViewModelLocator();
        }




        private Type GetPageType(string pageToken)
        {
            var layoutManager= Container.Resolve<ApplicationLayoutManager>();
            
            Type viewType = null;
            if (layoutManager.AppLayout == ApplicationLayout.TV)
            {
                // pageTokenに対応するXbox表示用のページの型を取得
                try
                {
                    var assemblyQualifiedAppType = this.GetType().AssemblyQualifiedName;

                    var pageNameWithParameter = assemblyQualifiedAppType.Replace(this.GetType().FullName, this.GetType().Namespace + ".Views.{0}Page_TV");

                    var viewFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
                    viewType = Type.GetType(viewFullName);
                }
                catch { }
            }
            else if (layoutManager.AppLayout == ApplicationLayout.Mobile)
            {
                try
                {
                    var assemblyQualifiedAppType = this.GetType().AssemblyQualifiedName;

                    var pageNameWithParameter = assemblyQualifiedAppType.Replace(this.GetType().FullName, this.GetType().Namespace + ".Views.{0}Page_Mobile");

                    var viewFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
                    viewType = Type.GetType(viewFullName);
                }
                catch { }
            }

            return viewType;// ?? base.GetPageType(pageToken);
        }


        
#endregion


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
                    _PrevWindowSize = localObjectStorageHelper.Read<Size>(ScondaryViewPlayerManager.primary_view_size);
                    localObjectStorageHelper.Save(ScondaryViewPlayerManager.primary_view_size, new Size(sender.VisibleBounds.Width, sender.VisibleBounds.Height));

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
                        localObjectStorageHelper.Save(ScondaryViewPlayerManager.primary_view_size, _PrevWindowSize);
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

        bool isFirstCrashe = true;

        private void PrismUnityApplication_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            Debug.Write(e.Message);

            if (e.Exception is OperationCanceledException)
            {
                e.Handled = true;
                return;
            }

            if (e.Exception is ObjectDisposedException)
            {
                e.Handled = true;
                return;
            }

            if (!isFirstCrashe)
            {
                e.Handled = true;
                return;
            }

            isFirstCrashe = false;

            if (IsDebugModeEnabled)
            {
                bool isSentError = false;
                var scheduler = Container.Resolve<IScheduler>("MainWindowsScheduler");
                scheduler.Schedule(() =>
                {
                    var sentErrorCommand = new DelegateCommand(async () =>
                    {
                        try
                        {
                            var errorTrankingManager = Container.Resolve<ErrorTrackingManager>();
                            var dialogService = Container.Resolve<DialogService>();
                            var rtb = await GetApplicationContentImage();
                            var result = await Presentation.Views.Dialogs.HohoemaErrorReportDialog.ShowAsync(e.Exception, sendScreenshot: false, rtb);

                            if (result.IsSendRequested is false) { return; }

                            var attachmentLogs = new List<ErrorAttachmentLog>();
                            if (!string.IsNullOrEmpty(result.Data.InputText))
                            {
                                attachmentLogs.Add(ErrorTrackingManager.CreateTextAttachmentLog(result.Data.InputText));

                                Debug.WriteLine("ReportLatestError: Add UserInputText Attachment");
                            }

                            if (result.Data.UseScreenshot)
                            {
                                try
                                {
                                    attachmentLogs.Add(await ErrorTrackingManager.CreateScreenshotAttachmentLog(rtb));
                                    Debug.WriteLine("ReportLatestError: Add Screenshot Attachment");
                                }
                                catch
                                {
                                    Debug.WriteLine("エラー報告用のスクショ生成に失敗");
                                }
                            }

                            errorTrankingManager.SendReportWithAttatchments(e.Exception, attachmentLogs.ToArray());

                            // isSentError を変更した後にErrorTeachingTipを閉じることでClose時の判定が走る
                            isSentError = true;
                        }
                        catch { }
                        finally
                        {
                            _primaryWindowCoreLayout.CloseErrorTeachingTip();
                        }
                    });

                    _primaryWindowCoreLayout.OpenErrorTeachingTip(sentErrorCommand, () => 
                    {
                        if (isSentError is false)
                        {
                            var errorTrankingManager = Container.Resolve<ErrorTrackingManager>();
                            errorTrankingManager.SendReportWithAttatchments(e.Exception);
                        }
                    });                    
                });
            }
            else
            {
                var errorTrankingManager = Container.Resolve<ErrorTrackingManager>();
                errorTrankingManager.SendReportWithAttatchments(e.Exception);
            }

            e.Handled = true;
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

        private async Task ProcessToastNotificationActivation(string arguments, ValueSet userInput)
        {
            if (arguments.StartsWith("cache_cancel"))
            {
                var cacheManager = Container.Resolve<VideoCacheManagerLegacy>();

                var query = arguments.Split('?')[1];
                var decode = new WwwFormUrlDecoder(query);

                var videoId = decode.GetFirstValueByName("id");
                var quality = (NicoVideoQuality)Enum.Parse(typeof(NicoVideoQuality), decode.GetFirstValueByName("quality"));

                await cacheManager.CancelCacheRequest(videoId);
            }
        }


#endregion

    }





}

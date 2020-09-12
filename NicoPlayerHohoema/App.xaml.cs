using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Unity;
using Hohoema.Models.Domain;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Storage;
using Windows.UI;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.Background;
using System.Reactive.Concurrency;
using System.Threading;
using System.Reactive.Linq;
using Unity.Lifetime;
using Unity.Injection;
using Prism.Unity;
using Prism.Ioc;
using Prism;
using Prism.Navigation;
using Prism.Services;
using Windows.Media.Playback;
using Windows.UI.Xaml.Data;
using Prism.Events;
using Hohoema.Models.UseCase;
using I18NPortable;
using Newtonsoft.Json;
using Hohoema.Models.UseCase.Playlist;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Hohoema.Presentation.ViewModels;
using LiteDB;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Domain.Helpers;

using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.UserFeature.Follow;
using Hohoema.Models.Domain.Player.Video;
using Hohoema.Models.UseCase.NicoVideoPlayer;
using Hohoema.Models.UseCase.Subscriptions;
using Hohoema.Presentation.Services.Page;
using Hohoema.Presentation.Services.Player;
using Hohoema.Presentation.Services;
using Hohoema.Models.Domain.Player.Video.Cache;
using Hohoema.Presentation.Services.Helpers;
using Hohoema.Presentation.Services.Notification;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase.Migration;
using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Niconico.UserFeature;
using LiteDB.Engine;
using Prism.Commands;

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
            
            AnimationSet.UseComposition = true;

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

                Microsoft.Toolkit.Uwp.Helpers.SystemInformation.TrackAppUse(launchArgs);
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
                    await _primaryWindowCoreLayout.RestoreNavigationStack();
#else
                    var navigationService = Container.Resolve<PageManager>();
                    var settings = Container.Resolve<AppearanceSettings>();
                    navigationService.OpenPage(settings.FirstAppearPageType);
#endif
                    // TODO: 前回再生中に終了したコンテンツを表示するかユーザーに確認
                    /*
                    var vm = _primaryWindowCoreLayout.DataContext as PrimaryWindowCoreLayoutViewModel;
                    var lastPlaying = vm.RestoreNavigationManager.GetCurrentPlayerEntry();
                    if (lastPlaying != null)
                    {
                        var playlistAggregateGetter = Container.Resolve<PlaylistAggregateGetter>();
                        var hohoemaPlaylist = Container.Resolve<HohoemaPlaylist>();
                        if (lastPlaying.PlaylistId != null)
                        {
                            var playlist = await playlistAggregateGetter.FindPlaylistAsync(lastPlaying.PlaylistId);
                            hohoemaPlaylist.Play(lastPlaying.ContentId, playlist, position: lastPlaying.Position);
                        }
                        else
                        {
                            hohoemaPlaylist.Play(lastPlaying.ContentId, position: lastPlaying.Position);
                        }
                    }
                    */
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

            _primaryWindowCoreLayout = Container.Resolve<Presentation.Views.PrimaryWindowCoreLayout>();
            var hohoemaInAppNotification = new Presentation.Views.HohoemaInAppNotification()
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

            return grid;
        }

        public override void RegisterTypes(IContainerRegistry container)
        {
            var unityContainer = container.GetContainer();

            MonkeyCache.LiteDB.Barrel.ApplicationId = nameof(Hohoema);
            unityContainer.RegisterInstance<MonkeyCache.IBarrel>(MonkeyCache.LiteDB.Barrel.Current);

            // 各ウィンドウごとのスケジューラを作るように
            unityContainer.RegisterType<IScheduler>(new PerThreadLifetimeManager(), new InjectionFactory(c => SynchronizationContext.Current != null ? new SynchronizationContextScheduler(SynchronizationContext.Current) : null));

            // MediaPlayerを各ウィンドウごとに一つずつ作るように
            unityContainer.RegisterType<MediaPlayer>(new PerThreadLifetimeManager());
            
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
            unityContainer.RegisterSingleton<VideoCacheSettings>();
            unityContainer.RegisterSingleton<NicoRepoSettings>();



            unityContainer.RegisterSingleton<NiconicoSession>();
            unityContainer.RegisterSingleton<NicoVideoSessionOwnershipManager>();
            
            unityContainer.RegisterSingleton<UserMylistManager>();
            unityContainer.RegisterSingleton<FollowManager>();

            unityContainer.RegisterSingleton<VideoCacheManager>();
            unityContainer.RegisterSingleton<SubscriptionManager>();



            // UseCase
            unityContainer.RegisterType<VideoPlayer>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<CommentPlayer>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<CommentFiltering>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<MediaPlayerSoundVolumeManager>(new PerThreadLifetimeManager());
            unityContainer.RegisterSingleton<HohoemaPlaylist>();
            unityContainer.RegisterSingleton<LocalMylistManager>();
            unityContainer.RegisterSingleton<VideoItemsSelectionContext>();
            unityContainer.RegisterSingleton<WatchHistoryManager>();
            unityContainer.RegisterSingleton<ApplicationLayoutManager>();
            



            // ViewModels
            unityContainer.RegisterSingleton<RankingCategoryListPageViewModel>();

            unityContainer.RegisterType<VideoPlayerPageViewModel>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<LivePlayerPageViewModel>(new PerThreadLifetimeManager());

#if DEBUG
            //			BackgroundUpdater.MaxTaskSlotCount = 1;
#endif
            // TODO: プレイヤーウィンドウ上で管理する
            //			var backgroundTask = MediaBackgroundTask.Create();
            //			Container.RegisterInstance(backgroundTask);

        }



        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Presentation.Views.BlankPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.CacheManagementPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.ChannelVideoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.CommunityPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.CommunityVideoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.DebugPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.FollowManagePage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.LiveInfomationPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.LoginPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.MylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.LocalPlaylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.NicoRepoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.RankingCategoryListPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.RankingCategoryPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.RecommendPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SearchPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SearchResultTagPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SearchResultMylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SearchResultKeywordPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SearchResultCommunityPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SearchResultLivePage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SettingsPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.TimeshiftPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.UserInfoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.UserMylistPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.UserVideoPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.VideoInfomationPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.WatchHistoryPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SeriesPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.UserSeriesPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.WatchAfterPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.SubscriptionManagementPage>();            

            containerRegistry.RegisterForNavigation<Presentation.Views.LivePlayerPage>();
            containerRegistry.RegisterForNavigation<Presentation.Views.VideoPlayerPage>();

            base.RegisterRequiredTypes(containerRegistry);
        }

        public bool IsTitleBarCustomized { get; } = DeviceTypeHelper.IsDesktop && InputCapabilityHelper.IsMouseCapable;

        Models.Domain.Helpers.AsyncLock InitializeLock = new Models.Domain.Helpers.AsyncLock();
        bool isInitialized = false;
        private async Task EnsureInitializeAsync()
        {
            using (await InitializeLock.LockAsync())
            {
                if (isInitialized) { return; }
                isInitialized = true;


                Type[] migrateTypes = new Type[]
                {
                };

                
                foreach (var migrateType in migrateTypes)
                {
                    Debug.WriteLine($"try migrate {migrateType.Name}");
                    var migrater = Container.Resolve(migrateType);
                    if (migrater is IMigrate migrateSycn)
                    {
                        migrateSycn.Migrate();
                    }
                    else if (migrater is IMigrateAsync migrateAsync)
                    {
                        await migrateAsync.MigrateAsync();
                    }
                }

                {
                    var unityContainer = Container.GetContainer();
                    var upgradeResult = LiteEngine.Upgrade(Path.Combine(ApplicationData.Current.LocalFolder.Path, "hohoema.db"));
                    Debug.WriteLine("upgrade: " + upgradeResult);

                    LiteDatabase db = new LiteDatabase($"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, "hohoema.db")};");
                    unityContainer.RegisterInstance<ILiteDatabase>(db);
                }
                
                Container.Resolve<MigrationCommentFilteringSettings>().Migration();
                Container.Resolve<CommentFilteringNGScoreZeroFixture>().Migration();
                await Task.Run(async () => { await Container.Resolve<SettingsMigration_V_0_23_0>().MigrateAsync(); });
                



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
                var mylitManager = Container.Resolve<UserMylistManager>();
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
                var cacheSettings = Container.Resolve<VideoCacheSettings>();
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





                // 2段階認証を処理するログインサービスをインスタンス化
                var loginService = Container.Resolve<NiconicoLoginService>();


                // バージョン間データ統合
                {
                    var unityContainer = Container.GetContainer();

                    unityContainer.Resolve<Models.UseCase.Migration.CommentFilteringNGScoreZeroFixture>().Migration();


                    // アプリのユースケース系サービスを配置
                    unityContainer.RegisterInstance(unityContainer.Resolve<NotificationCacheVideoDeletedService>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<NotificationMylistUpdatedService>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<CheckingClipboardAndNotificationService>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<NotificationFollowUpdatedService>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<NotificationCacheRequestRejectedService>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<SubscriptionUpdateManager>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<FeedResultAddToWatchLater>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<SyncWatchHistoryOnLoggedIn>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<LatestSubscriptionVideosNotifier>());

                    unityContainer.RegisterInstance(unityContainer.Resolve<VideoCacheResumingObserver>());
                    unityContainer.RegisterInstance(unityContainer.Resolve<VideoPlayRequestBridgeToPlayer>());
                }

                // バックグラウンドでのトースト通知ハンドリングを初期化
                await RegisterDebugToastNotificationBackgroundHandling();


                try
                {
                    var cacheManager = Container.Resolve<VideoCacheManager>();
                    _ = cacheManager.Initialize();
                }
                catch { }







                // 更新通知を表示
                try
                {
                    var dialogService = Container.Resolve<DialogService>();
                    if (AppUpdateNotice.IsMinorVersionUpdated)
                    {
                        _ = dialogService.ShowLatestUpdateNotice();
                        AppUpdateNotice.UpdateLastCheckedVersionInCurrentVersion();
                    }
                    else if (AppUpdateNotice.IsUpdated)
                    {
                        var version = Windows.ApplicationModel.Package.Current.Id.Version;
                        var notificationService = Container.Resolve<NotificationService>();
                        notificationService.ShowInAppNotification(new InAppNotificationPayload()
                        {
                            Content = $"Hohoema v{version.Major}.{version.Minor}.{version.Revision} に更新しました",
                            ShowDuration = TimeSpan.FromSeconds(7),
                            IsShowDismissButton = true,
                            SymbolIcon = Symbol.Refresh,
                            Commands =
                            {
                                new InAppNotificationCommand()
                                {
                                    Command = new DelegateCommand(() =>
                                    {
                                        _ = dialogService.ShowLatestUpdateNotice();
                                    }),
                                    Label = "更新情報を確認"
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
                    if (!Services.Helpers.ApiContractHelper.Is2018FallUpdateAvailable)
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
                                var playlistResolver = App.Current.Container.Resolve<PlaylistAggregateGetter>();
                                var hohoemaPlaylist = App.Current.Container.Resolve<HohoemaPlaylist>();
                                var playlist = await playlistResolver.FindPlaylistAsync(playlistId);
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
                    if (arguments == ACTIVATION_WITH_ERROR)
                    {
                        await ShowErrorLog().ConfigureAwait(false);
                    }
                    else if (arguments == ACTIVATION_WITH_ERROR_COPY_LOG)
                    {
                        var error = await GetMostRecentErrorText();

                        ClipboardHelper.CopyToClipboard(error);
                    }
                    else if (arguments == ACTIVATION_WITH_ERROR_OPEN_LOG)
                    {
                        await ShowErrorLogFolder();
                    }
                    else if (arguments.StartsWith("cache_cancel"))
                    {
                        var query = arguments.Split('?')[1];
                        var decode = new WwwFormUrlDecoder(query);

                        var videoId = decode.GetFirstValueByName("id");
                        var quality = (NicoVideoQuality)Enum.Parse(typeof(NicoVideoQuality), decode.GetFirstValueByName("quality"));

                        var cacheManager = Container.Resolve<VideoCacheManager>();
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

            // TODO: ログインが必要な動画かをチェックしてログインダイアログを出す

            // EventAggregator経由で動画IDの再生リクエストを送って
            // アプリケーションユースケースで動画情報を解決して再生開始するほうが良さそう

            var nicoVideoProvider = App.Current.Container.Resolve<NicoVideoProvider>();
            var videoInfo = await nicoVideoProvider.GetNicoVideoInfo(videoId);
            
            if (videoInfo == null || videoInfo.IsDeleted) { return; }

            if (playlistId != null)
            {
                var playlistAggregator = App.Current.Container.Resolve<PlaylistAggregateGetter>();
                var playlist = await playlistAggregator.FindPlaylistAsync(playlistId);

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
            // TODO: ログインが必要な生放送かをチェックしてログインダイアログを出す
            
            var ea = Container.Resolve<IEventAggregator>();
            ea.GetEvent<PlayerPlayLiveRequest>()
                .Publish(new PlayerPlayLiveRequestEventArgs() { LiveId = videoId });
        }


		


#region Page and Application Appiarance

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

                var assemblyQualifiedAppType = viewType.AssemblyQualifiedName;

                var pageNameWithParameter = assemblyQualifiedAppType.Replace(viewType.FullName, "Hohoema.Presentation.ViewModels.{0}ViewModel");

                var viewModelFullName = string.Format(CultureInfo.InvariantCulture, pageNameWithParameter, pageToken);
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
        private Presentation.Views.PrimaryWindowCoreLayout _primaryWindowCoreLayout;

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
            }
        }

        public async Task<string> GetMostRecentErrorText()
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("error", CreationCollisionOption.OpenIfExists);
            var errorFiles = await folder.GetItemsAsync();

            var errorFile = errorFiles
                .OrderBy(x => x.DateCreated)
                .LastOrDefault()
                as StorageFile;

            if (errorFile != null)
            {
                return await FileIO.ReadTextAsync(errorFile);
            }
            else
            {
                return null;
            }
        }

        private async Task ShowErrorLog()
        {
            var text = await GetMostRecentErrorText();

            if (text != null)
            {
                var contentDialog = new ContentDialog();
                contentDialog.Title = "Hohoemaで発生したエラー詳細";
                contentDialog.PrimaryButtonText = "OK";
                contentDialog.Content = new TextBox()
                {
                    Text = text,
                    IsReadOnly = true,
                    TextWrapping = TextWrapping.Wrap,
                };

                await contentDialog.ShowAsync().AsTask();
            }
        }

        public async Task ShowErrorLogFolder()
        {
            var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("error");
            if (folder != null)
            {
                await Windows.System.Launcher.LaunchFolderAsync(folder);
            }
        }

        private async void PrismUnityApplication_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            Debug.Write(e.Message);

            if (IsDebugModeEnabled)
            {
                await OutputErrorFile(e.Exception);

                ShowErrorToast(e.Message);
            }
        }


        struct ErrorReport
        {
            public string DeviceType { get; set; }
            public string OperatingSystem { get; set; }
            public string OperatingSystemVersion { get; set; }
            public string OperatingSystemArchitecture { get; set; }

            public string ApplicationVersion { get; set; }
            public DateTime Time { get; set; }
            public string RecentOpenedPageName { get; set; }
            public string ErrorMessage { get; set; }

            public bool IsInternetAvailable { get; set; }
            public bool IsLoggedIn { get; set; }
            public bool IsPremiumAccount { get; set; }

        }

        public async Task OutputErrorFile(Exception e, string pageName = null)
        {
            if (pageName == null)
            {
                var pageManager = Container.Resolve<PageManager>();
                pageName = pageManager.CurrentPageType.ToString();
            }

            var niconicoSession = Container.Resolve<NiconicoSession>();


            try
            {
                var v = Package.Current.Id.Version;
                var versionText = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("error", CreationCollisionOption.OpenIfExists);
                var errorFile = await folder.CreateFileAsync($"Hohoema-{versionText.Replace('.', '_')}-{Path.GetFileNameWithoutExtension(Path.GetRandomFileName())}.txt", CreationCollisionOption.OpenIfExists);

                var errorReport = new ErrorReport()
                {
                    ApplicationVersion = versionText,
                    Time = DateTime.Now,
                    RecentOpenedPageName = pageName,
                    IsInternetAvailable = InternetConnection.IsInternet(),
                    IsLoggedIn = niconicoSession.IsLoggedIn,
                    IsPremiumAccount = niconicoSession.IsPremiumAccount,
                    ErrorMessage = e.ToString(),
                    DeviceType = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.DeviceFamily,
                    OperatingSystem = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.OperatingSystem,
                    OperatingSystemArchitecture = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.OperatingSystemArchitecture.ToString(),
                    OperatingSystemVersion = Microsoft.Toolkit.Uwp.Helpers.SystemInformation.OperatingSystemVersion.ToString(),
                };

                var errorReportJsonText = Newtonsoft.Json.JsonConvert.SerializeObject(
                    errorReport, 
                    Newtonsoft.Json.Formatting.Indented, 
                    new Newtonsoft.Json.JsonSerializerSettings()
                    {
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None,
                    });

                await FileIO.WriteTextAsync(errorFile, errorReportJsonText);
            }
            catch { }
        }

        public void ShowErrorToast(string message)
        {
            var toast = Container.Resolve<NotificationService>();
            toast.ShowToast("ToastNotification_ExceptionHandled".Translate()
                , message
                , Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
                , luanchContent: ACTIVATION_WITH_ERROR
                ,  toastButtons: new[] 
                {
                    new ToastButton("OpenErrorLog".Translate(), ACTIVATION_WITH_ERROR_COPY_LOG) { ActivationType = ToastActivationType.Background },
                    new ToastButton("OpenErrorLogFolder".Translate(), ACTIVATION_WITH_ERROR_OPEN_LOG) { ActivationType = ToastActivationType.Background },
                }
                );
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
            
            // Perform tasks
            if (arguments == ACTIVATION_WITH_ERROR)
            {
                await ShowErrorLog().ConfigureAwait(false);
            }
            else if (arguments == ACTIVATION_WITH_ERROR_COPY_LOG)
            {
                var error = await GetMostRecentErrorText();
                ClipboardHelper.CopyToClipboard(error);
            }
            else if (arguments == ACTIVATION_WITH_ERROR_OPEN_LOG)
            {
                await ShowErrorLogFolder();
            }
            else if (arguments.StartsWith("cache_cancel"))
            {
                var cacheManager = Container.Resolve<VideoCacheManager>();

                var query = arguments.Split('?')[1];
                var decode = new WwwFormUrlDecoder(query);

                var videoId = decode.GetFirstValueByName("id");
                var quality = (NicoVideoQuality)Enum.Parse(typeof(NicoVideoQuality), decode.GetFirstValueByName("quality"));

                await cacheManager.CancelCacheRequest(videoId);
            }
        }


        /*
        private async void UINavigationManager_Pressed(Views.UINavigationManager sender, Views.UINavigationButtons buttons)
        {
            await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (buttons == Views.UINavigationButtons.Up ||
                buttons == Views.UINavigationButtons.Down ||
                buttons == Views.UINavigationButtons.Right ||
                buttons == Views.UINavigationButtons.Left
                )
                {
                    var focused = FocusManager.GetFocusedElement();
                    Debug.WriteLine("現在のフォーカス:" + focused?.ToString());
                }
            });
        }
        */



#endregion

    }





}

﻿using Prism.Mvvm;
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
using Hohoema.Models;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.Storage;
using Windows.UI;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.ApplicationModel.Background;
using System.Reactive.Concurrency;
using System.Threading;
using Hohoema.Services;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using Unity.Lifetime;
using Unity.Injection;
using Prism.Unity;
using Prism.Ioc;
using Prism;
using Prism.Navigation;
using Prism.Services;
using Hohoema.Models.LocalMylist;
using Windows.Media.Playback;
using Windows.UI.Xaml.Data;
using Prism.Events;
using Hohoema.Services.Player;
using Hohoema.UseCase;
using I18NPortable;
using Newtonsoft.Json;
using Hohoema.UseCase.Playlist;
using Microsoft.Toolkit.Uwp.UI.Animations;
using Hohoema.Models.Niconico;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Helpers;
using Hohoema.Models.Repository.VideoCache;
using Hohoema.Models.Repository.NicoRepo;
using Hohoema.Models.Repository.Playlist;
using Hohoema.Models.Repository.Niconico.NicoVideo.Ranking;
using Hohoema.Models.Repository.Subscriptions;
using Hohoema.Models.Pages;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Pages.PagePayload;
using Hohoema.ViewModels.Pages;
using Hohoema.Database;
using Hohoema.Models.Repository;
using LiteDB;
using Microsoft.Toolkit.Uwp.Helpers;

namespace Hohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : PrismApplication
    {
        static readonly string OldLocalDbFileName = "_v3";
        static readonly string LocalDbFileName = "Hohoema_v1.db";
        
        static string MakeDbConnectionString(string dbName)
        {
            return $"Filename={Path.Combine(ApplicationData.Current.LocalFolder.Path, dbName)}; Upgrade=true;";
        }

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
            }

            await EnsureInitializeAsync();

            if (args.StartKind == StartKinds.Launch)
            {
                var pageManager = Container.Resolve<PageManager>();
                pageManager.OpenStartupPage();
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

            var primaryWindowCoreLayout = Container.Resolve<Views.PrimaryWindowCoreLayout>();
            var hohoemaInAppNotification = new Views.HohoemaInAppNotification()
            {
                VerticalAlignment = VerticalAlignment.Bottom
            };

            var grid = new Grid()
            {
                Children =
                {
                    primaryWindowCoreLayout,
                    hohoemaInAppNotification,
                    new Views.NoUIProcessScreen()
                }
            };

            var primaryWindowContentNavigationService = primaryWindowCoreLayout.CreateNavigationService();
            Container.GetContainer().RegisterInstance(primaryWindowContentNavigationService);

            var primaryViewPlayerNavigationService = primaryWindowCoreLayout.CreatePlayerNavigationService();
            var name = "PrimaryPlayerNavigationService";
            Container.GetContainer().RegisterInstance(name, primaryViewPlayerNavigationService);



#if DEBUG
            primaryWindowCoreLayout.FocusEngaged += (__, args) => Debug.WriteLine("focus engagad: " + args.OriginalSource.ToString());
#endif

            return grid;
        }


        static readonly Type[] _instantiateServiceTypes = new[]
        {
            typeof(Services.Notification.NotificationCacheVideoDeletedService),
            typeof(Services.Notification.CheckingClipboardAndNotificationService),
            typeof(Services.Notification.NotificationMylistUpdatedService),
            typeof(Services.Notification.NotificationFollowUpdatedService),
            typeof(Services.Notification.NotificationCacheRequestRejectedService),
            typeof(UseCase.Subscriptions.SubscriptionUpdateManager),
            typeof(UseCase.Subscriptions.FeedResultAddToWatchLater),
            typeof(UseCase.Subscriptions.SyncWatchHistoryOnLoggedIn),
            typeof(UseCase.Subscriptions.LatestSubscriptionVideosNotifier),
            //typeof(UseCase.VideoCacheResumingObserver),
            typeof(UseCase.NicoVideoPlayer.VideoPlayRequestBridgeToPlayer),
        };



        public override void RegisterTypes(IContainerRegistry container)
        {
            var unityContainer = container.GetContainer();

            // 各ウィンドウごとのスケジューラを作るように
            unityContainer.RegisterType<IScheduler>(new PerThreadLifetimeManager(), new InjectionFactory(c => SynchronizationContext.Current != null ? new SynchronizationContextScheduler(SynchronizationContext.Current) : null));

            // MediaPlayerを各ウィンドウごとに一つずつ作るように
            unityContainer.RegisterType<MediaPlayer>(new PerThreadLifetimeManager());

            // Settings
            unityContainer.RegisterSingleton<AppearanceSettingsRepository>();
            unityContainer.RegisterSingleton<CommentFliteringRepository>();
            unityContainer.RegisterSingleton<PinRepository>();
            unityContainer.RegisterSingleton<VideoListFilterSettings>();
            unityContainer.RegisterSingleton<NicoRepoSettingsRepository>();
            unityContainer.RegisterSingleton<PlayerSettingsRepository>();
            unityContainer.RegisterSingleton<RankingSettingsRepository>();
            unityContainer.RegisterSingleton<CacheSettingsRepository>();
            unityContainer.RegisterSingleton<SubscriptionSettingsRepository>();



            // Service
            unityContainer.RegisterSingleton<PageManager>();
            unityContainer.RegisterSingleton<PrimaryViewPlayerManager>();
            unityContainer.RegisterSingleton<ScondaryViewPlayerManager>();
            unityContainer.RegisterSingleton<Services.NiconicoLoginService>();
            unityContainer.RegisterSingleton<NoUIProcessScreenContext>();

            unityContainer.RegisterType<UseCase.Services.IConfirmCacheUsageDialogService, Services.ConfirmCacheUsageDialogService>();
            unityContainer.RegisterType<UseCase.Services.IInAppNotificationService, Services.InAppNotificationService>();
            unityContainer.RegisterType<UseCase.Services.IEditMylistGroupDialogService, Services.EditMylistGroupDialogService>();
            unityContainer.RegisterType<UseCase.Services.IMultiSelectionDialogService, Services.MultiSelectionDialogService>();
            unityContainer.RegisterType<UseCase.Services.ITextInputDialogService, Services.TextInputDialogService>();
            unityContainer.RegisterType<UseCase.Services.IMessageDialogService, Services.MessageDialogService>();
            unityContainer.RegisterType<UseCase.Services.INiconicoLoginDialogService, Services.NiconicoLoginDialogService>();
            unityContainer.RegisterType<UseCase.Services.INiconicoTwoFactorAuthDialogService, Services.NiconicoTwoFactorAuthDialogService>();
            unityContainer.RegisterType<UseCase.Services.IToastNotificationService, Services.ToastNotificationService>();

            foreach (var type in _instantiateServiceTypes)
            {
                unityContainer.RegisterSingleton(type);
            }

            // Models
            unityContainer.RegisterSingleton<NiconicoSession>();
            unityContainer.RegisterSingleton<Models.NicoVideoSessionOwnershipManager>();
            
            unityContainer.RegisterSingleton<FollowManager>();

            unityContainer.RegisterSingleton<Models.Subscriptions.SubscriptionManager>();

            // UseCase
            unityContainer.RegisterType<UseCase.VideoPlayer>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<UseCase.NicoVideoPlayer.CommentFiltering>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<UseCase.NicoVideoPlayer.MediaPlayerSoundVolumeManager>(new PerThreadLifetimeManager());
            unityContainer.RegisterSingleton<UseCase.VideoCache.VideoCacheManager>();
            unityContainer.RegisterSingleton<UseCase.Playlist.HohoemaPlaylist>();
            unityContainer.RegisterSingleton<UseCase.Playlist.LocalMylistManager>();
            unityContainer.RegisterSingleton<UseCase.Playlist.VideoItemsSelectionContext>();
            unityContainer.RegisterSingleton<UseCase.Playlist.WatchHistoryManager>();
            unityContainer.RegisterSingleton<UseCase.ApplicationLayoutManager>();
            unityContainer.RegisterSingleton<UseCase.Playlist.UserMylistManager>();
            unityContainer.RegisterSingleton<UseCase.Playlist.LocalMylistManager>();
            unityContainer.RegisterSingleton<MylistRepository>();
            

            // ViewModels
            unityContainer.RegisterSingleton<ViewModels.RankingCategoryListPageViewModel>();
            
            unityContainer.RegisterType<ViewModels.Player.CommentPlayer>(new PerThreadLifetimeManager());
            unityContainer.RegisterType<ViewModels.VideoPlayerPageViewModel>(new PerThreadLifetimeManager());

#if DEBUG
            //			BackgroundUpdater.MaxTaskSlotCount = 1;
#endif
            // TODO: プレイヤーウィンドウ上で管理する
            //			var backgroundTask = MediaBackgroundTask.Create();
            //			Container.RegisterInstance(backgroundTask);
        }



        protected override void RegisterRequiredTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.BlankPage>();
            containerRegistry.RegisterForNavigation<Views.CacheManagementPage>();
            containerRegistry.RegisterForNavigation<Views.ChannelVideoPage>();
            containerRegistry.RegisterForNavigation<Views.CommunityPage>();
            containerRegistry.RegisterForNavigation<Views.CommunityVideoPage>();
            containerRegistry.RegisterForNavigation<Views.DebugPage>();
            containerRegistry.RegisterForNavigation<Views.FollowManagePage>();
            containerRegistry.RegisterForNavigation<Views.LoginPage>();
            containerRegistry.RegisterForNavigation<Views.MylistPage>();
            containerRegistry.RegisterForNavigation<Views.LocalPlaylistPage>();
            containerRegistry.RegisterForNavigation<Views.NicoRepoPage>();
            containerRegistry.RegisterForNavigation<Views.RankingCategoryListPage>();
            containerRegistry.RegisterForNavigation<Views.RankingCategoryPage>();
            containerRegistry.RegisterForNavigation<Views.RecommendPage>();
            containerRegistry.RegisterForNavigation<Views.SearchPage>();
            containerRegistry.RegisterForNavigation<Views.SearchResultTagPage>();
            containerRegistry.RegisterForNavigation<Views.SearchResultMylistPage>();
            containerRegistry.RegisterForNavigation<Views.SearchResultKeywordPage>();
            containerRegistry.RegisterForNavigation<Views.SettingsPage>();
            containerRegistry.RegisterForNavigation<Views.UserInfoPage>();
            containerRegistry.RegisterForNavigation<Views.UserMylistPage>();
            containerRegistry.RegisterForNavigation<Views.UserVideoPage>();
            containerRegistry.RegisterForNavigation<Views.VideoInfomationPage>();
            containerRegistry.RegisterForNavigation<Views.WatchHistoryPage>();
            containerRegistry.RegisterForNavigation<Views.SeriesPage>();
            containerRegistry.RegisterForNavigation<Views.UserSeriesPage>();
            containerRegistry.RegisterForNavigation<Views.WatchAfterPage>();
            containerRegistry.RegisterForNavigation<Views.SubscriptionManagementPage>();            

            containerRegistry.RegisterForNavigation<Views.VideoPlayerPage>();

            base.RegisterRequiredTypes(containerRegistry);
        }

        public bool IsTitleBarCustomized { get; } = DeviceTypeHelper.IsDesktop && Services.Helpers.InputCapabilityHelper.IsMouseCapable;

        Models.Helpers.AsyncLock InitializeLock = new Models.Helpers.AsyncLock();
        bool isInitialized = false;
        private async Task EnsureInitializeAsync()
        {
            using (await InitializeLock.LockAsync())
            {
                if (isInitialized) { return; }
                isInitialized = true;

                HohoemaLiteDb.Initialize((s) => new LiteRepository(s));

                MonkeyCache.LiteDB.Barrel.ApplicationId = nameof(Hohoema);

                try
                {
                    var c = MonkeyCache.LiteDB.Barrel.Current;
                }
                catch
                {
                    var folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("Hohoema");
                    if (folder != null)
                    {
                        var cacheFolder = await folder.GetFolderAsync("MonkeyCache");
                        var barrelFile = await cacheFolder.GetFileAsync("Barrel.db");
                        await barrelFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                }

                var unityContainer = Container.GetContainer();
                unityContainer.RegisterInstance<MonkeyCache.IBarrel>(MonkeyCache.LiteDB.Barrel.Current);

                // v1.0.0以前のローカルdbの名前をリネーム
                try
                {
                    var oldLocalDbFile = await StorageFile.GetFileFromPathAsync(Path.Combine(ApplicationData.Current.LocalFolder.Path, OldLocalDbFileName));
                    if (oldLocalDbFile != null)
                    {
                        await oldLocalDbFile.RenameAsync(LocalDbFileName);

                        Debug.WriteLine("ローカルDBのリネームを実行しました。（ _v3 -> Hohoema.db）");
                    }
                }
                catch { }

                var localDb = new LiteDatabase(MakeDbConnectionString(LocalDbFileName));
                unityContainer.RegisterInstance<ILiteDatabase>(localDb);



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

                
                
                var appearanceSettings = Container.Resolve<AppearanceSettingsRepository>();
                I18NPortable.I18N.Current.Locale = appearanceSettings.Locale ?? I18NPortable.I18N.Current.Locale;

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(I18NPortable.I18N.Current.Locale);

                // 設定の統合
                await Container.Resolve<UseCase.Migration.v0_22_x_MigrationSettings>().Migration();


                // ログイン前にログインセッションによって状態が変化するフォローとマイリストの初期化
                var followManager = Container.Resolve<FollowManager>();
                //Container.Resolve<UserMylistManager>();
                Container.Resolve<MylistRepository>();
                Container.Resolve<PlayerSettingsRepository>();

                Resources["IsXbox"] = DeviceTypeHelper.IsXbox;
                Resources["IsMobile"] = DeviceTypeHelper.IsMobile;


                try
                {
#if DEBUG
                    if (_DEBUG_XBOX_RESOURCE)
#else
                    if (Services.Helpers.DeviceTypeHelper.IsXbox)
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
                var cacheSettings = Container.Resolve<CacheSettingsRepository>();
                Resources["IsCacheEnabled"] = cacheSettings.IsCacheEnabled;

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





                // 更新通知を表示
                try
                {
                    if (Models.Helpers.AppUpdateNotice.HasNotCheckedUptedeNoticeVersion)
                    {
                        var dialogService = Container.Resolve<LatestUpdateNoticeDialogService>();
                        _ = dialogService.ShowLatestUpdateNotice();
                        Models.Helpers.AppUpdateNotice.UpdateLastCheckedVersionInCurrentVersion();
                    }
                }
                catch { }

                // バージョン間データ統合
                var migrationSubscription = Container.Resolve<UseCase.Migration.MigrationSubscriptions>();
                migrationSubscription.Migration();
                Container.Resolve<UseCase.Migration.CommentFilteringNGScoreZeroFixture>().Migration();


                // アプリのユースケース系サービスを配置
                foreach (var type in _instantiateServiceTypes)
                {
                    Container.Resolve(type);
                }

                // バックグラウンドでのトースト通知ハンドリングを初期化
                await RegisterDebugToastNotificationBackgroundHandling();


                try
                {
                    var cacheManager = Container.Resolve<UseCase.VideoCache.VideoCacheManager>();
                    _ = cacheManager.Initialize();
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
                    var nicoContentId = Models.Helpers.NicoVideoIdHelper.UrlToVideoId(arguments);

                    if (nicoContentId != null)
                    {
                        if (nicoContentId.IsVideoId())
                        {
                            PlayVideoFromExternal(nicoContentId);
                            isHandled = true;
                        }
                    }

                    var payload = LoginRedirectPayload.FromParameterString(arguments);
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
                        }
                    }
                    
                    if (Uri.TryCreate(arguments, UriKind.Absolute, out var uri))
                    {
                        var pageManager = Container.Resolve<PageManager>();
                        if (pageManager.OpenPage(uri))
                        {
                            isHandled = true;
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

                        var cacheManager = Container.Resolve<UseCase.VideoCache.VideoCacheManager>();
                        await cacheManager.CancelCacheRequest(videoId);
                    }
                    else
                    {
                        var nicoContentId = Models.Helpers.NicoVideoIdHelper.UrlToVideoId(arguments);

                        if (nicoContentId.IsVideoId())
                        {
                            PlayVideoFromExternal(nicoContentId);
                        }
                    }
                }

                
            }
            else if (args.Kind == ActivationKind.Protocol)
            {
                var param = (args as IActivatedEventArgs) as ProtocolActivatedEventArgs;
                var uri = param.Uri;
                var maybeNicoContentId = new string(uri.OriginalString.Skip("niconico://".Length).TakeWhile(x => x != '?' && x != '/').ToArray());

                if (maybeNicoContentId.IsVideoId()
                    || maybeNicoContentId.All(char.IsDigit)
                    )
                {
                    PlayVideoFromExternal(maybeNicoContentId);
                }
            }
		}


        public override void OnInitialized()
        {
            Window.Current.Activate();

            // ログイン
            try
            {
                var niconicoSession = Container.Resolve<NiconicoSession>();
                if (Models.Niconico.Account.AccountManager.HasPrimaryAccount())
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
            var hohoemaPlaylist = Container.Resolve<UseCase.Playlist.HohoemaPlaylist>();

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

                var pageNameWithParameter = assemblyQualifiedAppType.Replace(viewType.FullName, "Hohoema.ViewModels.{0}ViewModel");

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
            if (layoutManager.AppLayout == Models.Pages.ApplicationLayout.TV)
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
            else if (layoutManager.AppLayout == Models.Pages.ApplicationLayout.Mobile)
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
                    IsInternetAvailable = Models.Helpers.InternetConnection.IsInternet(),
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
            var toast = Container.Resolve<ToastNotificationService>();
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
                var cacheManager = Container.Resolve<UseCase.VideoCache.VideoCacheManager>();

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

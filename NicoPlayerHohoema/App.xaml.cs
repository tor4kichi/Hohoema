using NicoPlayerHohoema.ViewModels;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Prism.Unity.Windows;
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Prism.Events;
using NicoPlayerHohoema.Events;
using Prism.Windows.Navigation;
using Prism.Windows.AppModel;
using Prism.Windows.Mvvm;
//using BackgroundAudioShared;
using Windows.Media;
using NicoPlayerHohoema.Models.Db;
using Windows.Storage;
using System.Text;
using NicoPlayerHohoema.Util;

namespace NicoPlayerHohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Prism.Unity.Windows.PrismUnityApplication
    {
		public PlayerWindowManager PlayerWindow { get; private set; }

		private bool _IsPreLaunch;


		public const string ACTIVATION_WITH_ERROR = "error";

		static App()
		{
		}

		/// <summary>
		/// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
		///最初の行であるため、main() または WinMain() と論理的に等価です。
		/// </summary>
		public App()
        {
			UnhandledException += PrismUnityApplication_UnhandledException;

			this.Resuming += App_Resuming;
			//			this.Suspending += App_Suspending;

			RequestedTheme = GetTheme();


			this.InitializeComponent();
		}


		/*
		private async void App_Suspending(object sender, SuspendingEventArgs e)
		{
			
			var deferral = e.SuspendingOperation.GetDeferral();
			var hohoemaApp = Container.Resolve<HohoemaApp>();
			await hohoemaApp.OnSuspending();

			deferral.Complete();
		}
		*/

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

		protected override async Task OnSuspendingApplicationAsync()
		{
			if (_IsPreLaunch) { return; }

			var hohoemaApp = Container.Resolve<HohoemaApp>();
			
			// Note: ここで呼び出すとスレッドロックが発生するので
			// HohoemaViewModelBaseのNavigationFrom内でサスペンドの処理を行っています

//			await hohoemaApp.OnSuspending().ConfigureAwait(false);
//			await HohoemaApp.SyncToRoamingData().ConfigureAwait(false);

			
			await base.OnSuspendingApplicationAsync();
		}
		

		private async void App_Resuming(object sender, object e)
		{
			if (_IsPreLaunch) { return; }

			//			var backgroundTask = MediaBackgroundTask.Create();
			//			Container.RegisterInstance(backgroundTask);
			
			var hohoemaApp = Container.Resolve<HohoemaApp>();

			try
			{
				hohoemaApp.Resumed();
			}
			catch
			{
				Debug.WriteLine("アプリモデルの復帰処理でエラーを検出しました。");
				throw;
			}

			try
			{
				await CheckVideoCacheFolderState();
			}
			catch
			{
				Debug.WriteLine("キャッシュフォルダチェックに失敗しました。");
			}


		}

		protected override async Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
		{
#if DEBUG
			DebugSettings.IsBindingTracingEnabled = true;
#endif
			_IsPreLaunch = args.PrelaunchActivated;

			if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
			{
				//TODO: Load state from previously suspended application	
			}

			var pageManager = Container.Resolve<PageManager>();
            var hohoemaApp = Container.Resolve<HohoemaApp>();

			if (!args.PrelaunchActivated && args.Kind == ActivationKind.Launch)
			{
                // メディアバックグラウンドタスクの動作状態を初期化
                //				ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.AppState);


                //				var hohoemaApp = Container.Resolve<HohoemaApp>();
                //				if (HohoemaApp.HasPrimaryAccount())
                //				{
                //					pm.OpenPage(HohoemaPageType.Portal);
                //				}
                //				else

                await hohoemaApp.SignInWithPrimaryAccount();

                pageManager.OpenPage(HohoemaPageType.Portal);
			}

			
//			return Task.CompletedTask;
		}

		protected override async Task OnActivateApplicationAsync(IActivatedEventArgs args)
		{
			// モバイルで利用している場合に、ナビゲーションバーなどがページに被さらないように指定
			ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);

			var pageManager = Container.Resolve<PageManager>();

			if (args.Kind == ActivationKind.ToastNotification)
			{
				//Get the pre-defined arguments and user inputs from the eventargs;
				var toastArgs = args as IActivatedEventArgs as ToastNotificationActivatedEventArgs;
				var arguments = toastArgs.Argument;
				//				var input = toastArgs.UserInput["1"];
				if (arguments == ACTIVATION_WITH_ERROR)
				{
					await ShowErrorLog().ConfigureAwait(false);
				}
			}

			if (args.Kind == ActivationKind.Protocol)
			{
				var param = (args as IActivatedEventArgs) as ProtocolActivatedEventArgs;
				var uri = param.Uri;
				var maybeNicoContentId = new string(uri.OriginalString.Skip("niconico://".Length).TakeWhile(x => x != '?' && x != '/').ToArray());

				
				if (Mntone.Nico2.NiconicoRegex.IsVideoId(maybeNicoContentId)
					|| maybeNicoContentId.All(x => x >= '0' && x <= '9'))
				{
					HohoemaApp hohoemaApp = null;
					try
					{
						hohoemaApp = Container.Resolve<HohoemaApp>();
					}
					catch { }

                    pageManager.OpenPage(HohoemaPageType.VideoPlayer,
                        new VideoPlayPayload()
                        {
                            VideoId = maybeNicoContentId
                        }.ToParameterString()
                    );
				}
				else if (Mntone.Nico2.NiconicoRegex.IsLiveId(maybeNicoContentId))
				{
					HohoemaApp hohoemaApp = null;
					try
					{
						hohoemaApp = Container.Resolve<HohoemaApp>();
					}
					catch { }

					pageManager.OpenPage(HohoemaPageType.LiveVideoPlayer, 
						new Models.Live.LiveVidePagePayload(maybeNicoContentId)
						.ToParameterString()
						);
					
				}
			}


			await base.OnActivateApplicationAsync(args);
		}
		protected override void OnActivated(IActivatedEventArgs args)
		{
			
			base.OnActivated(args);
		}

		public async Task<string> GetMostRecentErrorText()
		{
			var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("error", CreationCollisionOption.OpenIfExists);
			var errorFiles = await folder.GetItemsAsync();

			var errorFile = errorFiles
				.Where(x => x.Name.StartsWith("hohoema_error"))
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

		/// <summary>
		/// 動画キャッシュ保存先フォルダをチェックします
		/// 選択済みだがフォルダが見つからない場合に、トースト通知を行います。
		/// </summary>
		/// <returns></returns>
		public async Task CheckVideoCacheFolderState()
		{
			var hohoemaApp = Container.Resolve<HohoemaApp>();
			var cacheFolderState = await hohoemaApp.GetVideoCacheFolderState();

			if (cacheFolderState == CacheFolderAccessState.SelectedButNotExist)
			{
				var toastService = Container.Resolve<Views.Service.ToastNotificationService>();
				toastService.ShowText(
					"キャッシュが利用できません"
					, "キャッシュ保存先フォルダが見つかりません。（ここをタップで設定画面を表示）"
					, duration: Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
					, toastActivatedAction: async () =>
					{
						await HohoemaApp.UIDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => 
						{
							var pm = Container.Resolve<PageManager>();
							pm.OpenPage(HohoemaPageType.Settings, HohoemaSettingsKind.Cache.ToString());
						});
					});
			}
		}


		protected override async Task OnInitializeAsync(IActivatedEventArgs args)
		{
			await Models.Db.NicoVideoDbContext.InitializeAsync();
			await Models.Db.HistoryDbContext.InitializeAsync();


			Microsoft.Toolkit.Uwp.UI.ImageCache.CacheDuration = TimeSpan.FromHours(24);

			// TwitterAPIの初期化
			await TwitterHelper.Initialize();

			await RegisterTypes();

			var hohoemaApp = Container.Resolve<HohoemaApp>();

			hohoemaApp.MediaManager.Context.DoneDownload += Context_DoneDownload;
			hohoemaApp.MediaManager.Context.StartDownload += (sender, a) => 
			{
				Debug.WriteLine("ダウンロードを開始" + a.RawVideoId);
			};
			hohoemaApp.MediaManager.Context.CancelDownload += (sender, a) =>
			{
				Debug.WriteLine("ダウンロードをキャンセル:" + a.RawVideoId);
			};

			//			var playNicoVideoEvent = EventAggregator.GetEvent<PlayNicoVideoEvent>();
			//			playNicoVideoEvent.Subscribe(PlayNicoVideoInPlayerWindow);

			await base.OnInitializeAsync(args);
		}

		private void Context_DoneDownload(NicoVideoDownloadContext sender, NiconicoDownloadEventArgs args)
		{
			var hohoemaApp = Container.Resolve<HohoemaApp>();
			var toastService = Container.Resolve<Views.Service.ToastNotificationService>();
			var pageManager = Container.Resolve<PageManager>();

			try
			{
				var videoData = Models.Db.VideoInfoDb.Get(args.RawVideoId);

				if (videoData != null)
				{
					toastService.ShowText(
						videoData.Title,
						$"キャッシュが完了、このメッセージをタップして再生開始",
						toastActivatedAction: () =>
						{
						// 再生
						var videoPagePayload = new VideoPlayPayload()
							{
								VideoId = args.RawVideoId,
								Quality = args.Quality
							};
							pageManager.OpenPage(HohoemaPageType.VideoPlayer, videoPagePayload.ToParameterString());
						}
						);
				}
			}
			catch { }
		}

		private async void PlayNicoVideoInPlayerWindow(string videoUrl)
		{
			await OpenPlayerWindow();

			await PlayerWindow.OpenVideo(videoUrl);
		}


		private async Task RegisterTypes()
		{
			// Models
			var hohoemaApp = await HohoemaApp.Create(EventAggregator);
			Container.RegisterInstance(hohoemaApp);
			Container.RegisterInstance(new PageManager(NavigationService));
			Container.RegisterInstance(hohoemaApp.ContentFinder);

			// 非同期更新機能の同時実行タスク数を指定
			var deviceFamily = Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily;
			BackgroundUpdater.MaxTaskSlotCount = deviceFamily.EndsWith("Mobile") ? 1u : 2u;

#if DEBUG
//			BackgroundUpdater.MaxTaskSlotCount = 1;
#endif
			// TODO: プレイヤーウィンドウ上で管理する
			//			var backgroundTask = MediaBackgroundTask.Create();
			//			Container.RegisterInstance(backgroundTask);


			// ViewModels
			Container.RegisterType<ViewModels.MenuNavigatePageBaseViewModel>(new ContainerControlledLifetimeManager());

			Container.RegisterType<ViewModels.RankingCategoryPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.HistoryPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.UserVideoPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.SearchPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.MylistPageViewModel>(new ContainerControlledLifetimeManager());
			//			Container.RegisterType<ViewModels.UserVideoPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.FeedVideoListPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.UserMylistPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.CacheManagementPageViewModel>(new ContainerControlledLifetimeManager());
//			Container.RegisterType<ViewModels.PortalPageViewModel>(new ContainerControlledLifetimeManager());


			// Service
			Container.RegisterType<Views.Service.RankingChoiceDialogService>();
			Container.RegisterInstance(new Views.Service.ToastNotificationService());
			Container.RegisterInstance(new Views.Service.MylistRegistrationDialogService(hohoemaApp));
			Container.RegisterInstance(new Views.Service.EditMylistGroupDialogService());
			Container.RegisterInstance(new Views.Service.AcceptCacheUsaseDialogService());
			Container.RegisterInstance(new Views.Service.TextInputDialogService());
			Container.RegisterInstance(new Views.Service.ContentSelectDialogDefaultSet());
            Container.RegisterInstance(new Views.Service.AccountManagementDialogService(hohoemaApp));
            
//			return Task.CompletedTask;
        }


		protected override void ConfigureViewModelLocator()
		{
			base.ConfigureViewModelLocator();
		}


		protected override void OnWindowCreated(WindowCreatedEventArgs args)
		{
			base.OnWindowCreated(args);
			
//			var mainWindowView = ApplicationView.GetForCurrentView();
//			mainWindowView.Consolidated += MainWindowView_Consolidated;

		}

		private void MainWindowView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
		{
			if (PlayerWindow == null) { App.Current.Exit(); }

			if (sender.Id  == PlayerWindow.ViewId)
			{
				PlayerWindow.Closed();
			}
			else
			{
				App.Current.Exit();
			}
		}

		protected override UIElement CreateShell(Frame rootFrame)
		{
			var menu = new Views.MenuNavigatePageBase();

			var viewModel = Container.Resolve<ViewModels.MenuNavigatePageBaseViewModel>();
			menu.DataContext = viewModel;

			menu.Content = rootFrame;

			rootFrame.Navigating += RootFrame_Navigating;
			
			return menu;
		}

		private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
		{
			// Note: 有害動画の確認ページへの進む動作を防止する
			if (e.NavigationMode == NavigationMode.Forward)
			{
				if (e.SourcePageType.Name.EndsWith("Page"))
				{
					var pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page"));

					HohoemaPageType pageType;
					if (Enum.TryParse(pageTypeString, out pageType))
					{
						if (pageType == HohoemaPageType.ConfirmWatchHurmfulVideo)
						{
							e.Cancel = true;
							return;
						}
					}
				}
			}
		}


		private async void PrismUnityApplication_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			e.Handled = true;

			Debug.Write(e.Message);

			await WriteErrorFile(e.Exception);

//			ShowErrorToast();
		}

		public async Task WriteErrorFile(Exception e)
		{
			try
			{
				var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("error", CreationCollisionOption.OpenIfExists);
				var errorFile = await folder.CreateFileAsync($"hohoema_error_{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.txt", CreationCollisionOption.OpenIfExists);

				var version = Package.Current.Id.Version;
				var versionText = $"{version.Major}.{version.Minor}.{version.Build}";
				var stringBuilder = new StringBuilder();
				var pageManager = Container.Resolve<PageManager>();
				stringBuilder.AppendLine($"Hohoema {versionText}");
				stringBuilder.AppendLine("開いていたページ:" + pageManager.CurrentPageType.ToString());
				stringBuilder.AppendLine("");
				stringBuilder.AppendLine("= = = = = = = = = = = = = = = =");
				stringBuilder.AppendLine("");
				stringBuilder.AppendLine(e.ToString());

				await FileIO.WriteTextAsync(errorFile, stringBuilder.ToString());
			}
			catch { }
		}

		public void ShowErrorToast()
		{
			var toast = Container.Resolve<Views.Service.ToastNotificationService>();
			toast.ShowText("Hohoema実行中に不明なエラーが発生しました"
				, "ここをタップすると再起動できます。"
				, Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long
				, luanchContent: ACTIVATION_WITH_ERROR
				);
		}

		
		private async Task OpenPlayerWindow()
		{
			var currentViewId = ApplicationView.GetForCurrentView().Id;
			System.Diagnostics.Debug.WriteLine($"MainWindow ViewId is {currentViewId}");


			if (PlayerWindow == null)
			{
				var view = CoreApplication.CreateNewView();

				PlayerWindow = await PlayerWindowManager.CreatePlayerWindowManager(view);
			}

			await PlayerWindow.ShowFront(currentViewId);
		}
	}




	public class PlayerWindowManager
	{
		public int ViewId { get; private set; }
		
		
		public CoreApplicationView View { get; private set; }
		public INavigationService NavigationService { get; private set;}



		PlayerWindowManager(CoreApplicationView view, int id, INavigationService ns)
		{
			this.View = view;
			NavigationService = ns;
			ViewId = id;
		}

		public static async Task<PlayerWindowManager> CreatePlayerWindowManager(CoreApplicationView playerView)
		{
			INavigationService ns = null;
			int id = 0;
			await playerView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var frame = new Frame();
				var frameFacade = new FrameFacadeAdapter(frame);
				Window.Current.Content = frame;

				var sessionStateService = new SessionStateService();
				ns = new FrameNavigationService(frameFacade
					, (pageToken) =>
					{
						if (pageToken == nameof(Views.VideoPlayerPage))
						{
							return typeof(Views.VideoPlayerPage);
						}
						else
						{
							return typeof(Page);
						}
					}, sessionStateService);
				id = ApplicationView.GetApplicationViewIdForWindow(playerView.CoreWindow);
			});

			return new PlayerWindowManager(playerView, id, ns);
		}

		public async Task ShowFront(int mainViewid)
		{
			await View.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
			{
				View.CoreWindow.Activate();
				await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
					ViewId
					, ViewSizePreference.Default
					, mainViewid
					, ViewSizePreference.Default
					);
			});
		}

		public async Task OpenVideo(string videoUrl)
		{
			// サブウィンドウをアクティベートして、サブウィンドウにPlayerページナビゲーションを飛ばす
			await View.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (!NavigationService.Navigate(nameof(Views.VideoPlayerPage), videoUrl))
				{
					System.Diagnostics.Debug.WriteLine("Failed open player.");
				}
				NavigationService.ClearHistory();
			});
		}


		public async void Closed()
		{
			await View.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				NavigationService.Navigate("", null);
				NavigationService.ClearHistory();

				
			});

			await Task.Delay(3000);
		}

	}
}

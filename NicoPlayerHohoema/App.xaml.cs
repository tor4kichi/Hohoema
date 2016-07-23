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
using BackgroundAudioShared;
using Windows.Media;

namespace NicoPlayerHohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Prism.Unity.Windows.PrismUnityApplication
    {
		public PlayerWindowManager PlayerWindow { get; private set; }

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
			
			this.InitializeComponent();
		}

		

		protected override async Task OnSuspendingApplicationAsync()
		{
			await base.OnSuspendingApplicationAsync();

//			var backTask = Container.Resolve<MediaBackgroundTask>();
//			Container.Teardown(backTask);

			var hohoemaApp = Container.Resolve<HohoemaApp>();
//			hohoemaApp.SignOut().ConfigureAwait(false);

//			await hohoemaApp.MediaManager.Context.Suspending();

		}

		

		private async void App_Resuming(object sender, object e)
		{
//			var backgroundTask = MediaBackgroundTask.Create();
//			Container.RegisterInstance(backgroundTask);

			var hohoemaApp = Container.Resolve<HohoemaApp>();
			await hohoemaApp.SignInFromUserSettings();

			await hohoemaApp.MediaManager.Context.Resume();
		}

		protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
		{
#if DEBUG
			DebugSettings.IsBindingTracingEnabled = true;
#endif

			if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
			{
				//TODO: Load state from previously suspended application
				var hohoemaApp = Container.Resolve<HohoemaApp>();
				
			}


			if (!args.PrelaunchActivated)
			{
				// メディアバックグラウンドタスクの動作状態を初期化
				ApplicationSettingsHelper.ReadResetSettingsValue(ApplicationSettingsConstants.AppState);

				var pm = Container.Resolve<PageManager>();
				pm.OpenPage(HohoemaPageType.Login, true /* Enable auto login */);
			}

			return Task.CompletedTask;
		}

		

		protected override async Task OnInitializeAsync(IActivatedEventArgs args)
		{
			await RegisterTypes();

			var playNicoVideoEvent = EventAggregator.GetEvent<PlayNicoVideoEvent>();
			playNicoVideoEvent.Subscribe(PlayNicoVideoInPlayerWindow);

			await base.OnInitializeAsync(args);
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

			// TODO: プレイヤーウィンドウ上で管理する
			var backgroundTask = MediaBackgroundTask.Create();
			Container.RegisterInstance(backgroundTask);


			// ViewModels
			Container.RegisterType<ViewModels.MenuNavigatePageBaseViewModel>(new ContainerControlledLifetimeManager());

			Container.RegisterType<ViewModels.RankingCategoryPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.HistoryPageViewModel>(new ContainerControlledLifetimeManager());
			//			Container.RegisterType<ViewModels.SubscriptionPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.UserVideoPageViewModel>(new ContainerControlledLifetimeManager());
//			Container.RegisterType<ViewModels.SearchPageViewModel>(new ContainerControlledLifetimeManager());
			//			Container.RegisterType<ViewModels.UserVideoPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.FavoriteAllFeedPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.UserMylistPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.CacheManagementPageViewModel>(new ContainerControlledLifetimeManager());
			//			Container.RegisterType<ViewModels.PortalContent.MylistPortalPageContentViewModel>(new ContainerControlledLifetimeManager());
			//			Container.RegisterType<ViewModels.PortalContent.FavPortalPageContentViewModel>(new ContainerControlledLifetimeManager());
			//			Container.RegisterType<ViewModels.PortalContent.HistoryPortalPageContentViewModel>(new ContainerControlledLifetimeManager());

			Container.RegisterType<ViewModels.VideoPlayerPageViewModel>(new TransientLifetimeManager());


			// Service
			Container.RegisterType<Views.Service.ISearchDialogService, Views.Service.SearchDialogService>();
			Container.RegisterType<Views.Service.RankingChoiceDialogService>();

		}


		protected override void ConfigureViewModelLocator()
		{
			base.ConfigureViewModelLocator();
		}


		protected override void OnWindowCreated(WindowCreatedEventArgs args)
		{
			base.OnWindowCreated(args);

			var mainWindowView = ApplicationView.GetForCurrentView();
			mainWindowView.Consolidated += MainWindowView_Consolidated;

		}

		private void MainWindowView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
		{
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

			rootFrame.Navigated += RootFrame_Navigated;

			return menu;
		}

		private void RootFrame_Navigated(object sender, NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Forward)
			{
				if (e.SourcePageType.Name.EndsWith("Page"))
				{
					var pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page"));

					HohoemaPageType pageType;
					if (Enum.TryParse(pageTypeString, out pageType))
					{
						var pageManager = Container.Resolve<PageManager>();
						pageManager.OnNavigated(pageType);
					}
					else
					{
						throw new NotSupportedException();
					}
				}
				else
				{
					throw new Exception();
				}
			}
		}



		private void PrismUnityApplication_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			e.Handled = true;

			Debug.Write(e.Message);
			Debugger.Break();
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

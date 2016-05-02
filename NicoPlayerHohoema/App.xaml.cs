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

namespace NicoPlayerHohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Prism.Unity.Windows.PrismUnityApplication
    {
		public PlayerWindowManager PlayerWindow { get; private set; }


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

		

		protected override Task OnSuspendingApplicationAsync()
		{
			Task.Run(async () => 
			{
				var hohoemaApp = Container.Resolve<HohoemaApp>();
				await hohoemaApp.SignOut();
			});

			return base.OnSuspendingApplicationAsync();
		}

		private void App_Resuming(object sender, object e)
		{
			Task.Run(async () =>
			{
				var hohoemaApp = Container.Resolve<HohoemaApp>();
				await hohoemaApp.SignInFromUserSettings();
			});
		}

		protected override Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
		{
#if DEBUG
			DebugSettings.IsBindingTracingEnabled = true;
#endif

			Window.Current.Activate();

			return Task.FromResult<object>(null);
		}

		

		protected override async Task OnInitializeAsync(IActivatedEventArgs args)
		{
			RegisterTypes();

			var hohoemaApp = Container.Resolve<HohoemaApp>();
			await hohoemaApp.LoadUserSettings();

			var pm = Container.Resolve<PageManager>();
			pm.OpenPage(HohoemaPageType.Login, true /* Enable auto login */);


			var playNicoVideoEvent = EventAggregator.GetEvent<PlayNicoVideoEvent>();
			playNicoVideoEvent.Subscribe(PlayNicoVideoInPlayerWindow);
			await base.OnInitializeAsync(args);
		}

		private async void PlayNicoVideoInPlayerWindow(string videoUrl)
		{
			await OpenPlayerWindow();

			await PlayerWindow.OpenVideo(videoUrl);
		}


		private void RegisterTypes()
		{
			// Models
			Container.RegisterInstance(new HohoemaApp(EventAggregator));
			Container.RegisterInstance(new PageManager(NavigationService));


			// ViewModels
			Container.RegisterType<ViewModels.MenuNavigatePageBaseViewModel>(new ContainerControlledLifetimeManager());

			Container.RegisterType<ViewModels.RankingCategoryPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.HistoryPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.SubscriptionPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.SearchPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.SettingsPageViewModel>(new ContainerControlledLifetimeManager());

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
//			var ui = base.CreateShell(rootFrame);

			var menuPage = new Views.MenuNavigatePageBase();

			menuPage.Content = rootFrame;


			

			return menuPage;
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
						if (pageToken == nameof(Views.PlayerPage))
						{
							return typeof(Views.PlayerPage);
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
				if (!NavigationService.Navigate(nameof(Views.PlayerPage), videoUrl))
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

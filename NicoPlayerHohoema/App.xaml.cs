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

namespace NicoPlayerHohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Prism.Unity.Windows.PrismUnityApplication
    {
		public CoreApplicationView PlayerWindow { get; private set; }


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

		

		protected override Task OnInitializeAsync(IActivatedEventArgs args)
		{
			RegisterTypes();

			var hohoemaApp = Container.Resolve<HohoemaApp>();
			Task.Run(async () =>
			{
				await hohoemaApp.SignInFromUserSettings();
			});

			var pm = Container.Resolve<PageManager>();
			pm.OpenPage(HohoemaPageType.Ranking);


//			CreatePlayerWindow();


			var playNicoVideoEvent = EventAggregator.GetEvent<PlayNicoVideoEvent>();
			playNicoVideoEvent.Subscribe(PlayNicoVideoWithCurrentPlayerSetting);

			return base.OnInitializeAsync(args);
		}

		private void PlayNicoVideoWithCurrentPlayerSetting(string videoUrl)
		{
			var hohoemaApp = Container.Resolve<HohoemaApp>();

			switch (PlayerDisplayMode.MainWindow)
			{
				case PlayerDisplayMode.MainWindow:
					// 現在のウィンドウに対してPlayerページへのナビゲーションを飛ばす
					PlayNicoVideoInMainWindow(videoUrl);
					break;
				case PlayerDisplayMode.Standalone:
					PlayNicoVideoInSubWindow(videoUrl);
					break;
				default:
					break;
			}
		}

		private void PlayNicoVideoInMainWindow(string videoUrl)
		{
			NavigationService.Navigate("Player", videoUrl);
		}

		private async void PlayNicoVideoInSubWindow(string videoUrl)
		{
			// サブウィンドウをアクティベートして、サブウィンドウにPlayerページナビゲーションを飛ばす
			await PlayerWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				((Frame)Window.Current.Content).Navigate(typeof(Views.PlayerPage), videoUrl);
				Window.Current.Activate();
			});
		}

		private void RegisterTypes()
		{
			// Models
			Container.RegisterInstance(new HohoemaApp());
			Container.RegisterInstance(new PageManager(NavigationService));


			// ViewModels
			Container.RegisterType<ViewModels.MenuNavigatePageBaseViewModel>(new ContainerControlledLifetimeManager());

			Container.RegisterType<ViewModels.RankingPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.HistoryPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.SubscriptionPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.SearchPageViewModel>(new ContainerControlledLifetimeManager());
			Container.RegisterType<ViewModels.SettingsPageViewModel>(new ContainerControlledLifetimeManager());

		}
		protected override void OnWindowCreated(WindowCreatedEventArgs args)
		{
			base.OnWindowCreated(args);

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

		private async void CreatePlayerWindow()
		{
			if (PlayerWindow == null)
			{
				var currentViewId = ApplicationView.GetForCurrentView().Id;
				PlayerWindow = CoreApplication.CreateNewView();
				
				await PlayerWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					Window.Current.Content = new Frame();
					((Frame)Window.Current.Content).Navigate(typeof(Views.PlayerPage));
//					Window.Current.Activate();
					/*
					await ApplicationViewSwitcher.TryShowAsStandaloneAsync(
						ApplicationView.GetApplicationViewIdForWindow(Window.Current.CoreWindow),
						ViewSizePreference.Default,
						currentViewId,
						ViewSizePreference.Default);
					*/

					Window.Current.Close();

				});
			}
		}
	}
}

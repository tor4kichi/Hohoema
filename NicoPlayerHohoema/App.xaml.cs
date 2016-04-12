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

namespace NicoPlayerHohoema
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : Prism.Unity.Windows.PrismUnityApplication
    {
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

			return base.OnInitializeAsync(args);
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
	}
}

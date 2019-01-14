using Microsoft.Toolkit.Uwp.UI.Animations;
using NicoPlayerHohoema.Models.Helpers;
using Prism.Events;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Prism.Unity;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class MenuNavigatePageBase : ContentControl
	{
        CoreDispatcher _UIDispatcher;
        public MenuNavigatePageBase()
		{
			this.InitializeComponent();

            this.Loaded += MenuNavigatePageBase_Loaded;
            this.Loading += MenuNavigatePageBase_Loading;
        }

        private void MenuNavigatePageBase_Loading(FrameworkElement sender, object args)
        {
            DataContext = App.Current.Container.Resolve<ViewModels.MenuNavigatePageBaseViewModel>();
        }

        private void MenuNavigatePageBase_Loaded(object sender, RoutedEventArgs e)
        {
            _UIDispatcher = Dispatcher;

            var subpageContentControl = GetTemplateChild("SubPageContentControl") as ContentControl;
            if (subpageContentControl != null)
            {
                subpageContentControl.ObserveDependencyProperty(ContentControl.ContentProperty)
                    .Subscribe(x =>
                    {
                        if (subpageContentControl.Content == null)
                        {
                            ShowMenuMainContent();
                        }
                        else
                        {
                            ShowMenuSubContent();
                        }
                    });

                ShowMenuMainContent();
            }
        }
        


        AsyncLock _MenuContentToggleLock = new AsyncLock();

        static readonly double MenuMainSubContentToggleAnimationDuration = 175;
        public async void ShowMenuMainContent()
        {
            var showTarget = GetTemplateChild("MenuMainPageContent") as FrameworkElement;
            var hideTarget = GetTemplateChild("MenuSubPageContent") as FrameworkElement;

            using (var releaser = await _MenuContentToggleLock.LockAsync())
            {
                showTarget.Visibility = Visibility.Visible;
                hideTarget.IsHitTestVisible = false;
                await Task.WhenAll(
                    showTarget.Fade(1.0f, MenuMainSubContentToggleAnimationDuration).Offset(0, duration: MenuMainSubContentToggleAnimationDuration).StartAsync(),
                    hideTarget.Fade(0.0f, MenuMainSubContentToggleAnimationDuration).Offset(100, duration: MenuMainSubContentToggleAnimationDuration).StartAsync()
                    );
                showTarget.IsHitTestVisible = true;
                hideTarget.Visibility = Visibility.Collapsed;
            }
        }

        public async void ShowMenuSubContent()
        {
            var hideTarget = GetTemplateChild("MenuMainPageContent") as FrameworkElement;
            var showTarget = GetTemplateChild("MenuSubPageContent") as FrameworkElement;

            using (var releaser = await _MenuContentToggleLock.LockAsync())
            {
                showTarget.Visibility = Visibility.Visible;
                hideTarget.IsHitTestVisible = false;
                await Task.WhenAll(
                    showTarget.Fade(1.0f, MenuMainSubContentToggleAnimationDuration).Offset(0, duration:MenuMainSubContentToggleAnimationDuration).StartAsync(),
                    hideTarget.Fade(0.0f, MenuMainSubContentToggleAnimationDuration).Offset(-100, duration: MenuMainSubContentToggleAnimationDuration).StartAsync()
                    );
                showTarget.IsHitTestVisible = true;
                hideTarget.Visibility = Visibility.Collapsed;
            }
        }


        /// <summary>
        /// 検索を実行した際、モバイルやXboxOneのときは自動でメニューを閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SearchTextBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var splitView = GetTemplateChild("ContentSplitView") as SplitView;
            if (splitView != null)
            {
                if (splitView.DisplayMode == SplitViewDisplayMode.Overlay)
                {
                    splitView.IsPaneOpen = false;
                }
            }
        }
    }
}

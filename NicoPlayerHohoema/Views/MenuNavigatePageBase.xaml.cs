using Microsoft.Toolkit.Uwp.UI.Animations;
using NicoPlayerHohoema.Helpers;
using Prism.Events;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace NicoPlayerHohoema.Views
{
	public sealed partial class MenuNavigatePageBase : ContentControl
	{
        CoreDispatcher _UIDispatcher;
        public MenuNavigatePageBase()
		{
			this.InitializeComponent();

            this.Loading += MenuNavigatePageBase_Loading;
            this.Loaded += MenuNavigatePageBase_Loaded;
        }

       
        private void MenuNavigatePageBase_Loading(FrameworkElement sender, object args)
		{
			ForceChangeChildDataContext();

        }

        private void MenuNavigatePageBase_Loaded(object sender, RoutedEventArgs e)
        {
            _UIDispatcher = Dispatcher;
        }
        

        private void ForceChangeChildDataContext()
		{
			if (Parent is FrameworkElement && Content is FrameworkElement)
			{
				var depContent = Content as FrameworkElement;
				depContent.DataContext = (Parent as FrameworkElement).DataContext;
			}
		}

        public Frame Frame { get; private set; }

        protected override void OnApplyTemplate()
        {
            Frame = GetTemplateChild("PlayerFrame") as Frame;

            {
                var frameFacade = new FrameFacadeAdapter(Frame);

                var sessionStateService = new SessionStateService();

                var ns = new FrameNavigationService(frameFacade
                    , (pageToken) =>
                    {
                        if (pageToken == nameof(Views.VideoPlayerPage))
                        {
                            return typeof(Views.VideoPlayerPage);
                        }
                        else if (pageToken == nameof(Views.LivePlayerPage))
                        {
                            return typeof(Views.LivePlayerPage);
                        }
                        else
                        {
                            return typeof(Views.BlankPage);
                        }
                    }, sessionStateService);

                (DataContext as ViewModels.MenuNavigatePageBaseViewModel).SetNavigationService(ns);
            }

            base.OnApplyTemplate();
        }





        AsyncLock _MenuContentToggleLock = new AsyncLock();


        private async void ToggleDisplayContent(FrameworkElement showTarget, FrameworkElement hideTarget)
        {
            using (var releaser = await _MenuContentToggleLock.LockAsync())
            {
                showTarget.Visibility = Visibility.Visible;
                hideTarget.IsHitTestVisible = false;
                await Task.WhenAll(
                    showTarget.Fade(1.0f, 175).StartAsync(),
                    hideTarget.Fade(0.0f, 175).StartAsync()
                    );
                showTarget.IsHitTestVisible = true;
                hideTarget.Visibility = Visibility.Collapsed;
            }
        }
        public void ShowMenuMainContent()
        {
            var menuMainContent = GetTemplateChild("MenuMainPageContent") as FrameworkElement;
            var menuSubContent = GetTemplateChild("MenuSubPageContent") as FrameworkElement;

            ToggleDisplayContent(menuMainContent, menuSubContent);
        }

        public void ShowMenuSubContent()
        {
            var menuMainContent = GetTemplateChild("MenuMainPageContent") as FrameworkElement;
            var menuSubContent = GetTemplateChild("MenuSubPageContent") as FrameworkElement;

            ToggleDisplayContent(menuSubContent, menuMainContent);
        }
    }
}

using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

            UINavigationManager.Pressed += UINavigationManager_Pressed;

            var pane = GetTemplateChild("PaneLayout") as FrameworkElement;

            pane.GotFocus += RootLayout_GotFocus;
            pane.LostFocus += RootLayout_LostFocus;
        }

        private bool _IsFocusing = false;

        private int LeftInputCount = 0;

        private void RootLayout_GotFocus(object sender, RoutedEventArgs e)
        {
            _IsFocusing = true;
        }

        private void RootLayout_LostFocus(object sender, RoutedEventArgs e)
        {
            _IsFocusing = false;
        }



        private async void UINavigationManager_Pressed(UINavigationManager sender, UINavigationButtons buttons)
        {
            await _UIDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
            {
                var splitView = GetTemplateChild("ContentSplitView") as SplitView;
                if (_IsFocusing && buttons == UINavigationButtons.Left)
                {
                    LeftInputCount++;
                    if (LeftInputCount > 1)
                    {
                        splitView.IsPaneOpen = true;
                    }
                }
                else
                {
                    LeftInputCount = 0;

                    if (buttons == UINavigationButtons.Accept || buttons == UINavigationButtons.Right)
                    {
                        splitView.IsPaneOpen = false;
                    }
                }
            });
            
            
        }

        private void ForceChangeChildDataContext()
		{
			if (Parent is FrameworkElement && Content is FrameworkElement)
			{
				var depContent = Content as FrameworkElement;
				depContent.DataContext = (Parent as FrameworkElement).DataContext;
			}
		}
	}
}

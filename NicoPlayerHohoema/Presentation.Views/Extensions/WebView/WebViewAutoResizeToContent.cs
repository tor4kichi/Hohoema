using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Presentation.Views.Behaviors
{
	// ここを参考にしました
	// http://www.jasonpoon.ca/2015/01/08/resizing-webview-to-its-content/

	public class WebViewAutoResizeToContent : Behavior<WebView>
	{
		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		}

		private void AssociatedObject_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
            this.AssociatedObject.NavigationCompleted += AssociatedObject_NavigationCompleted;
		}

        private async void AssociatedObject_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            try
            {
                var heightString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
                if (int.TryParse(heightString, out var height))
                {
                    this.AssociatedObject.Height = height;
                }
            }
            catch { }

            try
            {
                var widthString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
                if (int.TryParse(widthString, out var width))
                {
                    this.AssociatedObject.Width = width;
                }
            }
            catch { }
        }

        private async void AssociatedObject_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
            try
            {
                var heightString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
                if (int.TryParse(heightString, out var height))
                {
                    this.AssociatedObject.Height = height;
                }
            }
            catch { }

            try
            {
                var widthString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
                if (int.TryParse(widthString, out var width))
                {
                    this.AssociatedObject.Width = width;
                }
            }
            catch { }
        }

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.LoadCompleted -= AssociatedObject_LoadCompleted;
		}

		

	}
}

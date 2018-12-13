using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
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
            var heightString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
            int height;
            if (int.TryParse(heightString, out height))
            {
                this.AssociatedObject.Height = height;
            }

            var widthString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
            int width;
            if (int.TryParse(widthString, out width))
            {
                this.AssociatedObject.Width = width;
            }
        }

        private async void AssociatedObject_LoadCompleted(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			var heightString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollHeight.toString()" });
			int height;
			if (int.TryParse(heightString, out height))
			{
				this.AssociatedObject.Height = height;
			}

			var widthString = await this.AssociatedObject.InvokeScriptAsync("eval", new[] { "document.body.scrollWidth.toString()" });
			int width;
			if (int.TryParse(widthString, out width))
			{
				this.AssociatedObject.Width = width;
			}
		}

		protected override void OnDetaching()
		{
			base.OnDetaching();

			this.AssociatedObject.LoadCompleted -= AssociatedObject_LoadCompleted;
		}

		

	}
}

using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Behaviors
{

	public class WebViewNotifyUriClicked : Behavior<WebView>
	{

		#region Command Property

		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register("Command"
					, typeof(ICommand)
					, typeof(WebViewNotifyUriClicked)
					, new PropertyMetadata(default(ICommand))
				);

		public ICommand Command
		{
			get { return (ICommand)GetValue(CommandProperty); }
			set { SetValue(CommandProperty, value); }
		}

		#endregion


		protected override void OnAttached()
		{
			base.OnAttached();

			this.AssociatedObject.Loaded += AssociatedObject_Loaded;
		}

		private void AssociatedObject_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			this.AssociatedObject.NavigationCompleted += AssociatedObject_NavigationCompleted; ;
			this.AssociatedObject.NavigationStarting += AssociatedObject_NavigationStarting;
		}

		private void AssociatedObject_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
		{
			// 参考元：http://bluewatersoft.cocolog-nifty.com/blog/2013/10/windows-re-wind.html

			// テンポラリファイルからWebページを表示したとき、
			// 参考元の実装では同一ホスト判定を識別できなかったので
			// 条件を書き換えています。
			//			if (sender.Source.Host != args.Uri.Host)
			if (sender.Source.Segments[sender.Source.Segments.Length-1] != 
				args.Uri.Segments[args.Uri.Segments.Length - 1])
			{
				args.Cancel = true;

				var command = Command;
				if (command != null)
				{
					if (command.CanExecute(args.Uri))
					{
						command.Execute(args.Uri);
					}
				}
			}
		}

		private async void AssociatedObject_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
		{
			await this.AssociatedObject.InvokeScriptAsync("eval", new string[] {
@"
var anchors = document.getElementsByTagName('a');
for (var i = 0; i<anchors.length; i++) {
  anchors[i].target = '';
}"
			});
		}

		
		protected override void OnDetaching()
		{
			base.OnDetaching();
			this.AssociatedObject.NavigationCompleted -= AssociatedObject_NavigationCompleted; ;
			this.AssociatedObject.NavigationStarting -= AssociatedObject_NavigationStarting;

		}



	}
}

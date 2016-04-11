using Prism.Windows.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
		public MenuNavigatePageBase()
		{
			this.InitializeComponent();

			this.Loaded += MenuNavigatePageBase_Loaded;
		}

		private void MenuNavigatePageBase_Loaded(object sender, RoutedEventArgs e)
		{
			if (Parent is FrameworkElement && Content is FrameworkElement)
			{
				var depContent = Content as FrameworkElement;
				depContent.DataContext = (Parent as FrameworkElement).DataContext;
			}
		}
	}
}

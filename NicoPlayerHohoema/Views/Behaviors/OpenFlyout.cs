using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class OpenFlyout : Behavior<DependencyObject>, IAction
	{

		public object Execute(object sender, object parameter)
		{
			var flyout = FlyoutBase.GetAttachedFlyout((FrameworkElement)sender);
			if (flyout != null)
			{
				FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
			}

			return true;
		}
	}
}

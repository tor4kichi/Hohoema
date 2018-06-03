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
            var feSender = sender as FrameworkElement;
            var flyout = FlyoutBase.GetAttachedFlyout(feSender);
			if (flyout != null)
			{
                if (flyout is Windows.UI.Xaml.Controls.Flyout fo)
                {
                    if (fo.Content is FrameworkElement flyoutContent)
                    {
                        flyoutContent.DataContext = feSender.DataContext;
                    }
                }
                else if (flyout is Windows.UI.Xaml.Controls.MenuFlyout mf)
                {
                    foreach (var menuFlyoutItem in mf.Items)
                    {
                        menuFlyoutItem.DataContext = feSender.DataContext;
                    }
                }

                FlyoutBase.ShowAttachedFlyout(feSender);
			}

			return true;
		}
	}
}

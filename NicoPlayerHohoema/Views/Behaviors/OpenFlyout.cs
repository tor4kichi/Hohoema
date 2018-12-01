using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace NicoPlayerHohoema.Views.Behaviors
{
	public class OpenFlyout : Behavior<DependencyObject>, IAction
	{
        public static readonly DependencyProperty TargetFlyoutOwnerProperty =
           DependencyProperty.Register(nameof(TargetFlyoutOwner)
                   , typeof(FrameworkElement)
                   , typeof(OpenFlyout)
                   , new PropertyMetadata(default(FrameworkElement))
               );

        public FrameworkElement TargetFlyoutOwner
        {
            get { return (FrameworkElement)GetValue(TargetFlyoutOwnerProperty); }
            set { SetValue(TargetFlyoutOwnerProperty, value); }
        }


        public object Execute(object sender, object parameter)
		{
            var feSender = TargetFlyoutOwner ?? sender as FrameworkElement;
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

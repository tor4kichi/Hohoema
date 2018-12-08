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



        public static readonly DependencyProperty TargetFlyoutProperty =
           DependencyProperty.Register(nameof(TargetFlyout)
                   , typeof(FlyoutBase)
                   , typeof(OpenFlyout)
                   , new PropertyMetadata(default(FlyoutBase))
               );

        public FlyoutBase TargetFlyout
        {
            get { return (FlyoutBase)GetValue(TargetFlyoutProperty); }
            set { SetValue(TargetFlyoutProperty, value); }
        }


        public static readonly DependencyProperty DataContextProperty =
           DependencyProperty.Register(nameof(DataContext)
                   , typeof(object)
                   , typeof(OpenFlyout)
                   , new PropertyMetadata(default(object))
               );

        public object DataContext
        {
            get { return (object)GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        public static readonly DependencyProperty ShowAtProperty =
           DependencyProperty.Register(nameof(ShowAt)
                   , typeof(FrameworkElement)
                   , typeof(OpenFlyout)
                   , new PropertyMetadata(default(FrameworkElement))
               );

        public FrameworkElement ShowAt
        {
            get { return (FrameworkElement)GetValue(ShowAtProperty); }
            set { SetValue(ShowAtProperty, value); }
        }



        public object Execute(object sender, object parameter)
		{
            var feSender = TargetFlyoutOwner ?? sender as FrameworkElement;
            var flyout = TargetFlyout ?? FlyoutBase.GetAttachedFlyout(feSender);
            var dataContext = DataContext ?? feSender.DataContext;

            if (flyout != null)
			{
                if (flyout is Windows.UI.Xaml.Controls.Flyout fo)
                {
                    if (fo.Content is FrameworkElement flyoutContent)
                    {
                        flyoutContent.DataContext = dataContext;
                    }
                }
                else if (flyout is Windows.UI.Xaml.Controls.MenuFlyout mf)
                {
                    foreach (var menuFlyoutItem in mf.Items)
                    {
                        menuFlyoutItem.DataContext = dataContext;
                    }
                }

                flyout.ShowAt(ShowAt ?? feSender);               
            }

            return true;
		}
	}
}

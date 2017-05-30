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
    public class ScrollAction : DependencyObject, IAction
    {
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(nameof(Target)
                    , typeof(object)
                    , typeof(ScrollAction)
                    , new PropertyMetadata(default(object))
                );

        public object Target
        {
            get { return GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }


        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation)
                    , typeof(Windows.UI.Xaml.Controls.Orientation)
                    , typeof(ScrollAction)
                    , new PropertyMetadata(default(Windows.UI.Xaml.Controls.Orientation))
                );

        public Windows.UI.Xaml.Controls.Orientation Orientation
        {
            get { return (Windows.UI.Xaml.Controls.Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }


        public static readonly DependencyProperty ScrollDistanceProperty =
            DependencyProperty.Register(nameof(ScrollDistance)
                    , typeof(double)
                    , typeof(ScrollAction)
                    , new PropertyMetadata(default(double))
                );

        public double ScrollDistance
        {
            get { return (double)GetValue(ScrollDistanceProperty); }
            set { SetValue(ScrollDistanceProperty, value); }
        }

        public object Execute(object sender, object parameter)
        {
            ScrollViewer sv = null;
            if (Target is ScrollViewer)
            {
                sv = Target as ScrollViewer;
            }
            else if (Target is ItemsControl)
            {
                sv = VisualTreeExtention.FindFirstChild<ScrollViewer>(Target as FrameworkElement);
            }

            if (sv == null) { return false; }

            if (Orientation == Orientation.Horizontal)
            {
                sv.ChangeView(sv.HorizontalOffset + ScrollDistance, null, null);
            }
            else
            {
                sv.ChangeView(null, sv.VerticalOffset + ScrollDistance, null);
            }

            return true;
        }
    }
}

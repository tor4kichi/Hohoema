using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Views.Actions
{
    public sealed class ScrollViewerVerticalPositionSettingsAction : Behavior<DependencyObject>, IAction
    {
        public object Execute(object sender, object parameter)
        {
            if (sender is FrameworkElement element)
            {
                var scrollViewer = Target?.FindFirstChild<ScrollViewer>();
                return scrollViewer?.ChangeView(null, VerticalOffset, null, false) ?? false;
            }
            else
            {
                return false;
            }
        }


        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(nameof(Target)
                , typeof(FrameworkElement)
                , typeof(ScrollViewerVerticalPositionSettingsAction)
                , new PropertyMetadata(null)
            );

        public FrameworkElement Target
        {
            get { return (FrameworkElement)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }


        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register(nameof(VerticalOffset)
                , typeof(double)
                , typeof(ScrollViewerVerticalPositionSettingsAction)
                , new PropertyMetadata(0.0)
            );

        public double VerticalOffset
        {
            get { return (double)GetValue(VerticalOffsetProperty); }
            set { SetValue(VerticalOffsetProperty, value); }
        }
    }
}

#nullable enable
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Controls.LiteInAppNotification;

public partial class LiteInAppNotification : ContentControl
{
    public TimeSpan Interval
    {
        get { return (TimeSpan)GetValue(IntervalProperty); }
        set { SetValue(IntervalProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Interval.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty IntervalProperty =
        DependencyProperty.Register("Interval", typeof(TimeSpan), typeof(LiteInAppNotification), new PropertyMetadata(TimeSpan.FromSeconds(0.5)));




    public TimeSpan AnimationDuration
    {
        get { return (TimeSpan)GetValue(AnimationDurationProperty); }
        set { SetValue(AnimationDurationProperty, value); }
    }

    // Using a DependencyProperty as the backing store for AnimationDuration.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.Register("AnimationDuration", typeof(TimeSpan), typeof(LiteInAppNotification), new PropertyMetadata(TimeSpan.Zero));



}

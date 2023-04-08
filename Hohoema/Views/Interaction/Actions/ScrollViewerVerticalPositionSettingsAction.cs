using Microsoft.Xaml.Interactivity;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hohoema.Views.Actions;

public sealed class ScrollViewerVerticalPositionSettingsAction : Behavior<DependencyObject>, IAction
{
    public object Execute(object sender, object parameter)
    {
        if (sender is FrameworkElement element)
        {
            var dispatcher = Dispatcher;
            var delay = Delay;
            if (delay > TimeSpan.Zero)
            {
                Task.Delay(delay)
                    .ContinueWith(async _ =>
                    {
                        await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                        {
                            foreach (var i in Enumerable.Range(0, 100))
                            {
                                if (SetScrollView())
                                {
                                    break;
                                }

                                await Task.Delay(10);
                            }
                            
                        });
                    });
            }
            else
            {
                SetScrollView();
            }

            return false; 
        }
        else
        {
            return false;
        }
    }

    private bool SetScrollView()
    {
        var scrollViewer = Target?.FindFirstChild<ScrollViewer>();
        return scrollViewer?.ChangeView(null, VerticalOffset, null, !WithAnimation) ?? false;
    }

    public static readonly DependencyProperty DelayProperty =
        DependencyProperty.Register(nameof(Delay)
            , typeof(TimeSpan)
            , typeof(ScrollViewerVerticalPositionSettingsAction)
            , new PropertyMetadata(TimeSpan.Zero)
        );

    public TimeSpan Delay
    {
        get { return (TimeSpan)GetValue(DelayProperty); }
        set { SetValue(DelayProperty, value); }
    }

    public static readonly DependencyProperty WithAnimationProperty =
        DependencyProperty.Register(nameof(WithAnimation)
            , typeof(bool)
            , typeof(ScrollViewerVerticalPositionSettingsAction)
            , new PropertyMetadata(true)
        );

    public bool WithAnimation
    {
        get { return (bool)GetValue(WithAnimationProperty); }
        set { SetValue(WithAnimationProperty, value); }
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

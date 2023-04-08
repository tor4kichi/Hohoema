#nullable enable
using Windows.UI.Xaml;

namespace Hohoema.Views.Extensions;

public sealed partial class FlyoutBase : DependencyObject
{
    public static readonly DependencyProperty CustomContextProperty =
       DependencyProperty.RegisterAttached(
           "CustomContext",
           typeof(object),
           typeof(FlyoutBase),
           new PropertyMetadata(false, CustomContextPropertyChanged)
       );

    public static void SetCustomContext(UIElement element, object value)
    {
        element.SetValue(CustomContextProperty, value);
    }
    public static object GetCustomContext(UIElement element)
    {
        return (object)element.GetValue(CustomContextProperty);
    }


    private static void CustomContextPropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        if (s is Windows.UI.Xaml.Controls.Primitives.FlyoutBase flyout)
        {
            if (e.NewValue != null)
            {
                flyout.Opening += Flyout_Opening;
            }
            else
            {
                flyout.Opening -= Flyout_Opening;
            }
        }
    }

    private static void Flyout_Opening(object sender, object e)
    {
        if (sender is Windows.UI.Xaml.Controls.Primitives.FlyoutBase flyout)
        {

        }
    }
}

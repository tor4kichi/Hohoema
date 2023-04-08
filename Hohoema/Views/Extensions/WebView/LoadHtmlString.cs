using Windows.UI.Xaml;

namespace Hohoema.Views.Extensions;

public partial class WebView
{
    // "HtmlString" attached property for a WebView
    public static readonly DependencyProperty HtmlStringProperty =
       DependencyProperty.RegisterAttached("HtmlString", typeof(string), typeof(WebView), new PropertyMetadata("", OnHtmlStringChanged));

    // Getter and Setter
    public static string GetHtmlString(DependencyObject obj) { return (string)obj.GetValue(HtmlStringProperty); }
    public static void SetHtmlString(DependencyObject obj, string value) { obj.SetValue(HtmlStringProperty, value); }

    // Handler for property changes in the DataContext : set the WebView
    private static void OnHtmlStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var wv = d as Windows.UI.Xaml.Controls.WebView;
        if (wv != null && e.NewValue != null)
        {
            wv.NavigateToString((string)e.NewValue);
        }
    }
}

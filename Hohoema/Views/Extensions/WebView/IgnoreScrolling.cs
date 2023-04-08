using System;
using Windows.UI.Xaml;

namespace Hohoema.Views.Extensions;

public partial class WebView : DependencyObject
{
    public static readonly DependencyProperty IgnoreScrollingProperty =
        DependencyProperty.RegisterAttached(
            "IgnoreScrolling",
            typeof(bool),
            typeof(WebView),
            new PropertyMetadata(false, ItemContextFlyoutTemplatePropertyChanged)
        );

    public static void SetIgnoreScrolling(UIElement element, bool value)
    {
        element.SetValue(IgnoreScrollingProperty, value);
    }
    public static bool GetIgnoreScrolling(UIElement element)
    {
        return (bool)element.GetValue(IgnoreScrollingProperty);
    }

    private static void ItemContextFlyoutTemplatePropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
    {
        if (s is Windows.UI.Xaml.Controls.WebView webView && (bool)e.NewValue == true)
        {
            webView.NavigationCompleted -= Browser_NavigationCompleted;
            webView.NavigationCompleted += Browser_NavigationCompleted;
        }
    }

    const string DisableScrollingJs = @"function RemoveScrolling()
                              {
                                  var styleElement = document.createElement('style');
                                  var styleText = 'body, html { overflow: hidden; }'
                                  var headElements = document.getElementsByTagName('head');
                                  styleElement.type = 'text/css';
                                  if (headElements.length == 1)
                                  {
                                      headElements[0].appendChild(styleElement);
                                  }
                                  else if (document.head)
                                  {
                                      document.head.appendChild(styleElement);
                                  }
                                  if (styleElement.styleSheet)
                                  {
                                      styleElement.styleSheet.cssText = styleText;
                                  }
                              }";

    private static async void Browser_NavigationCompleted(Windows.UI.Xaml.Controls.WebView sender, Windows.UI.Xaml.Controls.WebViewNavigationCompletedEventArgs args)
    {
        await sender.InvokeScriptAsync("eval", new[] { DisableScrollingJs });
    }

}

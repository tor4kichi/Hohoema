using Reactive.Bindings;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Dialogs;

public sealed partial class NiconicoTwoFactorAuthDialog : ContentDialog, IDisposable
{

    public static readonly DependencyProperty WebViewContentProperty =
        DependencyProperty.Register(
            nameof(WebViewContent),
            typeof(object),
            typeof(NiconicoTwoFactorAuthDialog),
            new PropertyMetadata(null, (x, y) => 
            {
                var @this = x as NiconicoTwoFactorAuthDialog;
                if (@this.WebViewContent is HttpResponseMessage res)
                {
                    using var m = new Windows.Web.Http.HttpRequestMessage(Windows.Web.Http.HttpMethod.Get, res.Headers.Location);
                    foreach (var h in res.RequestMessage.Headers)
                    {
                        m.Headers.Add(h.Key, string.Join(' ', h.Value));
                    }

                    var cookies = res.Headers.GetValues("Set-Cookie");
                    m.Headers.Add("Cookies", cookies.ElementAt(0));

                    m.Headers.Host = new Windows.Networking.HostName("account.nicovideo.jp");
                    m.Headers.Add("Upgrade-Insecure-Requests", "1");
                    m.Headers.Referer = new Uri("https://account.nicovideo.jp/login?site=niconico");

                    @this.WebView.NavigateWithHttpRequestMessage(m);
                }
            })
            );

    public object WebViewContent
    {
        get { return (object)GetValue(WebViewContentProperty); }
        set { SetValue(WebViewContentProperty, value); }
    }




    public NiconicoTwoFactorAuthDialog()
    {
        this.InitializeComponent();
    }


    ReactiveProperty<bool> NowLoading { get; } = new ReactiveProperty<bool>(false);
    bool isCompleted = false;
    const string TwoFactorAuthSite = @"https://account.nicovideo.jp/mfa";
    private void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
    {
        Debug.WriteLine(e.Uri);
        Debug.WriteLine(e.WebErrorStatus);

        Hide();

        NowLoading.Value = false;
    }

    private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
    {
        NowLoading.Value = true;

        if (args.Uri != null && !args.Uri.OriginalString.StartsWith(TwoFactorAuthSite))
        {
//                isCompleted = true;
        }

        Debug.WriteLine(args.Uri);
    }

    private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
    {
        if (isCompleted)
        {
            Hide();
        }
        else
        {
            NowLoading.Value = false;

            WebView.Focus(FocusState.Programmatic);
        }
    }

    public void Dispose()
    {
        NowLoading.Dispose();
    }
}

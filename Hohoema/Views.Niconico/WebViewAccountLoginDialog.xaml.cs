using Hohoema.Views.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Hohoema.Views.Niconico;

public sealed partial class WebViewAccountLoginDialog : ContentDialog
{
    public WebViewAccountLoginDialog()
    {
        this.InitializeComponent();

        Opened += WebViewAccountLoginDialog_Opened;
        Closed += WebViewAccountLoginDialog_Closed;
        _ = MyWebView.EnsureCoreWebView2Async();
    }

    private void WebViewAccountLoginDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
    {
        MyWebView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
        MyWebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
    }

    // Note: ダイアログを開いた初回のみクリックできないエリアが表示される問題への対処。
    static bool _isFirstOpen = true;
    private async void WebViewAccountLoginDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        if (_isFirstOpen)
        {
            Hide();
            _isFirstOpen = false;
        }
    }

    public WebView2 GetWebView2() => MyWebView;

    public async Task<ContentDialogResult> ShowAsync()
    {
        try
        {
            if (_isFirstOpen)
            {
                await base.ShowAsync();
                await MyWebView.EnsureCoreWebView2Async();
            }
            _completed = false;
            // インストール済みの WebView2 ランタイムのバージョンを取得
            string versionInfo = CoreWebView2Environment.GetAvailableBrowserVersionString();
            Debug.WriteLine(versionInfo);
            // 戻り値の例: "124.0.2478.67"
            MyWebView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
            MyWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            MyWebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
            MyWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            MyWebView.CoreWebView2.Navigate("https://account.nicovideo.jp/spa/login/index.html?sec=header_pc&redirect_uri=https%3A%2F%2Fwww.nicovideo.jp%2F");
            MyWebView.Focus(FocusState.Keyboard);
            return await base.ShowAsync();
        }
        catch
        {
            throw;
        }
    }    



    bool _completed = false;
    private void CoreWebView2_NavigationStarting(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
    {
        Debug.WriteLine(args.Uri);

        if (!args.Uri.StartsWith("https://account.nicovideo.jp/spa/login/index.html"))
        {
            _completed = true;            
        }       
    }

    private void CoreWebView2_NavigationCompleted(Microsoft.Web.WebView2.Core.CoreWebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
    {
        MyWebView.Focus(FocusState.Keyboard);
        Debug.WriteLine(args.HttpStatusCode);        
        if (_completed)
        {
            Hide();
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
    }
}

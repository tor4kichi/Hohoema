using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace NicoPlayerHohoema.Dialogs
{
    public sealed partial class NiconicoTwoFactorAuthDialog : ContentDialog
    {

        public static readonly DependencyProperty WebViewContentProperty =
            DependencyProperty.Register(
                nameof(WebViewContent),
                typeof(object),
                typeof(NiconicoTwoFactorAuthDialog),
                new PropertyMetadata(null, (x, y) => 
                {
                    var @this = x as NiconicoTwoFactorAuthDialog;
                    if (@this.WebViewContent is HttpRequestMessage)
                    {
                        @this.WebView.NavigateWithHttpRequestMessage(@this.WebViewContent as HttpRequestMessage);
                    }
                    else if (@this.WebViewContent is Uri)
                    {
                        @this.WebView.Navigate(@this.WebViewContent as Uri);
                    }
                    else if (@this.WebViewContent is string)
                    {
                        @this.WebView.NavigateToString(@this.WebViewContent as string);
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
                isCompleted = true;
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
    }
}

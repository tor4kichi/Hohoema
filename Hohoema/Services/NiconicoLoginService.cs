#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Notification;
using Hohoema.Views.Dialogs;
using Hohoema.Views.Niconico;
using I18NPortable;
using NiconicoToolkit;
using NiconicoToolkit.Account;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.Services.Niconico.Account;


public sealed class NiconicoLoginService : IDisposable,
    IRecipient<NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage>
{
    public NiconicoLoginService(
        NiconicoSession niconicoSession,
        NoUIProcessScreenContext noProcessUIScreenContext,
        DialogService dialogService,
        NotificationService notificationService,
        IMessenger messenger
        )
    {
        NiconicoSession = niconicoSession;
        _noProcessUIScreenContext = noProcessUIScreenContext;
        DialogService = dialogService;
        NotificationService = notificationService;
        _messenger = messenger;

        // 二要素認証を求められるケースに対応する
        // 起動後の自動ログイン時に二要素認証を要求されることもある
        _messenger.Register<NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage>(this);
        _loginDialog = new();
    }


    public NiconicoSession NiconicoSession { get; }
    public DialogService DialogService { get; }
    public NotificationService NotificationService { get; }

    private RelayCommand _LoginCommand;
    private readonly NoUIProcessScreenContext _noProcessUIScreenContext;
    private readonly IMessenger _messenger;

    public RelayCommand LoginCommand => _LoginCommand
        ?? (_LoginCommand = new RelayCommand(async () => 
        {
            try
            {
                var currentView = CoreApplication.GetCurrentView();
                if (currentView.IsMain)
                {
                    await _noProcessUIScreenContext.StartNoUIWork("ログイン中...",
                        () => StartLoginSequence().AsAsyncAction()
                        );
                }
                else
                {
                    await StartLoginSequence();
                }
            }
            catch (OperationCanceledException)
            {
                await NiconicoSession.SignOutAsync();
            }
        }));

    WebViewAccountLoginDialog _loginDialog;

    public async Task<NiconicoSessionStatus> TryLoginAsync()
    {
        await SyncToWindowsHttpClientAsync(_loginDialog.GetWebView2(), new Uri(NiconicoUrls.NicoHomePageUrl), NiconicoSession.ToolkitContext.HttpClient);
        return await NiconicoSession.CheckSignedInStatus();
    }

    private async Task StartLoginSequence()
    {
        var webView = _loginDialog.GetWebView2();
        webView.CoreWebView2.CookieManager.DeleteAllCookies();
        NiconicoSession.ToolkitContext.HttpClient.DefaultRequestHeaders.Cookie.Clear();
        var result = await _loginDialog.ShowAsync();

        await SyncToWindowsHttpClientAsync(webView, new Uri(NiconicoUrls.NicoHomePageUrl), NiconicoSession.ToolkitContext.HttpClient);
        var currentStatus = await NiconicoSession.CheckSignedInStatus();
        Debug.WriteLine(currentStatus);
    }

    public async Task SyncToWindowsHttpClientAsync(Microsoft.UI.Xaml.Controls.WebView2 webView, Uri targetUri, HttpClient httpClient)
    {
        var filter = new HttpBaseProtocolFilter();
        var winCookieManager = filter.CookieManager;        

        await webView.EnsureCoreWebView2Async();
        var webViewCookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync(targetUri.AbsoluteUri);

        httpClient.DefaultRequestHeaders.Cookie.Clear();
        foreach (var wvCookie in webViewCookies)
        {
            var winCookie = new HttpCookiePairHeaderValue(wvCookie.Name, wvCookie.Value);

            Debug.WriteLine(winCookie.ToString());

            // Windows.Web.Http の CookieManager に設定
            //winCookieManager.SetCookie(winCookie);            
            httpClient.DefaultRequestHeaders.Cookie.Add(winCookie);
        }
    }

    public async Task<HttpClient> SyncToWindowsHttpClientAsync(Microsoft.UI.Xaml.Controls.WebView2 webView, Uri targetUri)
    {
        var filter = new HttpBaseProtocolFilter();
        var winCookieManager = filter.CookieManager;

        await webView.EnsureCoreWebView2Async();
        var webViewCookies = await webView.CoreWebView2.CookieManager.GetCookiesAsync(targetUri.AbsoluteUri);

        //httpClient.DefaultRequestHeaders.Cookie.Clear();
        foreach (var wvCookie in webViewCookies)
        {
            var cookie = new HttpCookiePairHeaderValue(wvCookie.Name, wvCookie.Value);

            Debug.WriteLine(cookie.ToString());

            // Windows.Web.Http の CookieManager に設定
            //winCookieManager.SetCookie(winCookie);            
            //httpClient.DefaultRequestHeaders.Cookie.Add(cookie);

            var winCookie = new HttpCookie(wvCookie.Name, wvCookie.Domain, wvCookie.Path)
            {
                Value = wvCookie.Value,
                HttpOnly = wvCookie.IsHttpOnly,
                Secure = wvCookie.IsSecure
            };

            // Windows.Web.Http の CookieManager に設定
            winCookieManager.SetCookie(winCookie);
        }
        return new HttpClient(filter);
    }

    async Task<NiconicoSessionLoginRequireTwoFactorAuthResponse> ShowTwoFactorNumberInputDialogAsync(Uri uri)
    {
        await Task.Delay(250);

        var dialog = new TwoFactorAuthDialog()
        {
            IsTrustedDevice = true,
            DeviceName = "Hohoema_App"
        };

        var result = await dialog.ShowAsync();

        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
        {
            return new NiconicoSessionLoginRequireTwoFactorAuthResponse(dialog.CodeText, dialog.IsTrustedDevice, dialog.DeviceName);
        }
        else
        {
            return null;
        }
    }

    public void Dispose()
    {
    }

    void IRecipient<NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage>.Receive(NiconicoSessionLoginRequireTwoFactorAsyncRequestMessage message)
    {
        message.Reply(ShowTwoFactorNumberInputDialogAsync(message.TwoFactorAuthLocation));

        // ログインに失敗していた場合はダイアログを再表示
        /*
        if (!NiconicoSession.IsLoggedIn && !NiconicoSession.ServiceStatus.IsOutOfService())
        {
            LoginCommand.Execute();
        }
        */
    }
}

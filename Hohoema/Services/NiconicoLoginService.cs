﻿#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Helpers;
using Hohoema.Models.Niconico;
using Hohoema.Models.Notification;
using Hohoema.Views.Dialogs;
using I18NPortable;
using NiconicoToolkit.Account;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
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

        NiconicoSession.LogIn += NiconicoSession_LogIn;
        NiconicoSession.LogInFailed += NiconicoSession_LogInFailed;
        NiconicoSession.LogOut += NiconicoSession_LogOut;
    }

    private void NiconicoSession_LogInFailed(object sender, NiconicoSessionLoginErrorEventArgs e)
    {
        _twoFactorAuthLoginCts?.SetResult(0);
    }

    private void NiconicoSession_LogOut(object sender, EventArgs e)
    {
        
    }

    private void NiconicoSession_LogIn(object sender, NiconicoSessionLoginEventArgs e)
    {
        _twoFactorAuthLoginCts?.SetResult(0);
    }



    TaskCompletionSource<long> _twoFactorAuthLoginCts;

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

            }
            finally
            {
                
            }

        }));

    private async Task StartLoginSequence()
    {
        var dialog = new Dialogs.NiconicoLoginDialog();

        var currentStatus = await NiconicoSession.CheckSignedInStatus();
        if (currentStatus == NiconicoSessionStatus.ServiceUnavailable)
        {
            dialog.WarningText = "UnavailableNiconicoService".Translate();

            NotificationService.ShowLiteInAppNotification("UnavailableNiconicoService".Translate(), DisplayDuration.MoreAttention, Windows.UI.Xaml.Controls.Symbol.Important);
        }

        var account = await AccountManager.GetPrimaryAccount();
        if (account != null)
        {
            dialog.Mail = account.Item1;
            dialog.Password = account.Item2;

            dialog.IsRememberPassword = true;
        }

        bool isLoginSuccess = false;
        bool isCanceled = false;
        while (!isLoginSuccess)
        {
            var result = await dialog.ShowAsync();
            if (result != Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                isCanceled = true;
                break;
            }

            dialog.WarningText = string.Empty;

            _twoFactorAuthLoginCts = new TaskCompletionSource<long>();
            var loginResult = await NiconicoSession.SignIn(dialog.Mail, dialog.Password, true);

            if (loginResult == NiconicoSessionStatus.RequireTwoFactorAuth)
            {
                await _twoFactorAuthLoginCts.Task;

                loginResult = await NiconicoSession.CheckSignedInStatus();
            }

            _twoFactorAuthLoginCts = null;
            
            if (loginResult == NiconicoSessionStatus.ServiceUnavailable)
            {
                // サービス障害中
                // 何か通知を出す？
                NotificationService.ShowLiteInAppNotification("UnavailableNiconicoService".Translate(), DisplayDuration.MoreAttention, Windows.UI.Xaml.Controls.Symbol.Important);
                break;
            }
            else if (loginResult == NiconicoSessionStatus.Failed)
            {
                dialog.WarningText = "LoginFailed_WrongMailOrPassword".Translate();
            }

            isLoginSuccess = loginResult == NiconicoSessionStatus.Success;
        }

        // ログインを選択していた場合にのみアカウント情報を更新する
        // （キャンセル時は影響を発生させない）
        if (!isCanceled)
        {
            if (account != null)
            {
                AccountManager.RemoveAccount(account.Item1);
            }

            AccountManager.SetPrimaryAccountId(dialog.Mail);
            if (dialog.IsRememberPassword)
            {
                await AccountManager.AddOrUpdateAccount(dialog.Mail, dialog.Password);
            }
        }
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

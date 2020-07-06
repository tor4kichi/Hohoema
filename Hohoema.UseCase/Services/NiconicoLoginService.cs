using I18NPortable;
using Hohoema.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Hohoema.Models.Niconico;
using Hohoema.UseCase.Services;
using Hohoema.Models.Niconico.Account;
using Hohoema.UseCase.Events;

namespace Hohoema.Services
{
    public sealed class NiconicoLoginService : IDisposable
    {
        public NiconicoLoginService(
            NiconicoSession niconicoSession,
            NoUIProcessScreenContext noProcessUIScreenContext,
            INiconicoTwoFactorAuthDialogService twoFactorAuthDialogService,
            INiconicoLoginDialogService loginDialogService,
            IInAppNotificationService notificationService
            )
        {
            NiconicoSession = niconicoSession;
            _noProcessUIScreenContext = noProcessUIScreenContext;
            _twoFactorAuthDialogService = twoFactorAuthDialogService;
            _loginDialogService = loginDialogService;
            NotificationService = notificationService;

            // 二要素認証を求められるケースに対応する
            // 起動後の自動ログイン時に二要素認証を要求されることもある
            NiconicoSession.RequireTwoFactorAuth += NiconicoSession_RequireTwoFactorAuth;
        }

        public NiconicoSession NiconicoSession { get; }
        public INiconicoTwoFactorAuthDialogService DialogService { get; }
        public IInAppNotificationService NotificationService { get; }

        private DelegateCommand _LoginCommand;
        private readonly NoUIProcessScreenContext _noProcessUIScreenContext;
        private readonly INiconicoTwoFactorAuthDialogService _twoFactorAuthDialogService;
        private readonly INiconicoLoginDialogService _loginDialogService;

        public DelegateCommand LoginCommand => _LoginCommand
            ?? (_LoginCommand = new DelegateCommand(async () => 
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
                finally
                {
                    
                }

            }));


        private async Task StartLoginSequence()
        {
            string mail = string.Empty;
            string password = string.Empty;
            bool isRememberPassword = false;
            string warningText = string.Empty;


            var currentStatus = await NiconicoSession.CheckSignedInStatus();
            if (currentStatus == NiconicoSignInStatus.ServiceUnavailable)
            {
                warningText = "UnavailableNiconicoService".Translate();

                NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                {
                    Content = "UnavailableNiconicoService".Translate(),
                    ShowDuration = TimeSpan.FromSeconds(10)
                });
            }

            var account = await AccountManager.GetPrimaryAccount();
            if (account != null)
            {
                mail = account.Item1;
                password = account.Item2;

                isRememberPassword = true;
            }

            bool isLoginSuccess = false;
            bool isCanceled = false;
            while (!isLoginSuccess)
            {
                var result = await _loginDialogService.ShowLoginInputDialogAsync(
                    mail,
                    password,
                    isRememberPassword,
                    warningText
                    );

                if (result == null)
                {
                    isCanceled = true;
                    break;
                }

                mail = result.Mail;
                password = result.Password;
                isRememberPassword = result.IsRememberPassword;
                warningText = string.Empty;

                var loginResult = await NiconicoSession.SignIn(mail, password, true);
                if (loginResult == NiconicoSignInStatus.ServiceUnavailable)
                {
                    // サービス障害中
                    // 何か通知を出す？
                    NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "UnavailableNiconicoService".Translate()
                        , ShowDuration = TimeSpan.FromSeconds(10)
                    });
                    break;
                }

                

                if (loginResult == NiconicoSignInStatus.TwoFactorAuthRequired)
                {
                    break;
                }

                if (loginResult == NiconicoSignInStatus.Failed)
                {
                    warningText = "LoginFailed_WrongMailOrPassword".Translate();
                }

                isLoginSuccess = loginResult == NiconicoSignInStatus.Success;
            }

            // ログインを選択していた場合にのみアカウント情報を更新する
            // （キャンセル時は影響を発生させない）
            if (!isCanceled)
            {
                if (account != null)
                {
                    AccountManager.RemoveAccount(account.Item1);
                }

                if (isRememberPassword)
                {
                    await AccountManager.AddOrUpdateAccount(mail, password);
                    AccountManager.SetPrimaryAccountId(mail);
                }
                else
                {
                    AccountManager.SetPrimaryAccountId("");
                }
            }
        }

        private async void NiconicoSession_RequireTwoFactorAuth(object sender, NiconicoSessionLoginRequireTwoFactorAuthEventArgs e)
        {
            var currentView = CoreApplication.GetCurrentView();
            if (currentView.IsMain)
            {
                await _noProcessUIScreenContext.StartNoUIWork("NowProcessTwoFactorAuth".Translate(),
                        () => ShowTwoFactorNumberInputDialogAsync(e.Token).AsAsyncAction()
                        );
            }
            else
            {
                await ShowTwoFactorNumberInputDialogAsync(e.Token);
            }

            // ログイン処理が終わるぐらいまで待機して
            await Task.Delay(500);

            // ログインに失敗していた場合はダイアログを再表示
            if (!NiconicoSession.IsLoggedIn && !NiconicoSession.ServiceStatus.IsOutOfService())
            {
                LoginCommand.Execute();
            }
        }

        async Task<NiconicoSignInStatus> ShowTwoFactorNumberInputDialogAsync(TwoFactorAuthToken token)
        {
            var result = await _twoFactorAuthDialogService.ShowNiconicoTwoFactorLoginDialog(defaultTrustedDevice: true, defaultDeviceName: "Hohoema_App");
            if (result != null)
            {
                var mfaResult = await NiconicoSession.TryTwoFactorAuthAsync(
                    token, result.Code, result.IsTrustedDevice, result.DeviceName
                    );

                Debug.WriteLine(mfaResult);

                return mfaResult;
            }
            else
            {
                return NiconicoSignInStatus.Failed;
            }
        }

        public void Dispose()
        {
            NiconicoSession.RequireTwoFactorAuth -= NiconicoSession_RequireTwoFactorAuth;
        }
    }
}

using Mntone.Nico2;
using NicoPlayerHohoema.Dialogs;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Views.Dialogs;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Services
{
    public sealed class NiconicoLoginService : IDisposable
    {
        public NiconicoLoginService(
            Models.NiconicoSession niconicoSession,
            PageManager pageManager,
            DialogService dialogService,
            NotificationService notificationService
            )
        {
            NiconicoSession = niconicoSession;
            PageManager = pageManager;
            DialogService = dialogService;
            NotificationService = notificationService;

            // 二要素認証を求められるケースに対応する
            // 起動後の自動ログイン時に二要素認証を要求されることもある
            NiconicoSession.RequireTwoFactorAuth += NiconicoSession_RequireTwoFactorAuth;
        }

        public Models.NiconicoSession NiconicoSession { get; }
        public PageManager PageManager { get; }
        public DialogService DialogService { get; }
        public NotificationService NotificationService { get; }

        private DelegateCommand _LoginCommand;
        public DelegateCommand LoginCommand => _LoginCommand
            ?? (_LoginCommand = new DelegateCommand(async () => 
            {

                try
                {
                    var currentView = CoreApplication.GetCurrentView();
                    if (currentView.IsMain)
                    {
                        await PageManager.StartNoUIWork("ログイン中...",
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
            var dialog = new Dialogs.NiconicoLoginDialog();



            var currentStatus = await NiconicoSession.CheckSignedInStatus();
            if (currentStatus == Mntone.Nico2.NiconicoSignInStatus.ServiceUnavailable)
            {
                dialog.WarningText = "【ニコニコサービス利用不可】\nメンテナンス中またはサービス障害が発生しているかもしれません";

                // TODO: トースト通知で障害発生中としてブラウザで開くアクションを提示？
            }
            if (currentStatus == Mntone.Nico2.NiconicoSignInStatus.TwoFactorAuthRequired)
            {
                dialog.WarningText = "二要素認証によるログインが必要です";
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

                var loginResult = await NiconicoSession.SignIn(dialog.Mail, dialog.Password, true);
                if (loginResult == Mntone.Nico2.NiconicoSignInStatus.ServiceUnavailable)
                {
                    // サービス障害中
                    // 何か通知を出す？
                    NotificationService.ShowInAppNotification(new InAppNotificationPayload()
                    {
                        Content = "【ニコニコサービス利用不可】\nサービスがメンテナンス中、また何らかの障害が発生しているかもしれません"
                        , ShowDuration = TimeSpan.FromSeconds(10)
                    });
                    break;
                }

                if (loginResult == Mntone.Nico2.NiconicoSignInStatus.TwoFactorAuthRequired)
                {
                    break;
                }

                if (loginResult == Mntone.Nico2.NiconicoSignInStatus.Failed)
                {
                    dialog.WarningText = "メールアドレスかパスワードが違うようです";
                }

                isLoginSuccess = loginResult == Mntone.Nico2.NiconicoSignInStatus.Success;
            }

            // ログインを選択していた場合にのみアカウント情報を更新する
            // （キャンセル時は影響を発生させない）
            if (!isCanceled)
            {
                if (account != null)
                {
                    AccountManager.RemoveAccount(account.Item1);
                }

                if (dialog.IsRememberPassword)
                {
                    await AccountManager.AddOrUpdateAccount(dialog.Mail, dialog.Password);
                    AccountManager.SetPrimaryAccountId(dialog.Mail);
                }
                else
                {
                    AccountManager.SetPrimaryAccountId("");
                }
            }
        }

        private async void NiconicoSession_RequireTwoFactorAuth(object sender, Models.NiconicoSessionLoginRequireTwoFactorAuthEventArgs e)
        {
            var deferral = e.Deferral;
            var currentView = CoreApplication.GetCurrentView();
            if (currentView.IsMain)
            {
                await PageManager.StartNoUIWork("２段階認証を処理しています...",
                        () => ShowTwoFactorNumberInputDialogAsync(e.HttpRequestMessage.RequestUri, e.Context).AsAsyncAction()
                        );
            }
            else
            {
                await ShowTwoFactorNumberInputDialogAsync(e.HttpRequestMessage.RequestUri, e.Context);
            }

            deferral.Complete();

            // ログイン処理が終わるぐらいまで待機して
            await Task.Delay(500);

            // ログインに失敗していた場合はダイアログを再表示
            if (!NiconicoSession.IsLoggedIn && !NiconicoSession.ServiceStatus.IsOutOfService())
            {
                LoginCommand.Execute();
            }
        }

        async Task ShowTwoFactorNumberInputDialogAsync(Uri uri, NiconicoContext context)
        {
            var dialog = new TwoFactorAuthDialog()
            {
                IsTrustedDevice = true,
                DeviceName = "Hohoema_UWP"
            };

            var result = await dialog.ShowAsync();

            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                var codeText = dialog.CodeText;
                var isTrustedDevice = dialog.IsTrustedDevice;
                var deviceName = dialog.DeviceName;
                var mfaResult = await NiconicoSession.TryTwoFactorAuthAsync(uri, context, codeText, isTrustedDevice, deviceName);

                Debug.WriteLine(mfaResult);
            }
            
        }

        public void Dispose()
        {
            NiconicoSession.RequireTwoFactorAuth -= NiconicoSession_RequireTwoFactorAuth;
        }
    }
}

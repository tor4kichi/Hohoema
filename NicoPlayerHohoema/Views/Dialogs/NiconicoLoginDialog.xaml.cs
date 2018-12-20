using NicoPlayerHohoema.Commands;
using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
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
using Microsoft.Practices.Unity;
using NicoPlayerHohoema.Models.Helpers;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace NicoPlayerHohoema.Dialogs
{
    public sealed partial class NiconicoLoginDialog : ContentDialog
    {
        public NiconicoSession NiconicoSession { get; }

        // TODO: 2要素認証の再実装、Serviceとして切り出し

        public NiconicoLoginDialog(NiconicoSession niconicoSession)
        {
            this.InitializeComponent();

            this.Loading += NiconicoLoginDialog_Loading;
            NiconicoSession = niconicoSession;
        }

        private async void NiconicoLoginDialog_Loading(FrameworkElement sender, object args)
        {
            var account = await AccountManager.GetPrimaryAccount();
            if (account != null)
            {
                Mail.Text = account.Item1;
                Password.Password = account.Item2;

                IsRememberPassword.IsOn = true;
            }
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ProcessingBarrier.Visibility = Visibility.Visible;
            LoginProgressRing.IsActive = true;

            var defer = args.GetDeferral();

            bool isTwoFactorLogin = false;
            try
            {

                var mail = Mail.Text;
                var password = Password.Password;
                var result = await NiconicoSession.SignIn(mail, password);

                if (IsRememberPassword.IsOn)
                {
                    await AccountManager.AddOrUpdateAccount(mail, password);
                    AccountManager.SetPrimaryAccountId(mail);
                }
                else
                {
                    AccountManager.SetPrimaryAccountId("");
                    AccountManager.RemoveAccount(mail);
                }

                if (!isTwoFactorLogin 
                    && (result == Mntone.Nico2.NiconicoSignInStatus.Failed || result == Mntone.Nico2.NiconicoSignInStatus.ServiceUnavailable))
                {
                    args.Cancel = true;
                }

                
            }
            finally
            {
                if (!isTwoFactorLogin)
                {
                    defer.Complete();
                }

                ProcessingBarrier.Visibility = Visibility.Collapsed;
                LoginProgressRing.IsActive = false;
            }
        }



        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}

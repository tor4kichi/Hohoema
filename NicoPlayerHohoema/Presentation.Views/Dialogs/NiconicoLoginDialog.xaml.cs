using Hohoema.Commands;
using Hohoema.Models.Domain;
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
using Unity;
using Hohoema.Models.Domain.Helpers;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Dialogs
{
    public sealed partial class NiconicoLoginDialog : ContentDialog
    {
        public NiconicoLoginDialog()
        {
            this.InitializeComponent();
        }

        public string Mail
        {
            get { return MailTextBox.Text; }
            set { MailTextBox.Text = value; }
        }


        public string Password
        {
            get { return PasswordBox.Password; }
            set { PasswordBox.Password = value; }
        }


        public bool IsRememberPassword
        {
            get { return IsRememberPasswordToggle.IsOn; }
            set { IsRememberPasswordToggle.IsOn = value; }
        }

        public string WarningText
        {
            get { return WarningTextBlock.Text; }
            set { WarningTextBlock.Text = value; }
        }
    }
}

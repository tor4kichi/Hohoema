using Windows.UI.Xaml.Controls;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Hohoema.Dialogs;

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

using Windows.UI.Xaml.Controls;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Hohoema.Views.Dialogs;

public sealed partial class TwoFactorAuthDialog : ContentDialog
{
    public TwoFactorAuthDialog()
    {
        this.InitializeComponent();
    }


    public string CodeText
    {
        get => CodeTextBox.Text;
        set => CodeTextBox.Text = value;
    }

    public string DeviceName
    {
        get => DeviceNameTextBox.Text;
        set => DeviceNameTextBox.Text = value;
    }

    public bool IsTrustedDevice
    {
        get => IsTrustedDeviceToggleSwitch.IsOn;
        set => IsTrustedDeviceToggleSwitch.IsOn = value;
    }
}

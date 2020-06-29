using Hohoema.UseCase.Services;
using Hohoema.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    internal sealed class NiconicoTwoFactorAuthDialogService : INiconicoTwoFactorAuthDialogService
    {
        public async ValueTask<TwoFactorAuthInputResult> ShowNiconicoTwoFactorLoginDialog(bool defaultTrustedDevice, string defaultDeviceName)
        {
            var dialog = new TwoFactorAuthDialog()
            {
                IsTrustedDevice = defaultTrustedDevice,
                DeviceName = defaultDeviceName
            };

            var result = await dialog.ShowAsync();
            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return new TwoFactorAuthInputResult()
                {
                    Code = dialog.CodeText,
                    DeviceName = dialog.DeviceName,
                    IsTrustedDevice = dialog.IsTrustedDevice
                };
            }
            else
            {
                return null;
            }
        }
    }


}

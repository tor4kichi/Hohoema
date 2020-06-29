using Hohoema.UseCase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    internal sealed class NiconicoLoginDialogService : INiconicoLoginDialogService
    {
        public async ValueTask<LoginInfoInputResult> ShowLoginInputDialogAsync(string mail, string password, bool isRemember, string warningText)
        {
            var dialog = new Dialogs.NiconicoLoginDialog()
            {
                Mail = mail,
                Password = password,
                IsRememberPassword = isRemember,
                WarningText = warningText
            };
            
            var result = await dialog.ShowAsync();
            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return new LoginInfoInputResult()
                {
                    Mail = dialog.Mail,
                    Password = dialog.Password,
                    IsRememberPassword = dialog.IsRememberPassword,
                };
            }
            else
            {
                return null;
            }
        }
    }

}

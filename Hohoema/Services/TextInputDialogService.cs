using Hohoema.Dialogs;
using Hohoema.UseCase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Services
{
    internal sealed class TextInputDialogService : ITextInputDialogService
    {
        public async Task<string> GetTextAsync(string title, string placeholder, string defaultText = "", Func<string, bool> validater = null)
        {
            if (validater == null)
            {
                validater = (_) => true;
            }
            var context = new TextInputDialogContext(title, placeholder, defaultText, validater);

            var dialog = new TextInputDialog()
            {
                DataContext = context
            };

            var result = await dialog.ShowAsync();

            // 仮想入力キーボードを閉じる
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryHide();

            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return context.GetValidText();
            }
            else
            {
                return null;
            }
        }
    }
}

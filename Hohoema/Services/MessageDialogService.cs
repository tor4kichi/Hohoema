using Hohoema.UseCase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Hohoema.Services
{
    internal sealed class MessageDialogService : IMessageDialogService
    {
        public async Task<bool> ShowMessageDialog(string content, string title, string acceptButtonText = null, string cancelButtonText = null)
        {
            var dialog = new MessageDialog(content, title);
            if (acceptButtonText != null)
            {
                dialog.Commands.Add(new UICommand(acceptButtonText) { Id = "accept" });
            }

            if (cancelButtonText != null)
            {
                dialog.Commands.Add(new UICommand(cancelButtonText) { Id = "cancel" });
            }

            var result = await dialog.ShowAsync();

            return (result?.Id as string) == "accept";
        }

    }
}

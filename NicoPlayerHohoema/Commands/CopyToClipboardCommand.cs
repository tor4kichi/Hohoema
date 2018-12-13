using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class CopyToClipboardCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return true;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.INiconicoContent)
            {
                var content = parameter as Interfaces.INiconicoContent;

                var shareContent = Services.Helpers.ShareHelper.MakeShareText(content);
                var clipboardService = HohoemaCommnadHelper.GetClipboardService();
                clipboardService.CopyToClipboard(shareContent);
            }
            else if (parameter != null)
            {
                var clipboardService = HohoemaCommnadHelper.GetClipboardService();
                clipboardService.CopyToClipboard(parameter.ToString());
            }
        }
    }
}

using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class CopyToClipboardCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.INiconicoContent
                || parameter is string
                ;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.INiconicoContent)
            {
                var content = parameter as Interfaces.INiconicoContent;

                var shareContent = Helpers.ShareHelper.MakeShareText(content);
                Helpers.ShareHelper.CopyToClipboard(shareContent);
            }
            else if (parameter is string)
            {
                Helpers.ShareHelper.CopyToClipboard(parameter as string);
            }
        }
    }
}

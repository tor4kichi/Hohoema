using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenVideoOwnerVideoListCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;
                if (string.IsNullOrEmpty(content.OwnerUserId))
                {
                    return;
                }

                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(Models.HohoemaPageType.UserVideo, content.OwnerUserId.ToString());
            }
        }
    }
}

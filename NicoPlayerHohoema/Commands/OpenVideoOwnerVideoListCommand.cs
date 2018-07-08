using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenVideoOwnerVideoListCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent || parameter is string;
        }

        protected override void Execute(object parameter)
        {
            string ownerId = null;
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;
                if (string.IsNullOrEmpty(content.OwnerUserId))
                {
                    return;
                }
                ownerId = content.OwnerUserId;
            }
            else if (parameter is string id)
            {
                ownerId = id;
            }

            if (ownerId != null)
            {
                var pageManager = HohoemaCommnadHelper.GetPageManager();

                if (Mntone.Nico2.NiconicoRegex.IsChannelId(ownerId))
                {
                    pageManager.OpenPage(Models.HohoemaPageType.ChannelVideo, ownerId);
                }
                else
                {
                    pageManager.OpenPage(Models.HohoemaPageType.UserVideo, ownerId);
                }
            }

        }
    }
}

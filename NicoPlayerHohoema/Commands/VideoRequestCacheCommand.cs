using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class VideoRequestCacheCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent content)
            {
                var mediaManager = HohoemaCommnadHelper.GetVideoCacheManager();
                await mediaManager.RequestCache(content.Id);
            }
        }
    }
}

using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class VideoRequestCacheCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent
                && Util.InternetConnection.IsInternet()
                ;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var mediaManager = HohoemaCommnadHelper.GetHohoemaApp().MediaManager;
                var nicoVideo = await mediaManager.GetNicoVideoAsync(content.Id);
                await nicoVideo.RequestCache();
            }
        }
    }
}

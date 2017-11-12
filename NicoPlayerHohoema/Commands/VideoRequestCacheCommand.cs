using Prism.Commands;

namespace NicoPlayerHohoema.Commands
{
    public sealed class VideoRequestCacheCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent
                && Helpers.InternetConnection.IsInternet()
                ;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var mediaManager = HohoemaCommnadHelper.GetHohoemaApp().CacheManager;
                await mediaManager.RequestCache(content.Id, Models.NicoVideoQuality.Smile_Original);

                // TODO: キャッシュする画質を指定可能にしたい
            }
        }
    }
}

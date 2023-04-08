using Hohoema.Models.Niconico.Video;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed class OpenVideoOwnerMylistListCommand : CommandBase
{
    private readonly PageManager _pageManager;
    private readonly NicoVideoProvider _nicoVideoProvider;

    public OpenVideoOwnerMylistListCommand(
        PageManager pageManager,
        NicoVideoProvider nicoVideoProvider
        )
    {
        _pageManager = pageManager;
        _nicoVideoProvider = nicoVideoProvider;
    }

    protected override bool CanExecute(object parameter)
    {
        return parameter is IVideoContent or IVideoContentProvider;
    }

    protected override async void Execute(object parameter)
    {
        try
        {
            if (parameter is IVideoContentProvider provider && provider.ProviderType is NiconicoToolkit.Video.OwnerType.User && provider.ProviderId is not null)
            {
                _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.UserMylist, provider.ProviderId);
                return;
            }
        }
        catch { }

        if (parameter is IVideoContent content)
        {
            var video = await _nicoVideoProvider.GetCachedVideoInfoAsync(content.VideoId);
            if (video.ProviderType is NiconicoToolkit.Video.OwnerType.User)
            {
                _pageManager.OpenPageWithId(Models.PageNavigation.HohoemaPageType.UserMylist, video.ProviderId);
            }
        }
    }
}

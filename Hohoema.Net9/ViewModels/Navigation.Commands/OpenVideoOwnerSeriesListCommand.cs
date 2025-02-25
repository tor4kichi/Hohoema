#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed partial class OpenVideoOwnerSeriesListCommand : CommandBase
{
    private readonly IMessenger _messenger;
    private readonly NicoVideoProvider _nicoVideoProvider;

    public OpenVideoOwnerSeriesListCommand(
        IMessenger messenger,
        NicoVideoProvider nicoVideoProvider
        )        
    {
        _messenger = messenger;
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
                _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserSeries, provider.ProviderId);
                return;
            }
        }
        catch { }

        if (parameter is IVideoContent content)
        {
            var video = await _nicoVideoProvider.GetCachedVideoInfoAsync(content.VideoId);
            if (video.ProviderType is NiconicoToolkit.Video.OwnerType.User)
            {
                _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserSeries, video.ProviderId);
            }
        }
    }
}

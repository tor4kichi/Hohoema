#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed partial class OpenVideoOwnerMylistListCommand : CommandBase
{
    private readonly IMessenger _messenger;
    private readonly NicoVideoProvider _nicoVideoProvider;

    public OpenVideoOwnerMylistListCommand(
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
                await _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserMylist, provider.ProviderId);
                return;
            }
        }
        catch { }

        if (parameter is IVideoContent content)
        {
            var video = await _nicoVideoProvider.GetCachedVideoInfoAsync(content.VideoId);
            if (video.ProviderType is NiconicoToolkit.Video.OwnerType.User)
            {
                await _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserMylist, video.ProviderId);
            }
        }
    }
}

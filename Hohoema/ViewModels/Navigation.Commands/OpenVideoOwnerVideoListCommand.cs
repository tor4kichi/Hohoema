#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed class OpenVideoOwnerVideoListCommand : CommandBase
{
    private readonly IMessenger _messenger;
    private readonly NicoVideoProvider _nicoVideoProvider;
    
    public OpenVideoOwnerVideoListCommand(
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
            if (parameter is IVideoContentProvider provider && provider.ProviderType is not NiconicoToolkit.Video.OwnerType.Hidden && provider.ProviderId is not null)
            {
                if (provider.ProviderType is NiconicoToolkit.Video.OwnerType.User)
                {
                    _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserVideo, provider.ProviderId);
                }
                else if (provider.ProviderType is NiconicoToolkit.Video.OwnerType.Channel)
                {
                    _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.ChannelVideo, provider.ProviderId);
                }
                return;
            }
        }
        catch { }

        if (parameter is IVideoContent content)
        {
            var video = await _nicoVideoProvider.GetCachedVideoInfoAsync(content.VideoId);
            if (video.ProviderType is NiconicoToolkit.Video.OwnerType.User)
            {
                _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.UserVideo, video.ProviderId);
            }
            else if (video.ProviderType is NiconicoToolkit.Video.OwnerType.Channel)
            {
                _ = _messenger.OpenPageWithIdAsync(Models.PageNavigation.HohoemaPageType.ChannelVideo, video.ProviderId);
            }
        }

    }
}

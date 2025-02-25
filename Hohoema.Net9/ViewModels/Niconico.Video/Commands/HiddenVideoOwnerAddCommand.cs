#nullable enable
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Video;

namespace Hohoema.ViewModels.Niconico.Video.Commands;

public sealed partial class HiddenVideoOwnerAddCommand : CommandBase
{
    public HiddenVideoOwnerAddCommand(
        VideoFilteringSettings ngSettings,
        ChannelProvider channelProvider,
        UserProvider userProvider,
        NicoVideoOwnerCacheRepository nicoVideoOwnerRepository
        )
    {
        NgSettings = ngSettings;
        ChannelProvider = channelProvider;
        UserProvider = userProvider;
        _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
    }

    private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

    public VideoFilteringSettings NgSettings { get; }
    public ChannelProvider ChannelProvider { get; }
    public UserProvider UserProvider { get; }

    protected override bool CanExecute(object parameter)
    {
        if (parameter is IVideoContentProvider provider)
        {
            if (provider.ProviderId != null)
            {
                return !NgSettings.IsHiddenVideoOwnerId(provider.ProviderId);
            }
        }

        return false;
    }

    protected override async void Execute(object parameter)
    {
        if (parameter is IVideoContentProvider provider)
        {
            string ownerName = null;
            if (string.IsNullOrEmpty(ownerName))
            {
                if (provider.ProviderType == OwnerType.User)
                {
                    var user = _nicoVideoOwnerRepository.Get(provider.ProviderId);
                    if (user?.ScreenName is not null)
                    {
                        ownerName = user.ScreenName;
                    }
                    else
                    {
                        ownerName = await UserProvider.GetUserNameAsync(provider.ProviderId);
                    }
                }
                else if (provider.ProviderType == OwnerType.Channel)
                {
                    ownerName = await ChannelProvider.GetChannelNameWithCacheAsync(provider.ProviderId);
                }
            }

            NgSettings.AddHiddenVideoOwnerId(provider.ProviderId.ToString(), ownerName);
        }
    }
}

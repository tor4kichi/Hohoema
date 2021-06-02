using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using NiconicoToolkit.Video;
using Prism.Commands;
using System;
using System.Collections.Generic;
using Windows.UI.Popups;

namespace Hohoema.Presentation.ViewModels.Niconico.Video.Commands
{
    public sealed class HiddenVideoOwnerAddCommand : DelegateCommandBase
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
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

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
                            ownerName = await UserProvider.GetUserName(provider.ProviderId);
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

}

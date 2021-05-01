using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
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
            if (parameter is IVideoContent video)
            {
                if (video.ProviderId != null)
                {
                    return !NgSettings.IsHiddenVideoOwnerId(video.ProviderId);
                }
            }

            return false;
        }

        protected override async void Execute(object parameter)
        {
            var currentMethod = System.Reflection.MethodBase.GetCurrentMethod();
            Microsoft.AppCenter.Analytics.Analytics.TrackEvent($"{currentMethod.DeclaringType.Name}#{currentMethod.Name}");

            if (parameter is IVideoContent)
            {
                var content = parameter as IVideoContent;

                string ownerName = null;
                if (string.IsNullOrEmpty(ownerName))
                {
                    if (content.ProviderType == NicoVideoUserType.User)
                    {
                        try
                        {
                            var userInfo = await UserProvider.GetUser(content.ProviderId);

                            ownerName = userInfo.ScreenName;
                        }
                        catch
                        {
                            return;
                        }
                    }
                    else if (content.ProviderType == NicoVideoUserType.Channel)
                    {
                        var channelInfo = await ChannelProvider.GetChannelInfo(content.ProviderId);
                        ownerName = channelInfo.Name;

                        var channel = _nicoVideoOwnerRepository.Get(content.ProviderId) 
                            ?? new NicoVideoOwner()
                            {
                                OwnerId = channelInfo.ChannelId.ToString(),
                                UserType = NicoVideoUserType.Channel,
                            };                        
                        channel.ScreenName = channelInfo.ScreenName ?? channel.ScreenName;
                        _nicoVideoOwnerRepository.UpdateItem(channel);
                    }
                }

                NgSettings.AddHiddenVideoOwnerId(content.ProviderId.ToString(), ownerName);
            }
        }
    }

}

using I18NPortable;
using Hohoema.Database;
using Hohoema.Models;
using Prism.Commands;
using System;
using Windows.UI.Popups;
using Hohoema.Models.Repository.Niconico.Channel;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.App;
using Hohoema.Models.Repository;

namespace Hohoema.UseCase.Playlist.Commands
{
    public sealed class AddToHiddenUserCommand : DelegateCommandBase
    {
        private readonly VideoListFilterSettings _videoListFilterSettings;

        public AddToHiddenUserCommand(
            VideoListFilterSettings videoListFilterSettings,
            ChannelProvider channelProvider,
            UserProvider userProvider
            )
        {
            _videoListFilterSettings = videoListFilterSettings;
            ChannelProvider = channelProvider;
            UserProvider = userProvider;
        }

        public ChannelProvider ChannelProvider { get; }
        public UserProvider UserProvider { get; }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is IVideoContent video)
            {
                if (video.ProviderId != null)
                {
                    return _videoListFilterSettings.IsNgVideoOwner(video.ProviderId);
                }
            }

            return false;
        }

        protected override async void Execute(object parameter)
        {
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

                        var channel = Database.NicoVideoOwnerDb.Get(content.ProviderId) 
                            ?? new Database.NicoVideoOwner()
                            {
                                OwnerId = channelInfo.ChannelId.ToString(),
                                UserType = NicoVideoUserType.Channel,
                            };                        
                        channel.ScreenName = channelInfo.ScreenName ?? channel.ScreenName;
                        Database.NicoVideoOwnerDb.AddOrUpdate(channel);
                    }
                }

                _videoListFilterSettings.AddNgVideoOwner(content.ProviderId.ToString(), ownerName);
            }
        }
    }

}

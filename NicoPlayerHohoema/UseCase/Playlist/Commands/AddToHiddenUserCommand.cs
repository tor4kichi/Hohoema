using I18NPortable;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Provider;
using Prism.Commands;
using System;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class AddToHiddenUserCommand : DelegateCommandBase
    {
        public AddToHiddenUserCommand(
            NGSettings ngSettings,
            ChannelProvider channelProvider,
            UserProvider userProvider
            )
        {
            NgSettings = ngSettings;
            ChannelProvider = channelProvider;
            UserProvider = userProvider;
        }

        public NGSettings NgSettings { get; }
        public ChannelProvider ChannelProvider { get; }
        public UserProvider UserProvider { get; }

        protected override bool CanExecute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent video)
            {
                if (video.ProviderId != null)
                {
                    return NgSettings.IsNgVideoOwnerId(video.ProviderId) == null;
                }
            }

            return false;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

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

                var dialog = new MessageDialog(
                    "Dialog_HiddenVideoOwnerDesc".Translate(ownerName),
                    "Dialog_HiddenVideoOwnerTitle".Translate(ownerName)
                    );

                dialog.Commands.Add(new UICommand()
                {
                    Label = "AddHiddenVideoOwner".Translate(),
                    Invoked = (uicommand) =>
                    {
                        NgSettings.AddNGVideoOwnerId(content.ProviderId.ToString(), ownerName);
                    }
                });
                dialog.Commands.Add(new UICommand() { Label = "Cancel".Translate() });

                dialog.DefaultCommandIndex = 0;

                await dialog.ShowAsync();
            }
        }
    }

}

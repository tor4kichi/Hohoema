using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Provider;
using Prism.Commands;
using System;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.Commands
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

                
                var ownerName = content.ProviderName;
                if (string.IsNullOrEmpty(ownerName))
                {
                    if (content.ProviderType == Mntone.Nico2.Videos.Thumbnail.UserType.User)
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
                    else if (content.ProviderType == Mntone.Nico2.Videos.Thumbnail.UserType.Channel)
                    {
                        var channelInfo = await ChannelProvider.GetChannelInfo(content.ProviderId);
                        ownerName = channelInfo.Name;

                        var channel = Database.NicoVideoOwnerDb.Get(content.ProviderId) 
                            ?? new Database.NicoVideoOwner()
                            {
                                OwnerId = channelInfo.ChannelId.ToString(),
                                UserType = Mntone.Nico2.Videos.Thumbnail.UserType.Channel,
                            };                        
                        channel.ScreenName = channelInfo.ScreenName ?? channel.ScreenName;
                        Database.NicoVideoOwnerDb.AddOrUpdate(channel);
                    }
                }

                var dialog = new MessageDialog(
                    $"この変更は投稿者（{ownerName} さん）のアプリ内ユーザー情報ページから取り消すことができます。",

                    $"『{ownerName}』さんの投稿動画を非表示にしますか？"
                    );

                dialog.Commands.Add(new UICommand()
                {
                    Label = "非表示に設定",
                    Invoked = (uicommand) =>
                    {
                        NgSettings.AddNGVideoOwnerId(content.ProviderId.ToString(), ownerName);
                    }
                });
                dialog.Commands.Add(new UICommand() { Label = "キャンセル" });

                dialog.DefaultCommandIndex = 0;

                await dialog.ShowAsync();
            }
        }
    }

}

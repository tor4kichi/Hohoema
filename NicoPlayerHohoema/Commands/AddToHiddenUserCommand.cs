using Prism.Commands;
using System;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.Commands
{
    public sealed class AddToHiddenUserCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                
                var ownerName = content.OwnerUserName;
                if (string.IsNullOrEmpty(ownerName))
                {
                    if (content.OwnerUserType == Mntone.Nico2.Videos.Thumbnail.UserType.User)
                    {
                        try
                        {
                            var userProvider = HohoemaCommnadHelper.Resolve<Models.Provider.UserProvider>();
                            var userInfo = await userProvider.GetUser(content.OwnerUserId);

                            ownerName = userInfo.ScreenName;
                        }
                        catch
                        {
                            return;
                        }
                    }
                    else if (content.OwnerUserType == Mntone.Nico2.Videos.Thumbnail.UserType.Channel)
                    {
                        var channelProvider = HohoemaCommnadHelper.Resolve<Models.Provider.ChannelProvider>();
                        var channelInfo = await channelProvider.GetChannelInfo(content.OwnerUserId);
                        ownerName = channelInfo.Name;

                        var channel = Database.NicoVideoOwnerDb.Get(content.OwnerUserId) 
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
                        var ngSettings = HohoemaCommnadHelper.Resolve<Models.NGSettings>();
                        ngSettings.AddNGVideoOwnerId(content.OwnerUserId.ToString(), ownerName);
                    }
                });
                dialog.Commands.Add(new UICommand() { Label = "キャンセル" });

                dialog.DefaultCommandIndex = 0;

                await dialog.ShowAsync();
            }
        }
    }

}

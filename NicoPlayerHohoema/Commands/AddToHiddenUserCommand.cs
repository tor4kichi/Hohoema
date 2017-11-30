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

                var hohoemaApp = HohoemaCommnadHelper.GetHohoemaApp();
                var ownerName = content.OwnerUserName;
                if (string.IsNullOrEmpty(ownerName))
                {
                    try
                    {
                        var userInfo = await hohoemaApp.ContentProvider.GetUserDetail(content.OwnerUserId);

                        ownerName = userInfo.Nickname;
                    }
                    catch
                    {
                        return;
                    }
                }

                var dialog = new MessageDialog(
                    $"この変更は投稿者（{ownerName} さん）のアプリ内ユーザー情報ページから取り消すことができます。",

                    $"『{ownerName}』さんの投稿動画を非表示に設定しますか？"
                    );

                dialog.Commands.Add(new UICommand()
                {
                    Label = "非表示に設定",
                    Invoked = (uicommand) =>
                    {
                        hohoemaApp.UserSettings.NGSettings.AddNGVideoOwnerId(content.OwnerUserId.ToString(), content.OwnerUserName);

                        // TODO: 表示中ページへの更新イベントをトリガー
                    }
                });
                dialog.Commands.Add(new UICommand() { Label = "キャンセル" });

                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            }
        }
    }

}

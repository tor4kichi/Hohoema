using Prism.Commands;
using System;
using System.Linq;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.Commands
{
    public sealed class VideoDeleteCacheCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IVideoContent
                ;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IVideoContent)
            {
                var content = parameter as Interfaces.IVideoContent;

                var mediaManager = HohoemaCommnadHelper.GetHohoemaApp().MediaManager;
                var nicoVideo = mediaManager.GetNicoVideo(content.Id);

                if (nicoVideo.GetAllQuality().ToArray().Any(x => x.IsCached))
                {
                    // キャッシュ済みがある場合は、削除確認を行う


                    var dialog = new MessageDialog(
                    $"{nicoVideo.Title} の キャッシュデータ（全ての画質）を削除します。この操作は元に戻せません。",
                    "キャッシュの削除確認"
                    );

                    dialog.Commands.Add(new UICommand()
                    {
                        Label = "キャッシュを削除",
                        Invoked = async (uicommand) =>
                        {
                            await nicoVideo.CancelCacheRequest();
                        }
                    });
                    dialog.Commands.Add(new UICommand()
                    {
                        Label = "キャンセル",
                    });

                    dialog.DefaultCommandIndex = 1;

                    await dialog.ShowAsync();
                }
                else
                {
                    // キャッシュリクエストのみで
                    // キャッシュがいずれも未完了の場合は
                    // 確認無しで削除

                    await nicoVideo.CancelCacheRequest();
                }
            }
        }
    }
}

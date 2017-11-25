using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace NicoPlayerHohoema.Commands
{
    public sealed class FeedGroupRemoveCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IFeedGroup;
        }

        protected override async void Execute(object parameter)
        {
            if (parameter is Interfaces.IFeedGroup)
            {
                var feedGroupManager = HohoemaCommnadHelper.GetFeedManager();
                var content = parameter as Interfaces.IFeedGroup;
                var feedGroup = feedGroupManager.GetFeedGroup(int.Parse(content.Id));

                if (feedGroup != null)
                {
                    var dialog = new MessageDialog(
                            $"指定されたフィードグループを削除します。この操作は元に戻せません。",
                            $"フィードグループ \"{feedGroup.Label}\"を削除します"
                            );
                    dialog.Commands.Add(new UICommand("フィードグループを削除") { Id = "delete" });
                    dialog.Commands.Add(new UICommand("キャンセル"));

                    dialog.CancelCommandIndex = 1;
                    dialog.DefaultCommandIndex = 1;
                    var result = await dialog.ShowAsync();

                    if ((result.Id as string) == "delete")
                    {
                        feedGroupManager.RemoveFeedGroup(feedGroup);
                    }
                }
            }
        }
    }
}

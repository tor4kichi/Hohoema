using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class FeedGroupUpdateCommand : DelegateCommandBase
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
                // await feedGroup.Refresh();

                // TODO: フィードマネージャやフィード編集ページの表示を更新する
            }
        }
    }
}

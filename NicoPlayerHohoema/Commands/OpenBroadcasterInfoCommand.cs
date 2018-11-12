using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenBroadcasterInfoCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.ILiveContent liveContent
                && !string.IsNullOrEmpty(liveContent.BroadcasterId);
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.ILiveContent content)
            {
                if (!string.IsNullOrEmpty(content.BroadcasterId))
                {
                    var pageManager = HohoemaCommnadHelper.GetPageManager();
                    pageManager.OpenPage(Models.HohoemaPageType.Community, content.BroadcasterId);
                }
            }
        }
    }
}

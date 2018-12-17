using Mntone.Nico2;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Commands
{
    public sealed class OpenLiveInfoCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is string || parameter is Interfaces.ILiveContent;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string liveId
                && NiconicoRegex.IsLiveId(liveId)
                )
            {
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(NicoPlayerHohoema.Models.HohoemaPageType.LiveInfomation, liveId);
            }
            else if (parameter is Interfaces.ILiveContent liveContent)
            {
                var pageManager = HohoemaCommnadHelper.GetPageManager();
                pageManager.OpenPage(NicoPlayerHohoema.Models.HohoemaPageType.LiveInfomation, liveContent.Id);
            }
        }
    }
}

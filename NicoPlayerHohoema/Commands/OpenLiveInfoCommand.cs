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
            return parameter is string;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is string liveId
                && NiconicoRegex.IsLiveId(liveId)
                )
            {
                var pageManager = App.Current.Container.Resolve<Models.PageManager>();
                pageManager.OpenPage(NicoPlayerHohoema.Models.HohoemaPageType.LiveInfomation, liveId);
            }
        }
    }
}

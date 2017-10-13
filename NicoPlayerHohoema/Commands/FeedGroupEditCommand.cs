using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Commands
{
    public sealed class FeedGroupEditCommand : DelegateCommandBase
    {
        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.IFeedGroup;
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.IFeedGroup)
            {
                var content = parameter as Interfaces.IFeedGroup;
                var pageManager = HohoemaCommnadHelper.GetPageManager();

                pageManager.OpenPage(Models.HohoemaPageType.FeedGroup, content.Id);
            }
        }
    }
}

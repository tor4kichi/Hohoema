using Hohoema.Services;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Commands
{
    public sealed class OpenBroadcasterInfoCommand : DelegateCommandBase
    {
        public OpenBroadcasterInfoCommand(
            PageManager pageManager
            )
        {
            PageManager = pageManager;
        }

        public PageManager PageManager { get; }

        protected override bool CanExecute(object parameter)
        {
            return parameter is Interfaces.ILiveContent liveContent
                && !string.IsNullOrEmpty(liveContent.ProviderId);
        }

        protected override void Execute(object parameter)
        {
            if (parameter is Interfaces.ILiveContent content)
            {
                if (!string.IsNullOrEmpty(content.ProviderId))
                {
                    PageManager.OpenPageWithId(HohoemaPageType.Community, content.ProviderId);
                }
            }
        }
    }
}

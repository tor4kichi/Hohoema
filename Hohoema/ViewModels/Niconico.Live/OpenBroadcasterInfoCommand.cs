using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase.PageNavigation;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Live
{
    public sealed class OpenBroadcasterInfoCommand : CommandBase
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
            return parameter is ILiveContentProvider liveContent
                && !string.IsNullOrEmpty(liveContent.ProviderId);
        }

        protected override void Execute(object parameter)
        {
            if (parameter is ILiveContentProvider content)
            {
                if (!string.IsNullOrEmpty(content.ProviderId))
                {
                    PageManager.OpenPageWithId(HohoemaPageType.Community, content.ProviderId);
                }
            }
        }
    }
}

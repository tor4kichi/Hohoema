using Hohoema.Database;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico.Mylist;
using Prism.Commands;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Pages.Commands
{
    public sealed class OpenContentOwnerPageCommand : DelegateCommandBase
    {
        private readonly PageManager _pageManager;

        public OpenContentOwnerPageCommand(PageManager pageManager)
        {
            _pageManager = pageManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is IVideoContent
                || parameter is IMylist
                ;
        }

        protected override void Execute(object parameter)
        {
            switch (parameter)
            {
                case IVideoContent videoContent:
                    if (videoContent.ProviderType == NicoVideoUserType.User)
                    {
                        var p = new NavigationParameters();
                        p.Add("id", videoContent.ProviderId);
                        _pageManager.OpenPage(HohoemaPageType.UserInfo, p);
                    }
                    else if (videoContent.ProviderType == NicoVideoUserType.Channel)
                    {
                        var p = new NavigationParameters();
                        p.Add("id", videoContent.ProviderId);
                        _pageManager.OpenPage(HohoemaPageType.ChannelVideo, p);
                    }

                    break;
                case IMylist mylist:
                    {
                        _pageManager.OpenPageWithId(HohoemaPageType.Mylist, mylist.Id);
                        break;

                    }
            }
        }
    }
}

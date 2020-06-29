using Hohoema.Database;
using Hohoema.Interfaces;
using Hohoema.Models.Pages;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Mylist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Pages.Commands
{
    public sealed class OpenVideoListPageCommand : DelegateCommandBase
    {
        private readonly PageManager _pageManager;

        public OpenVideoListPageCommand(PageManager pageManager)
        {
            _pageManager = pageManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string
                 || parameter is IVideoContent
                 || parameter is ILiveContent
                 || parameter is ICommunity
                 || parameter is IMylist
                 || parameter is IUser
                 || parameter is ISearchWithtag
                 || parameter is ITag
                 || parameter is ISearchHistory
                 || parameter is IChannel
                 ;
        }

        protected override void Execute(object parameter)
        {
            switch (parameter)
            {
                case string s:
                    {
                        if (Enum.TryParse<HohoemaPageType>(s, out var pageType))
                        {
                            _pageManager.OpenPage(pageType);
                        }

                        break;
                    }

                case IVideoContent videoContent:
                    if (videoContent.ProviderType == NicoVideoUserType.User)
                    {
                        _pageManager.OpenPageWithId(HohoemaPageType.UserVideo, videoContent.ProviderId);
                    }
                    else if (videoContent.ProviderType == NicoVideoUserType.Channel)
                    {
                        _pageManager.OpenPageWithId(HohoemaPageType.ChannelVideo, videoContent.ProviderId);
                    }
                    break;
                case ILiveContent liveContent:
                    _pageManager.OpenPageWithId(HohoemaPageType.LiveInfomation, liveContent.Id);
                    break;
                case ICommunity communityContent:
                    _pageManager.OpenPageWithId(HohoemaPageType.CommunityVideo, communityContent.Id);
                    break;
                case IMylist mylistContent:
                    _pageManager.OpenPageWithId(HohoemaPageType.Mylist, mylistContent.Id);
                    break;
                case IUser user:
                    _pageManager.OpenPageWithId(HohoemaPageType.UserVideo, user.Id);
                    break;
                case ISearchWithtag tag:
                    _pageManager.Search(SearchTarget.Tag, tag.Tag);
                    break;
                case ISearchHistory history:
                    _pageManager.Search(history.Target, history.Keyword);
                    break;
                case IChannel channel:
                    _pageManager.OpenPageWithId(HohoemaPageType.UserVideo, channel.Id);
                    break;
            }
        }
    }
}

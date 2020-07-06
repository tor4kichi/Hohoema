using Hohoema.Interfaces;
using Hohoema.Models.Niconico.Follow;
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
    public sealed class OpenPageCommand : DelegateCommandBase
    {
        private readonly PageManager _pageManager;

        public OpenPageCommand(PageManager pageManager)
        {
            _pageManager = pageManager;
        }

        protected override bool CanExecute(object parameter)
        {
            return parameter is string
                || parameter is FollowItemInfo
                || parameter is IPageNavigatable
                || parameter is IVideoContent
                || parameter is ICommunity
                || parameter is IMylist
                || parameter is IUser
                || parameter is ISearchWithtag
                || parameter is ITag
                || parameter is ISearchHistory
                || parameter is IChannel
                || parameter is IPlaylist
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

                case FollowItemInfo followItem:
                    switch (followItem.FollowItemType)
                    {
                        case FollowItemType.Tag:
                            _pageManager.Search(SearchTarget.Tag, followItem.Id);
                            break;
                        case FollowItemType.Mylist:
                            _pageManager.OpenPageWithId(HohoemaPageType.Mylist, followItem.Id);
                            break;
                        case FollowItemType.User:
                            _pageManager.OpenPageWithId(HohoemaPageType.UserInfo, followItem.Id);
                            break;
                        case FollowItemType.Community:
                            _pageManager.OpenPageWithId(HohoemaPageType.Community, followItem.Id);
                            break;
                        case FollowItemType.Channel:
                            _pageManager.OpenPageWithId(HohoemaPageType.ChannelVideo, followItem.Id);
                            break;
                        default:
                            break;
                    }
                    break;
                case IPageNavigatable item:
                    if (item.Parameter != null)
                    {
                        _pageManager.OpenPage(item.PageType, item.Parameter);
                    }
                    else
                    {
                        _pageManager.OpenPage(item.PageType, default(string));
                    }
                    break;
                case Models.Pages.HohoemaPin pin:
                    _pageManager.OpenPage(pin.PageType, pin.Parameter);
                    break;
                case IVideoContent videoContent:
                    _pageManager.OpenPageWithId(HohoemaPageType.VideoInfomation, videoContent.Id);
                    break;
                case ICommunity communityContent:
                    _pageManager.OpenPageWithId(HohoemaPageType.Community, communityContent.Id);
                    break;
                case IMylist mylistContent:
                    _pageManager.OpenPageWithId(HohoemaPageType.Mylist, mylistContent.Id);
                    break;
                case IUser user:
                    _pageManager.OpenPageWithId(HohoemaPageType.UserInfo, user.Id);
                    break;
                case ISearchWithtag tag:
                    _pageManager.Search(SearchTarget.Tag, tag.Tag);
                    break;
                case ITag videoTag:
                    _pageManager.Search(SearchTarget.Tag, videoTag.Tag);
                    break;
                case ISearchHistory history:
                    _pageManager.Search(history.Target, history.Keyword);
                    break;
                case IChannel channel:
                    _pageManager.OpenPageWithId(HohoemaPageType.ChannelVideo, channel.Id);
                    break;
                case IPlaylist playlist:
                    _pageManager.OpenPageWithId(HohoemaPageType.LocalPlaylist, playlist.Id);
                    break;
            }
        }
    }
}

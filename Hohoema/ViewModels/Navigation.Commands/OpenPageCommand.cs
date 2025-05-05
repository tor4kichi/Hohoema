#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using DryIoc;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Channels;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Navigation.Commands;

public sealed class OpenPageCommand : CommandBase
{
    private readonly IMessenger _messenger;

    public OpenPageCommand(IMessenger messenger) 
    {
        _messenger = messenger;
    }

    protected override bool CanExecute(object parameter)
    {
        return true;
    }

    protected override void Execute(object parameter)
    {
        switch (parameter)
        {
            case HohoemaPageType rawPageType:
                _ = _messenger.OpenPageAsync(rawPageType);
                break;
            case string s:
                {
                    if (Enum.TryParse<HohoemaPageType>(s, out var pageType))
                    {
                        _ = _messenger.OpenPageAsync(pageType);
                    }

                    break;
                }

            case FollowItemInfo followItem:
                switch (followItem.FollowItemType)
                {
                    case FollowItemType.User:
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.UserInfo, (UserId)followItem.Id);
                        break;
                    case FollowItemType.Tag:
                        _ = _messenger.OpenSearchPageAsync(SearchTarget.Tag, followItem.Id);
                        break;
                    case FollowItemType.Mylist:
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.Mylist, (MylistId)followItem.Id);
                        break;
                    case FollowItemType.Channel:
                        _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.ChannelVideo, (ChannelId)followItem.Id);
                        break;
                    default:
                        break;
                }
                break;
            case IPageNavigatable item:
                if (item.Parameter != null)
                {
                    _ = _messenger.OpenPageAsync(item.PageType, item.Parameter);
                }
                else
                {
                    _ = _messenger.OpenPageAsync(item.PageType);
                }
                break;
            case HohoemaPin pin:
                _ = _messenger.OpenPageAsync(pin.PageType, new NavigationParameters(pin.Parameter));
                break;
            case IVideoContent videoContent:
                _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.VideoInfomation, videoContent.VideoId);
                break;
            case ILiveContent liveContent:
                _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.LiveInfomation, liveContent.LiveId);
                break;
            case IMylist mylistContent:
                _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.Mylist, mylistContent.MylistId);
                break;
            case IUser user:
                _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.UserInfo, user.UserId);
                break;
            case ITag tag:
                _ = _messenger.OpenSearchPageAsync(SearchTarget.Tag, tag.Tag);
                break;
            case ISearchHistory history:
                _ = _messenger.OpenSearchPageAsync(history.Target, history.Keyword);
                break;
            case IChannel channel:
                _ = _messenger.OpenPageWithIdAsync(HohoemaPageType.ChannelVideo, channel.ChannelId);
                break;
            case IPlaylist playlist:
                _ = _messenger.OpenPlaylistPageAsync(playlist);
                break;
        }
    }
}

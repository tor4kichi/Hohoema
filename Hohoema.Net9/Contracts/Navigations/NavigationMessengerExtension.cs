#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Contracts.Navigations;
public static class NavigationMessengerExtension
{
    static readonly Dictionary<HohoemaPageType, string> _pageTypeToName = Enum.GetValues(typeof(HohoemaPageType))
        .Cast<HohoemaPageType>().Select(x => (x, x + "Page")).ToDictionary(x => x.x, x => x.Item2);
   
    public static async Task<INavigationResult> OpenPageAsync(this IMessenger messenger, HohoemaPageType pageType, INavigationParameters? parameters = null)
    {
        return await messenger.Send(new NavigationAsyncRequestMessage(new(_pageTypeToName[pageType], parameters)));
    }

    public static async Task<INavigationResult> OpenPageWithIdAsync(this IMessenger messenger, HohoemaPageType pageType, string id)
    {
        return await messenger.OpenPageAsync(pageType, ("id", id));
    }

    public static async Task<INavigationResult> OpenPageAsync(this IMessenger messenger, HohoemaPageType pageType, params (string key, object value)[] parameters)
    {
        return await messenger.Send(new NavigationAsyncRequestMessage(new(_pageTypeToName[pageType], new NavigationParameters(parameters))));
    }

    public static async Task<INavigationResult> OpenPlaylistPageAsync(this IMessenger messenger, IPlaylist playlist)
    {
        if (playlist.PlaylistId == QueuePlaylist.Id)
        {
            return await messenger.OpenPageAsync(HohoemaPageType.VideoQueue);
        }
        else if (playlist.PlaylistId.Origin is PlaylistItemsSourceOrigin.SearchWithKeyword)
        {
            return await messenger.OpenSearchPageAsync(SearchTarget.Keyword, playlist.PlaylistId.Id);
        }
        else if (playlist.PlaylistId.Origin is PlaylistItemsSourceOrigin.SearchWithTag)
        {
            return await messenger.OpenSearchPageAsync(SearchTarget.Tag, playlist.PlaylistId.Id);
        }
        else
        {
            var pageType = playlist.PlaylistId.Origin switch
            {
                PlaylistItemsSourceOrigin.Mylist => HohoemaPageType.Mylist,
                PlaylistItemsSourceOrigin.Local => HohoemaPageType.LocalPlaylist,
                PlaylistItemsSourceOrigin.ChannelVideos => HohoemaPageType.ChannelVideo,
                PlaylistItemsSourceOrigin.UserVideos => HohoemaPageType.UserVideo,
                PlaylistItemsSourceOrigin.Series => HohoemaPageType.Series,
                PlaylistItemsSourceOrigin.CommunityVideos => HohoemaPageType.CommunityVideo,
                _ => throw new NotSupportedException(playlist.PlaylistId.Origin.ToString()),
            };

            INavigationParameters parameter = new NavigationParameters(("id", playlist.PlaylistId.Id));
            return await messenger.OpenPageAsync(pageType, parameter);
        }        
    }

    public static Task<INavigationResult> OpenSearchPageAsync(this IMessenger messenger, SearchTarget target, string keyword)
    {
        var p = new NavigationParameters
        {
            { "keyword", System.Net.WebUtility.UrlEncode(keyword) },
            { "service", target }
        };

        return messenger.OpenPageAsync(HohoemaPageType.Search, p);
    }


    public static async Task<bool> OpenUriAsync(this IMessenger messenger, Uri uri)
    {
        var path = uri.AbsoluteUri;
        // is mylist url?
        if (path.StartsWith("http://www.nicovideo.jp/mylist/") || path.StartsWith("https://www.nicovideo.jp/mylist/"))
        {
            var mylistId = uri.AbsolutePath.Split('/').Last();
            await messenger.OpenPageWithIdAsync(HohoemaPageType.Mylist, mylistId);
            return true;
        }
        else if (path.StartsWith("http://www.nicovideo.jp/watch/") || path.StartsWith("https://www.nicovideo.jp/watch/"))
        {
            // is nico video url?
            var videoId = uri.AbsolutePath.Split('/').Last();
            await messenger.Send(new VideoPlayRequestMessage() { VideoId = videoId });
            return true;
        }
        else if (path.StartsWith("http://com.nicovideo.jp/community/") || path.StartsWith("https://com.nicovideo.jp/community/"))
        {
            var communityId = uri.AbsolutePath.Split('/').Last();
            await messenger.OpenPageWithIdAsync(HohoemaPageType.Community, communityId);
            return true;
        }
        else if (path.StartsWith("http://com.nicovideo.jp/user/") || path.StartsWith("https://com.nicovideo.jp/user/"))
        {
            var userId = uri.AbsolutePath.Split('/').Last();
            await messenger.OpenPageWithIdAsync(HohoemaPageType.UserInfo, userId);
            return true;
        }
        else if (path.StartsWith("http://ch.nicovideo.jp/") || path.StartsWith("https://ch.nicovideo.jp/"))
        {
            var elem = uri.AbsolutePath.Split('/');
            if (elem.Any(x => x == "article" || x == "blomaga"))
            {
                Debug.WriteLine("Urlを処理できませんでした : {0}", uri.OriginalString);
                return false;
            }
            else
            {
                var channelId = elem.Last();
                await messenger.OpenPageWithIdAsync(HohoemaPageType.ChannelVideo, channelId);
                return true;
            }
        }
        else
        {
            Debug.WriteLine("Urlを処理できませんでした : {0}", uri.OriginalString);
            return false;
        }
    }

    public static async Task<INavigationResult> OpenVideoListPageAsync(this IMessenger messenger, string pageTypeString)
    {
        if (Enum.TryParse<HohoemaPageType>(pageTypeString, out var pageType))
        {
            return await messenger.OpenPageAsync(pageType);
        }
        else
        {
            return new NavigationResult() { IsSuccess = false };
        }
    }


    public static async Task<INavigationResult> OpenVideoListPageAsync(this IMessenger messenger, IVideoContentProvider videoContent)
    {
        if (videoContent.ProviderType == OwnerType.User)
        {
            return await messenger.OpenPageWithIdAsync(HohoemaPageType.UserVideo, videoContent.ProviderId);
        }
        else if (videoContent.ProviderType == OwnerType.Channel)
        {
            return await messenger.OpenPageWithIdAsync(HohoemaPageType.ChannelVideo, videoContent.ProviderId);
        }
        else
        {
            return new NavigationResult() { IsSuccess = false };
        }
    }

    public static async Task<INavigationResult> OpenVideoListPageAsync(this IMessenger messenger, IMylist mylistContent)
    {
        return await messenger.OpenPageWithIdAsync(HohoemaPageType.Mylist, mylistContent.PlaylistId.Id);
    }

    public static async Task<INavigationResult> OpenVideoListPageAsync(this IMessenger messenger, IUser user)
    {
        return await messenger.OpenPageWithIdAsync(HohoemaPageType.UserVideo, user.UserId);
    }

    public static async Task<INavigationResult> OpenVideoListPageAsync(this IMessenger messenger, ITag tag)
    {
        return await messenger.OpenSearchPageAsync(SearchTarget.Tag, tag.Tag);
    }

    public static async Task<INavigationResult> OpenVideoListPageAsync(this IMessenger messenger, ISearchHistory history)
    {
        return await messenger.OpenSearchPageAsync(history.Target, history.Keyword);
    }

    public static async Task<INavigationResult> OpenVideoListPageAsync(this IMessenger messenger, IChannel channel)
    {
        return await messenger.OpenPageWithIdAsync(HohoemaPageType.ChannelVideo, channel.ChannelId);
    }    
}

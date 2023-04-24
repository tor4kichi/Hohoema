#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Contracts.Navigations;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Live;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Pins;
using Hohoema.Models.Playlist;
using Hohoema.ViewModels;
using I18NPortable;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.Channels;
using NiconicoToolkit.Community;
using NiconicoToolkit.Live;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using ZLogger;

namespace Hohoema.Services.Navigations;

public class PageManager : ObservableObject
{
    private readonly IMessenger _messenger;
    private readonly ILogger<PageManager> _logger;

    public AppearanceSettings AppearanceSettings { get; }
    public VideoCacheSettings_Legacy CacheSettings { get; }
    public IScheduler Scheduler { get; }

    public static readonly HashSet<HohoemaPageType> IgnoreRecordNavigationStack = new HashSet<HohoemaPageType>
    {
        HohoemaPageType.Splash,
        HohoemaPageType.PrologueIntroduction,
        HohoemaPageType.EpilogueIntroduction,
    };


    public static readonly HashSet<HohoemaPageType> DontNeedMenuPageTypes = new HashSet<HohoemaPageType>
    {
        HohoemaPageType.Splash,
        HohoemaPageType.PrologueIntroduction,
        HohoemaPageType.NicoAccountIntroduction,
        HohoemaPageType.VideoCacheIntroduction,
        HohoemaPageType.EpilogueIntroduction,
    };

    public PageManager(
        IScheduler scheduler,
        IMessenger messenger,
        ILoggerFactory loggerFactory,
        AppearanceSettings appearanceSettings,
        VideoCacheSettings_Legacy cacheSettings
        )
    {
        Scheduler = scheduler;
        _messenger = messenger;
        _logger = loggerFactory.CreateLogger<PageManager>();
        AppearanceSettings = appearanceSettings;
        CacheSettings = cacheSettings;
    }

    public static bool IsHiddenMenuPage(HohoemaPageType pageType)
    {
        return DontNeedMenuPageTypes.Contains(pageType);
    }


    /// <summary>
    /// バックナビゲーションの暴発防止等に対応したい場合に利用します
    /// </summary>
    public bool PreventBackNavigation { get; internal set; }


    private RelayCommand<object> _OpenPageCommand;
    public RelayCommand<object> OpenPageCommand => _OpenPageCommand
        ?? (_OpenPageCommand = new RelayCommand<object>(parameter =>
        {
            switch (parameter)
            {
                case HohoemaPageType rawPageType:
                    OpenPage(rawPageType);
                    break;
                case string s:
                    {
                        if (Enum.TryParse<HohoemaPageType>(s, out var pageType))
                        {
                            OpenPage(pageType);
                        }

                        break;
                    }

                case FollowItemInfo followItem:
                    switch (followItem.FollowItemType)
                    {
                        case FollowItemType.User:
                            OpenPageWithId(HohoemaPageType.UserInfo, (UserId)followItem.Id);
                            break;
                        case FollowItemType.Tag:
                            this.Search(SearchTarget.Tag, followItem.Id);
                            break;
                        case FollowItemType.Mylist:
                            OpenPageWithId(HohoemaPageType.Mylist, (MylistId)followItem.Id);
                            break;
                        case FollowItemType.Channel:
                            OpenPageWithId(HohoemaPageType.ChannelVideo, (ChannelId)followItem.Id);
                            break;
                        case FollowItemType.Community:
                            OpenPageWithId(HohoemaPageType.Community, (CommunityId)followItem.Id);
                            break;
                        default:
                            break;
                    }
                    break;
                case IPageNavigatable item:
                    if (item.Parameter != null)
                    {
                        OpenPage(item.PageType, item.Parameter);
                    }
                    else
                    {
                        OpenPage(item.PageType, default(string));
                    }
                    break;
                case HohoemaPin pin:
                    OpenPage(pin.PageType, pin.Parameter);
                    break;
                case IVideoContent videoContent:
                    OpenPageWithId(HohoemaPageType.VideoInfomation, videoContent.VideoId);
                    break;
                case ILiveContent liveContent:
                    OpenPageWithId(HohoemaPageType.LiveInfomation, liveContent.LiveId);
                    break;
                case ICommunity communityContent:
                    OpenPageWithId(HohoemaPageType.Community, communityContent.CommunityId);
                    break;
                case IMylist mylistContent:
                    OpenPageWithId(HohoemaPageType.Mylist, mylistContent.MylistId);
                    break;
                case IUser user:
                    OpenPageWithId(HohoemaPageType.UserInfo, user.UserId);
                    break;
                case ITag tag:
                    this.Search(SearchTarget.Tag, tag.Tag);
                    break;
                case ISearchHistory history:
                    this.Search(history.Target, history.Keyword);
                    break;
                case IChannel channel:
                    OpenPageWithId(HohoemaPageType.ChannelVideo, channel.ChannelId);
                    break;
                case IPlaylist playlist:
                    OpenPlaylistPage(playlist);
                    break;
            }
        }));


    private RelayCommand<object> _OpenVideoListPageCommand;
    public RelayCommand<object> OpenVideoListPageCommand => _OpenVideoListPageCommand
        ?? (_OpenVideoListPageCommand = new RelayCommand<object>(parameter =>
        {
            switch (parameter)
            {
                case string s:
                    {
                        if (Enum.TryParse<HohoemaPageType>(s, out var pageType))
                        {
                            OpenPage(pageType);
                        }

                        break;
                    }

                case IVideoContentProvider videoContent:
                    if (videoContent.ProviderType == OwnerType.User)
                    {
                        OpenPageWithId(HohoemaPageType.UserVideo, videoContent.ProviderId);
                    }
                    else if (videoContent.ProviderType == OwnerType.Channel)
                    {
                        OpenPageWithId(HohoemaPageType.ChannelVideo, videoContent.ProviderId);
                    }
                    break;
                case ILiveContent liveContent:
                    OpenPageWithId(HohoemaPageType.LiveInfomation, liveContent.LiveId);
                    break;
                case ICommunity communityContent:
                    OpenPageWithId(HohoemaPageType.CommunityVideo, communityContent.CommunityId);
                    break;
                case IMylist mylistContent:
                    OpenPageWithId(HohoemaPageType.Mylist, mylistContent.PlaylistId.Id);
                    break;
                case IUser user:
                    OpenPageWithId(HohoemaPageType.UserVideo, user.UserId);
                    break;
                case ITag tag:
                    this.Search(SearchTarget.Tag, tag.Tag);
                    break;
                case ISearchHistory history:
                    this.Search(history.Target, history.Keyword);
                    break;
                case IChannel channel:
                    OpenPageWithId(HohoemaPageType.ChannelVideo, channel.ChannelId);
                    break;
            }
        }));



    private RelayCommand<object> _OpenContentOwnerPageCommand;
    public RelayCommand<object> OpenContentOwnerPageCommand => _OpenContentOwnerPageCommand
        ?? (_OpenContentOwnerPageCommand = new RelayCommand<object>(parameter =>
        {
            switch (parameter)
            {
                case IVideoContentProvider videoContent:
                    if (videoContent.ProviderType == OwnerType.User)
                    {
                        var p = new NavigationParameters();
                        p.Add("id", videoContent.ProviderId);
                        OpenPage(HohoemaPageType.UserInfo, p);
                    }
                    else if (videoContent.ProviderType == OwnerType.Channel)
                    {
                        var p = new NavigationParameters();
                        p.Add("id", videoContent.ProviderId);
                        OpenPage(HohoemaPageType.ChannelVideo, p);
                    }

                    break;
                case ILiveContentProvider liveContent:
                    if (liveContent.ProviderType == ProviderType.Community)
                    {
                        var p = new NavigationParameters();
                        p.Add("id", liveContent.ProviderId);
                        OpenPage(HohoemaPageType.Community, p);
                    }
                    break;
                case IMylist mylist:
                    {
                        OpenPageWithId(HohoemaPageType.Mylist, mylist.PlaylistId.Id);
                        break;

                    }
            }
        }
        , parameter => parameter is INiconicoContent
        ));


    public INavigationParameters? CurrentPageNavigationParameters { get; private set; }
    public HohoemaPageType CurrentPageType { get; private set; }

    public bool OpenPage(Uri uri)
    {
        var path = uri.AbsoluteUri;
        // is mylist url?
        if (path.StartsWith("http://www.nicovideo.jp/mylist/") || path.StartsWith("https://www.nicovideo.jp/mylist/"))
        {
            var mylistId = uri.AbsolutePath.Split('/').Last();
            OpenPageWithId(HohoemaPageType.Mylist, mylistId);
            return true;
        }


        if (path.StartsWith("http://www.nicovideo.jp/watch/") || path.StartsWith("https://www.nicovideo.jp/watch/"))
        {
            // is nico video url?
            var videoId = uri.AbsolutePath.Split('/').Last();
            _messenger.Send(new VideoPlayRequestMessage() { VideoId = videoId });
            return true;
        }

        if (path.StartsWith("http://com.nicovideo.jp/community/") || path.StartsWith("https://com.nicovideo.jp/community/"))
        {
            var communityId = uri.AbsolutePath.Split('/').Last();
            OpenPageWithId(HohoemaPageType.Community, communityId);

            return true;
        }

        if (path.StartsWith("http://com.nicovideo.jp/user/") || path.StartsWith("https://com.nicovideo.jp/user/"))
        {
            var userId = uri.AbsolutePath.Split('/').Last();
            OpenPageWithId(HohoemaPageType.UserInfo, userId);

            return true;
        }

        if (path.StartsWith("http://ch.nicovideo.jp/") || path.StartsWith("https://ch.nicovideo.jp/"))
        {
            var elem = uri.AbsolutePath.Split('/');
            if (elem.Any(x => x == "article" || x == "blomaga"))
            {
                return false;
            }
            else
            {
                var channelId = elem.Last();
                OpenPageWithId(HohoemaPageType.ChannelVideo, channelId);
            }

            return true;
        }

        _logger.ZLogWarning("Urlを処理できませんでした : {0}", uri.OriginalString);

        return false;
    }

    public void OpenDebugPage()
    {
        _messenger.Send(new NavigationAsyncRequestMessage(new(nameof(Views.Pages.Hohoema.DebugPage), behavior: NavigationStackBehavior.NotRemember)));
    }

    public void OpenPage(HohoemaPageType pageType, INavigationParameters? parameter = null, NavigationStackBehavior stackBehavior = NavigationStackBehavior.Push)
    {
        try
        {
            CurrentPageType = pageType;
            CurrentPageNavigationParameters = parameter;

            _messenger.Send(new NavigationAsyncRequestMessage(_pageTypeToName[pageType], parameter, true, stackBehavior));
        }
        catch (Exception e)
        {
            _logger.ZLogError(e, "OpenPage failed.");
        }
    }

    public void OpenPlaylistPage(IPlaylist playlist, NavigationStackBehavior stackBehavior = NavigationStackBehavior.Push)
    {
        if (playlist.PlaylistId == QueuePlaylist.Id)
        {
            OpenPage(HohoemaPageType.VideoQueue);
        }
        else if (playlist.PlaylistId.Origin is PlaylistItemsSourceOrigin.SearchWithKeyword)
        {
            this.Search(SearchTarget.Keyword, playlist.PlaylistId.Id);
        }
        else if (playlist.PlaylistId.Origin is PlaylistItemsSourceOrigin.SearchWithTag)
        {
            this.Search(SearchTarget.Tag, playlist.PlaylistId.Id);
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
            OpenPage(pageType, parameter, stackBehavior);
        }
    }

    public void OpenPage(HohoemaPageType pageType, string parameterString, NavigationStackBehavior stackBehavior = NavigationStackBehavior.Push)
    {
        INavigationParameters parameter = parameterString is not null ? new NavigationParameters(parameterString) : new NavigationParameters();
        OpenPage(pageType, parameter, stackBehavior);
    }

    public void OpenPageWithId<T>(HohoemaPageType pageType, T id, NavigationStackBehavior stackBehavior = NavigationStackBehavior.Push)
    {
        INavigationParameters parameter = new NavigationParameters(("id", id));
        OpenPage(pageType, parameter, stackBehavior);
    }


    static readonly Dictionary<HohoemaPageType, string> _pageTypeToName = Enum.GetValues(typeof(HohoemaPageType))
        .Cast<HohoemaPageType>().Select(x => (x, x + "Page")).ToDictionary(x => x.x, x => x.Item2);
    public bool IsIgnoreRecordPageType(HohoemaPageType pageType)
    {
        return IgnoreRecordNavigationStack.Contains(pageType);
    }

    public void ForgetLastPage()
    {
        // TODO: ナビゲーション履歴の削除

    }



    /// <summary>
    /// 外部で戻る処理が行われた際にPageManager上での整合性を取ります
    /// </summary>
    /*
		public void OnNavigated(INavigationParameters parameters)
		{
        var navigationMode = parameters.GetNavigationMode();

        if (navigationMode == NavigationMode.Back || navigationMode == NavigationMode.Forward)
			{
            string pageTypeString = null;
            
            if (e.SourcePageType.Name.EndsWith("Page"))
            {
                pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page"));
            }
            else if (e.SourcePageType.Name.EndsWith("TV"))
            {
                pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page_TV"));
            }
            else if (e.SourcePageType.Name.EndsWith("Mobile"))
            {
                pageTypeString = e.SourcePageType.Name.Remove(e.SourcePageType.Name.IndexOf("Page_Mobile"));
            }


            if (pageTypeString != null)
            { 
                HohoemaPageType pageType;
					if (Enum.TryParse(pageTypeString, out pageType))
					{
						try
						{
							PageNavigating = true;

							CurrentPageType = pageType;
						}
						finally
						{
							PageNavigating = false;
						}

						System.Diagnostics.Debug.WriteLine($"navigated : {pageType.ToString()}");
					}
					else
					{
						throw new NotSupportedException();
					}

                PageNavigationParameter = e.Parameter;
            }
            else
				{
					throw new Infra.HohoemaExpception();
				}
			}
		}
    */

    /// <summary>
    /// 画面遷移の履歴を消去します
    /// </summary>
    /// <remarks>
    /// ログイン後にログイン画面の表示履歴を消す時や
    /// ログアウト後にログイン状態中の画面遷移を消すときに利用します。
    /// </remarks>
    public void ClearNavigateHistory()
    {
        Scheduler.Schedule(() =>
        {
            // TODO: ナビゲーションスタックのクリア
        });

    }

    public string CurrentDefaultPageTitle()
    {
        return CurrentPageType.Translate();
    }


    public void OpenIntroductionPage()
    {
        OpenPage(HohoemaPageType.PrologueIntroduction);
    }


    public static string PageTypeToTitle(HohoemaPageType pageType)
    {
        return pageType.Translate();
    }
}

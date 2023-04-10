#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.AppLifecycle;
using Hohoema.Models.Application;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Playlist;
using Hohoema.Models.VideoCache;
using Hohoema.Services.Player.Events;
using Hohoema.Services.Playlist;
using LiteDB;
using Microsoft.Toolkit.Uwp.Notifications;
using NiconicoToolkit.Live;
using NiconicoToolkit.Video;
using System;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace Hohoema.Services.Navigations;

public sealed class NavigationTriggerFromExternal 
    : IToastActivationAware
{
    private readonly MylistResolver _mylistResolver;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly VideoCacheManager _videoCacheManager;
    private readonly PageManager _pageManager;
    private readonly IMessenger _messenger;

    public NavigationTriggerFromExternal(
        MylistResolver mylistResolver,
        NicoVideoProvider nicoVideoProvider, 
        VideoCacheManager videoCacheManager,
        PageManager pageManager,
        IMessenger messenger
        )
    {
        _mylistResolver = mylistResolver;
        _nicoVideoProvider = nicoVideoProvider;
        _videoCacheManager = videoCacheManager;
        _pageManager = pageManager;
        _messenger = messenger;
    }

    async ValueTask<bool> IToastActivationAware.TryHandleActivationAsync(ToastArguments toastArguments, ValueSet userInput)
    {
        bool isHandled = false;
        if (toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_Action, out string actionType))
        {
            switch (actionType)
            {
                case ToastNotificationConstants.ToastArgumentValue_Action_DeleteCache:
                    isHandled = true;
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_Id, out string id))
                        {
                            throw new Infra.HohoemaException("no id");
                        }

                        await _videoCacheManager.CancelCacheRequestAsync(id);
                    }
                    break;
                case ToastNotificationConstants.ToastArgumentValue_Action_PlayVideo:
                    isHandled = true;
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_Id, out string id))
                        {
                            throw new Infra.HohoemaException("no id");
                        }

                        await PlayVideoFromExternal(id);
                    }
                    break;
                case ToastNotificationConstants.ToastArgumentValue_Action_PlayPlaylist:
                    isHandled = true;
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_PlaylistId, out string playlistId))
                        {
                            throw new Infra.HohoemaException("no id");
                        }

                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_PlaylistOrigin, out string playlistOrigin)
                            || !Enum.TryParse<PlaylistItemsSourceOrigin>(playlistOrigin, out var origin)
                            )
                        {
                            throw new Infra.HohoemaException("no id");
                        }

                        await PlayPlaylistFromExternal(origin, playlistId);
                    }
                    break;
                case ToastNotificationConstants.ToastArgumentValue_Action_OpenPage:
                    isHandled = true;
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_PageType, out string pageTypeStr))
                        {
                            throw new Infra.HohoemaException("no pageType");
                        }

                        if (!Enum.TryParse<HohoemaPageType>(pageTypeStr, out var pageType))
                        {
                            throw new Infra.HohoemaException("no supported pageType: " + pageTypeStr);
                        }

                        if (toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_PageParameters, out string parameters))
                        {
                            _pageManager.OpenPage(pageType, parameters);
                        }
                        else
                        {
                            _pageManager.OpenPage(pageType);
                        }
                    }
                    break;
                
            }
        }

        return isHandled;
    }

    public async Task PlayVideoFromExternal(VideoId videoId)
    {
        var result = await _messenger.Send(new VideoPlayRequestMessage() { VideoId = videoId });
    }

    public async Task PlayPlaylistFromExternal(PlaylistItemsSourceOrigin origin, string playlistId)
    {
        var result = await _messenger.Send(new VideoPlayRequestMessage() { PlaylistId = playlistId, PlaylistOrigin = origin });
    }

    public async Task PlayPlaylistFromExternal(VideoId videoId, PlaylistItemsSourceOrigin origin, string playlistId)
    {
        var result = await _messenger.Send(new VideoPlayRequestMessage() { PlaylistId = playlistId, PlaylistOrigin = origin });
    }

    public void PlayLiveVideoFromExternal(LiveId liveId)
    {
        _messenger.Send(new PlayerPlayLiveRequestMessage(new() { LiveId = liveId }));
    }
}

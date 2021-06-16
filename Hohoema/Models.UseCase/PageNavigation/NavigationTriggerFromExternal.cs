using Hohoema.Models.Domain.Application;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.Domain.VideoCache;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Models.UseCase.Player;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Notifications;
using NiconicoToolkit.Live;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.PageNavigation
{
    public sealed class NavigationTriggerFromExternal
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PlaylistResolver _playlistResolver;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly VideoCacheManager _videoCacheManager;
        private readonly PageManager _pageManager;
        private readonly IMessenger _messenger;

        public NavigationTriggerFromExternal(
            HohoemaPlaylist hohoemaPlaylist,
            PlaylistResolver playlistResolver,
            NicoVideoProvider nicoVideoProvider, 
            VideoCacheManager videoCacheManager,
            PageManager pageManager,
            IMessenger messenger
            )
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            _playlistResolver = playlistResolver;
            _nicoVideoProvider = nicoVideoProvider;
            _videoCacheManager = videoCacheManager;
            _pageManager = pageManager;
            _messenger = messenger;
        }

        public async Task Process(string arguments)
        {
            var toastArguments = ToastArguments.Parse(arguments);
            if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_Action, out string actionType))
            {
                return;
            }

            switch (actionType)
            {
                case ToastNotificationConstants.ToastArgumentValue_Action_DeleteCache:
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_Id, out string id))
                        {
                            throw new Models.Infrastructure.HohoemaExpception("no id");
                        }

                        await _videoCacheManager.CancelCacheRequestAsync(id);
                    }
                    break;
                case ToastNotificationConstants.ToastArgumentValue_Action_PlayVideo:
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_Id, out string id))
                        {
                            throw new Models.Infrastructure.HohoemaExpception("no id");
                        }

                        await PlayVideoFromExternal(id);
                    }
                    break;
                case ToastNotificationConstants.ToastArgumentValue_Action_PlayPlaylist:
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_PlaylistId, out string playlistId))
                        {
                            throw new Models.Infrastructure.HohoemaExpception("no id");
                        }

                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_PlaylistOrigin, out string playlistOrigin)
                            || !Enum.TryParse<PlaylistOrigin>(playlistOrigin, out var origin)
                            )
                        {
                            throw new Models.Infrastructure.HohoemaExpception("no id");
                        }

                        await PlayPlaylistFromExternal(origin, playlistId);
                    }
                    break;
                case ToastNotificationConstants.ToastArgumentValue_Action_OpenPage:
                    {
                        if (!toastArguments.TryGetValue(ToastNotificationConstants.ToastArgumentKey_PageType, out string pageTypeStr))
                        {
                            throw new Models.Infrastructure.HohoemaExpception("no pageType");
                        }

                        if (!Enum.TryParse<HohoemaPageType>(pageTypeStr, out var pageType))
                        {
                            throw new Models.Infrastructure.HohoemaExpception("no supported pageType: " + pageTypeStr);
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



        public async Task PlayVideoFromExternal(VideoId videoId, string playlistId = null)
        {
            var (res, videoInfo) = await _nicoVideoProvider.GetVideoInfoAsync(videoId);

            if (videoInfo == null || res.Video.IsDeleted) { return; }

            if (playlistId == null)
            {
                _hohoemaPlaylist.Play(videoInfo);
            }
            else 
            {
                var playlist = await _playlistResolver.ResolvePlaylistAsync(Models.Domain.Playlist.PlaylistOrigin.Mylist, playlistId);
                if (playlist != null)
                {
                    _hohoemaPlaylist.Play(videoInfo, playlist);
                }
                else 
                {
                    _hohoemaPlaylist.Play(videoInfo);

                    // TODO: 指定したプレイリストが見つからなかった旨を通知
                }
            }
        }

        public async Task PlayPlaylistFromExternal(PlaylistOrigin origin, string playlistId)
        {
            var playlist = await _playlistResolver.ResolvePlaylistAsync(origin, playlistId);
            _hohoemaPlaylist.Play(playlist);
        }

        public void PlayLiveVideoFromExternal(LiveId liveId)
        {
            StrongReferenceMessenger.Default.Send(new PlayerPlayLiveRequestMessage(new() { LiveId = liveId }));
        }


    }
}

using I18NPortable;
using Hohoema.UseCase.Playlist;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Hohoema.Events;
using Hohoema.Models.Helpers;
using Hohoema.Models.Repository.Niconico.NicoVideo;
using Hohoema.Models.Repository.NicoLive;
using Hohoema.Models.Repository.Niconico;
using Hohoema.Models.Repository.Niconico.Community;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.UseCase.Services;
using Hohoema.UseCase.Events;
using Hohoema.Models.Pages;
using Hohoema.ViewModels.Pages;

namespace Hohoema.Services
{
    public sealed class ContentSuggenstionHelper
    {
        private static readonly TimeSpan DefaultNotificationShowDuration = TimeSpan.FromSeconds(20);
        
        
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PageManager _pageManager;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly MylistProvider _mylistProvider;
        private readonly NicoLiveProvider _nicoLiveProvider;
        private readonly CommunityProvider _communityProvider;
        private readonly UserProvider _userProvider;

        public ContentSuggenstionHelper(
            HohoemaPlaylist hohoemaPlaylist,
            PageManager pageManager,
            NicoVideoProvider nicoVideoProvider,
            MylistProvider mylistProvider,
            NicoLiveProvider nicoLiveProvider,
            CommunityProvider communityProvider,
            UserProvider userProvider
            )
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            _pageManager = pageManager;
            _nicoVideoProvider = nicoVideoProvider;
            _mylistProvider = mylistProvider;
            _nicoLiveProvider = nicoLiveProvider;
            _communityProvider = communityProvider;
            _userProvider = userProvider;
        }





        internal async Task<InAppNotificationPayload> SubmitVideoContentSuggestion(string videoId)
        {
            var nicoVideo = await _nicoVideoProvider.GetNicoVideoInfo(videoId);

            if (nicoVideo.IsDeleted || string.IsNullOrEmpty(nicoVideo.Title)) { return null; }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(nicoVideo.Title),
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "Play".Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _hohoemaPlaylist.Play(nicoVideo);
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "@view".Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _hohoemaPlaylist.AddWatchAfterPlaylist(nicoVideo);
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.VideoInfomation.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _pageManager.OpenPageWithId(HohoemaPageType.VideoInfomation, videoId);
                            })
                        },
                    }
            };
        }

        internal async Task<InAppNotificationPayload> SubmitLiveContentSuggestion(string liveId)
        {
            /*
            var liveDesc = await NicoLiveProvider.GetLiveInfoAsync(liveId);

            if (liveDesc == null) { return null; }

            var liveTitle = liveDesc.VideoInfo.Video.Title;

            var payload = new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(liveTitle),
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "WatchLiveStreaming".Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _eventAggregator.GetEvent<Services.Player.PlayerPlayLiveRequest>()
                                    .Publish(new Services.Player.PlayerPlayLiveRequestEventArgs() { LiveId = liveId });

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.LiveInfomation.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPageWithId(HohoemaPageType.LiveInfomation, liveId);

                                NotificationService.DismissInAppNotification();
                            })
                        },

                    }
            };

            if (liveDesc.VideoInfo.Community != null)
            {
                payload.Commands.Add(new InAppNotificationCommand()
                {
                    Label = HohoemaPageType.Community.Translate(),
                    Command = new DelegateCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.Community, liveDesc.VideoInfo.Community.GlobalId);

                        NotificationService.DismissInAppNotification();
                    })
                });
            }

            return payload;
            */

            throw new NotImplementedException();
        }

        internal async Task<InAppNotificationPayload> SubmitMylistContentSuggestion(string mylistId)
        {
            var result = await _mylistProvider.GetMylistGroupDetail(mylistId);

            if (result == null || !result.IsOK) { return null; }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(result.Name),
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.Mylist.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _pageManager.OpenPageWithId(HohoemaPageType.Mylist, mylistId);
                            })
                        },
                    }
            };
        }

        internal async Task<InAppNotificationPayload> SubmitCommunityContentSuggestion(string communityId)
        {
            var communityInfo = await _communityProvider.GetCommunityInfo(communityId);

            if (communityInfo == null) { return null; }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(communityInfo.Name),
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.Community.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _pageManager.OpenPageWithId(HohoemaPageType.Community, communityId);
                            })
                        },
                    }
            };
        }

        internal async Task<InAppNotificationPayload> SubmitUserSuggestion(string userId)
        {
            var user = await _userProvider.GetUser(userId);

            if (user == null) { return null; }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(user.ScreenName),
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.UserInfo.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _pageManager.OpenPageWithId(HohoemaPageType.UserInfo, userId);
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.UserVideo.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _pageManager.OpenPageWithId(HohoemaPageType.UserVideo, userId);
                            })
                        },
                    }
            };
        }


    }

}

﻿using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.PageNavigation;
using Hohoema.Models.UseCase.PageNavigation;
using I18NPortable;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Notifications;
using NiconicoToolkit.Live;
using Prism.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Controls;
using Hohoema.Models.Domain.Notification;
using NiconicoToolkit.Community;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using Hohoema.Models.Domain.Niconico;
using NiconicoToolkit;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.UseCase.Niconico.Player.Events;
using Hohoema.Models.Domain.Playlist;

namespace Hohoema.Presentation.Services
{
    public sealed class HohoemaNotificationService
    {
        private static readonly TimeSpan DefaultNotificationShowDuration = TimeSpan.FromSeconds(20);

        public PageManager PageManager { get; }
        public NotificationService NotificationService { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public MylistProvider MylistProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public CommunityProvider CommunityProvider { get; }
        public UserProvider UserProvider { get; }

        private readonly IMessenger _messenger;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;
        private readonly NiconicoSession _niconicoSession;

        public HohoemaNotificationService(
            PageManager pageManager,
            QueuePlaylist queuePlaylist,
            HohoemaPlaylistPlayer hohoemaPlaylistPlayer,
            NiconicoSession niconicoSession,
            NotificationService notificationService,
            NicoVideoProvider nicoVideoProvider,
            MylistProvider mylistProvider,
            NicoLiveProvider nicoLiveProvider,
            CommunityProvider communityProvider,
            UserProvider userProvider,
            IMessenger messenger
            )
        {
            PageManager = pageManager;
            _queuePlaylist = queuePlaylist;
            _niconicoSession = niconicoSession;
            NotificationService = notificationService;
            NicoVideoProvider = nicoVideoProvider;
            MylistProvider = mylistProvider;
            NicoLiveProvider = nicoLiveProvider;
            CommunityProvider = communityProvider;
            UserProvider = userProvider;

            _messenger = messenger;
        }


        public async void ShowInAppNotification(NiconicoId id)
        {
            Task<InAppNotificationPayload> notificationPayload = null;
            switch (id.IdType)
            {
                case NiconicoIdType.Video:
                    notificationPayload = SubmitVideoContentSuggestion((VideoId)id);
                    break;
                case NiconicoIdType.Live:
                    notificationPayload = SubmitLiveContentSuggestion((LiveId)id);
                    break;
                case NiconicoIdType.Mylist:
                    notificationPayload = SubmitMylistContentSuggestion((MylistId)id);
                    break;
                case NiconicoIdType.Community:
                    notificationPayload = SubmitCommunityContentSuggestion((CommunityId)id);
                    break;
                case NiconicoIdType.User:
                    notificationPayload = SubmitUserSuggestion((UserId)id);
                    break;
                case NiconicoIdType.Channel:
                    //notificationPayload = SubmitChannelSuggestion(id);
                    break;
                case NiconicoIdType.Series:
                    notificationPayload = SubmitSeriesSuggestion(id);
                    break;
                default:
                    break;
            }

            if (notificationPayload != null)
            {
                NotificationService.ShowInAppNotification(await notificationPayload);
            }
        }



        private async Task<InAppNotificationPayload> SubmitVideoContentSuggestion(VideoId videoId)
        {
            var (res, nicoVideo) = await NicoVideoProvider.GetVideoInfoAsync(videoId);

            if (res.Video.IsDeleted || string.IsNullOrEmpty(nicoVideo.Title)) { return null; }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(nicoVideo.Title),
                ShowDuration = DefaultNotificationShowDuration,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "Play".Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _messenger.Send(VideoPlayRequestMessage.PlayVideoWithQueue(videoId));

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "@view".Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _queuePlaylist.Add(nicoVideo);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.VideoInfomation.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPageWithId(HohoemaPageType.VideoInfomation, videoId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                    }
            };
        }

        private async Task<InAppNotificationPayload> SubmitLiveContentSuggestion(LiveId liveId)
        {
            var liveDesc = await NicoLiveProvider.GetLiveInfoAsync(liveId);

            if (liveDesc == null) { return null; }

            var liveTitle = liveDesc.Data.Title;

            var payload = new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(liveTitle),
                ShowDuration = DefaultNotificationShowDuration,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "WatchLiveStreaming".Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                _messenger.Send(new PlayerPlayLiveRequestMessage(new() { LiveId = liveId }));

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

            if (liveDesc.Data.ProviderType == ProviderType.Community)
            {
                payload.Commands.Add(new InAppNotificationCommand()
                {
                    Label = HohoemaPageType.Community.Translate(),
                    Command = new DelegateCommand(() =>
                    {
                        PageManager.OpenPageWithId(HohoemaPageType.Community, liveDesc.Data.ProviderId);

                        NotificationService.DismissInAppNotification();
                    })
                });
            }

            return payload;
        }

        private async Task<InAppNotificationPayload> SubmitMylistContentSuggestion(MylistId mylistId)
        {
            MylistPlaylist mylistDetail = null;
            try
            {
                mylistDetail = await MylistProvider.GetMylist(mylistId);
            }
            catch { }

            if (mylistDetail == null) { return null; }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(mylistDetail.Name),
                ShowDuration = DefaultNotificationShowDuration,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.Mylist.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPageWithId(HohoemaPageType.Mylist, mylistId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                    }
            };
        }

        private async Task<InAppNotificationPayload> SubmitCommunityContentSuggestion(CommunityId communityId)
        {
            CommunityInfoResponse communityInfo = null;
            try
            {
                communityInfo = await CommunityProvider.GetCommunityInfo(communityId);
            }
            catch { }

            if (communityInfo?.IsOK != true || communityInfo.Community == null) { return null; }

            var community = communityInfo.Community;
            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(community.Name),
                ShowDuration = DefaultNotificationShowDuration,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.Community.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPageWithId(HohoemaPageType.Community, communityId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                    }
            };
        }

        private async Task<InAppNotificationPayload> SubmitUserSuggestion(UserId userId)
        {
            var user = await UserProvider.GetUserInfoAsync(userId);

            if (user == null) { return null; }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(user.ScreenName),
                ShowDuration = DefaultNotificationShowDuration,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.UserInfo.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPageWithId(HohoemaPageType.UserInfo, userId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.UserVideo.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPageWithId(HohoemaPageType.UserVideo, userId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                    }
            };
        }
        /*
        private async Task<InAppNotificationPayload> SubmitChannelSuggestion(string channelIdOrScreenName)
        {
            var channel = await _niconicoSession.ToolkitContext.Channel.GetChannelVideoAsync()
        }
        */
        private async Task<InAppNotificationPayload> SubmitSeriesSuggestion(string seriesId)
        {
            var series = await _niconicoSession.ToolkitContext.Series.GetSeriesVideosAsync(seriesId);

            if (!(series.Videos?.Any() ?? false))
            {
                return null;
            }

            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(series.Series.Title),
                ShowDuration = DefaultNotificationShowDuration,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = HohoemaPageType.Series.Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPageWithId(HohoemaPageType.Series, seriesId);

                                NotificationService.DismissInAppNotification();
                            })
                        }
                    }
            };
        }
    }

    public sealed class NotificationService : INotificationService
    {
        private readonly ToastNotifier _notifier;
        private ToastNotification _prevToastNotification;
        private readonly IMessenger _messenger;

        public NotificationService(IMessenger messenger)
        {
            _messenger = messenger;
            _notifier = ToastNotificationManager.CreateToastNotifier();
            ToastNotificationManager.History.Clear();
        }


        public void ShowToast(string title, string content, ToastDuration duration = ToastDuration.Short, bool isSuppress = false, string luanchContent = null, Action toastActivatedAction = null)
        {
            var toust = new ToastContent();
            toust.Visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },

                        new AdaptiveText()
                        {
                            Text = content,

                        },


                    }
                }
            };

            toust.Launch = luanchContent;
            toust.Duration = duration;


            var toast = new ToastNotification(toust.GetXml());
            toast.SuppressPopup = isSuppress;

            if (toastActivatedAction != null)
            {
                toast.Activated += (ToastNotification sender, object args) => toastActivatedAction();
            }

            _notifier.Show(toast);
            _prevToastNotification = toast;
        }

        public void ShowToast(string title, string content, ToastDuration duration = ToastDuration.Short, bool isSuppress = false, string luanchContent = null, Action toastActivatedAction = null, params ToastButton[] toastButtons)
        {
            var toust = new ToastContent();
            toust.Visual = new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText()
                        {
                            Text = title
                        },

                        new AdaptiveText()
                        {
                            Text = content,

                        },
                    }
                }
            };

            toust.Launch = luanchContent;
            toust.Duration = duration;

            if (toastButtons.Any())
            {
                var actions = new ToastActionsCustom();
                foreach (var button in toastButtons)
                {
                    actions.Buttons.Add(button);
                }
                toust.Actions = actions;

            }
            var toast = new ToastNotification(toust.GetXml());
            toast.SuppressPopup = isSuppress;

            if (toastActivatedAction != null)
            {
                toast.Activated += (ToastNotification sender, object args) => toastActivatedAction();
            }

            _notifier.Show(toast);
            _prevToastNotification = toast;
        }




        public void ShowInAppNotification(InAppNotificationPayload payload)
        {
            _messenger.Send(new InAppNotificationMessage(payload));
        }



        public void DismissInAppNotification()
        {
            _messenger.Send(new InAppNotificationDismissMessage());
        }

        public void HideToast()
        {
            ToastNotificationManager.History.Clear();
        }


        public void ShowLiteInAppNotification_Success(string content, DisplayDuration? displayDuration = null)
        {
            ShowLiteInAppNotification(content, displayDuration, Symbol.Accept);
        }

        public void ShowLiteInAppNotification_Success(string content, TimeSpan duration)
        {
            ShowLiteInAppNotification(content, duration, Symbol.Accept);
        }

        public void ShowLiteInAppNotification_Fail(string content, DisplayDuration? displayDuration = null)
        {
            ShowLiteInAppNotification(content, displayDuration ?? DisplayDuration.MoreAttention, Symbol.Important);
        }

        public void ShowLiteInAppNotification_Fail(string content, TimeSpan duration)
        {
            ShowLiteInAppNotification(content, duration, Symbol.Important);
        }


        public void ShowLiteInAppNotification(string content, DisplayDuration? displayDuration = null, Symbol? symbol = null)
        {
            _messenger.Send(new LiteNotificationMessage(new()
            {
                Content = content,
                Symbol = symbol ?? default,
                IsDisplaySymbol = symbol is not null,
                DisplayDuration = displayDuration
            }));
        }

        public void ShowLiteInAppNotification(string content, TimeSpan duration, Symbol? symbol = null)
        {
            _messenger.Send(new LiteNotificationMessage(new()
            {
                Content = content,
                Symbol = symbol ?? default,
                IsDisplaySymbol = symbol is not null,
                Duration = duration
            }));
        }

    }
}

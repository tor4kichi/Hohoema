using I18NPortable;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Notifications;
using Mntone.Nico2.Users.Mylist;
using NiconicoLiveToolkit.Live;
using Hohoema.Models.Domain.Niconico.Community;
using Hohoema.Models.Domain.Niconico.Live;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.UserFeature.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.UseCase.NicoVideos;
using Hohoema.Presentation.Services.Helpers;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Controls;
using Hohoema.Presentation.Services.Page;
using Hohoema.Models.Domain.PageNavigation;

namespace Hohoema.Presentation.Services
{
    public sealed class HohoemaNotificationService
    {
        private static readonly TimeSpan DefaultNotificationShowDuration = TimeSpan.FromSeconds(20);
        private readonly IEventAggregator _eventAggregator;

        public PageManager PageManager { get; }
        public HohoemaPlaylist Playlist { get; }
        public NotificationService NotificationService { get; }
        public NicoVideoProvider NicoVideoProvider { get; }
        public MylistProvider MylistProvider { get; }
        public NicoLiveProvider NicoLiveProvider { get; }
        public CommunityProvider CommunityProvider { get; }
        public UserProvider UserProvider { get; }

        public HohoemaNotificationService(
            IEventAggregator eventAggregator,
            PageManager pageManager,
            HohoemaPlaylist playlist,
            NotificationService notificationService,
            NicoVideoProvider nicoVideoProvider,
            MylistProvider mylistProvider,
            NicoLiveProvider nicoLiveProvider,
            CommunityProvider communityProvider,
            UserProvider userProvider
            )
        {
            _eventAggregator = eventAggregator;
            PageManager = pageManager;
            Playlist = playlist;
            NotificationService = notificationService;
            NicoVideoProvider = nicoVideoProvider;
            MylistProvider = mylistProvider;
            NicoLiveProvider = nicoLiveProvider;
            CommunityProvider = communityProvider;
            UserProvider = userProvider;
        }


        public async void ShowInAppNotification(ContentType type, string id)
        {
            Task<InAppNotificationPayload> notificationPayload = null;
            switch (type)
            {
                case ContentType.Video:
                    notificationPayload = SubmitVideoContentSuggestion(id);
                    break;
                case ContentType.Live:
                    notificationPayload = SubmitLiveContentSuggestion(id);
                    break;
                case ContentType.Mylist:
                    notificationPayload = SubmitMylistContentSuggestion(id);
                    break;
                case ContentType.Community:
                    notificationPayload = SubmitMylistContentSuggestion(id);
                    break;
                case ContentType.User:
                    notificationPayload = SubmitMylistContentSuggestion(id);
                    break;
                case ContentType.Channel:

                    // TODO: 
                    break;
                default:
                    break;
            }

            if (notificationPayload != null)
            {
                NotificationService.ShowInAppNotification(await notificationPayload);
            }
        }



        private async Task<InAppNotificationPayload> SubmitVideoContentSuggestion(string videoId)
        {
            var nicoVideo = await NicoVideoProvider.GetNicoVideoInfo(videoId);

            if (nicoVideo.IsDeleted || string.IsNullOrEmpty(nicoVideo.Title)) { return null; }

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
                                Playlist.Play(nicoVideo);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "@view".Translate(),
                            Command = new DelegateCommand(() =>
                            {
                                Playlist.AddWatchAfterPlaylist(nicoVideo);

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

        private async Task<InAppNotificationPayload> SubmitLiveContentSuggestion(string liveId)
        {
            var liveDesc = await NicoLiveProvider.NiconicoSession.LiveContext.Live.CasApi.GetLiveProgramAsync(liveId);

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

            if (liveDesc.Data.ProviderType == ProviderType.Community.ToString().ToLower())
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

        private async Task<InAppNotificationPayload> SubmitMylistContentSuggestion(string mylistId)
        {
            Mylist mylistDetail = null;
            try
            {
                mylistDetail = await MylistProvider.GetMylistGroupDetail(mylistId);
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

        private async Task<InAppNotificationPayload> SubmitCommunityContentSuggestion(string communityId)
        {
            Mntone.Nico2.Communities.Detail.CommunityDetailResponse communityDetail = null;
            try
            {
                communityDetail = await CommunityProvider.GetCommunityDetail(communityId);
            }
            catch { }

            if (communityDetail == null || !communityDetail.IsStatusOK) { return null; }

            var communityInfo = communityDetail.CommunitySammary.CommunityDetail;
            return new InAppNotificationPayload()
            {
                Content = "InAppNotification_ContentDetectedFromClipboard".Translate(communityInfo.Name),
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

        private async Task<InAppNotificationPayload> SubmitUserSuggestion(string userId)
        {
            var user = await UserProvider.GetUser(userId);

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


    }

    public sealed class NotificationService
	{
        public IEventAggregator EventAggregator { get; }

        private readonly ToastNotifier _notifier;

        private ToastNotification _prevToastNotification;

        public NotificationService(
            IEventAggregator ea
            )
		{
            EventAggregator = ea;
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
            var notificationEvent = EventAggregator.GetEvent<InAppNotificationEvent>();
            notificationEvent.Publish(payload);
        }

        

        public void DismissInAppNotification()
        {
            var notificationDismissEvent = EventAggregator.GetEvent<InAppNotificationDismissEvent>();
            notificationDismissEvent.Publish(0);
        }

        public void HideToast()
        {
            ToastNotificationManager.History.Clear();
        }
    }
}

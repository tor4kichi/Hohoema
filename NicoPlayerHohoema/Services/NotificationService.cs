using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Notifications;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.Services.Page;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml.Controls;

namespace NicoPlayerHohoema.Services
{
    public sealed class HohoemaNotificationService
    {
        private static readonly TimeSpan DefaultNotificationShowDuration = TimeSpan.FromSeconds(20);

        public PageManager PageManager { get; }
        public HohoemaPlaylist Playlist { get; }
        public NotificationService NotificationService { get; }
        public Models.Provider.NicoVideoProvider NicoVideoProvider { get; }
        public Models.Provider.MylistProvider MylistProvider { get; }
        public Models.Provider.NicoLiveProvider NicoLiveProvider { get; }
        public Models.Provider.CommunityProvider CommunityProvider { get; }
        public Models.Provider.UserProvider UserProvider { get; }

        public HohoemaNotificationService(
            PageManager pageManager,
            HohoemaPlaylist playlist,
            NotificationService notificationService,
            Models.Provider.NicoVideoProvider nicoVideoProvider,
            Models.Provider.MylistProvider mylistProvider,
            Models.Provider.NicoLiveProvider nicoLiveProvider,
            Models.Provider.CommunityProvider communityProvider,
            Models.Provider.UserProvider userProvider
            )
        {
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
                Content = $"{nicoVideo.Title} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "再生",
                            Command = new DelegateCommand(() =>
                            {
                                Playlist.PlayVideo(nicoVideo.RawVideoId, nicoVideo.Title);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "あとで見る",
                            Command = new DelegateCommand(() =>
                            {
                                Playlist.DefaultPlaylist.AddMylistItem(nicoVideo.RawVideoId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "動画情報を開く",
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPage(HohoemaPageType.VideoInfomation, videoId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                    }
            };
        }

        private async Task<InAppNotificationPayload> SubmitLiveContentSuggestion(string liveId)
        {
            var liveDesc = await NicoLiveProvider.GetLiveInfoAsync(liveId);

            if (liveDesc == null) { return null; }

            var liveTitle = liveDesc.VideoInfo.Video.Title;

            var payload = new InAppNotificationPayload()
            {
                Content = $"{liveTitle} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "視聴する",
                            Command = new DelegateCommand(() =>
                            {
                                Playlist.PlayLiveVideo(liveId, liveTitle);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "放送情報を確認",
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPage(HohoemaPageType.LiveInfomation, liveId);

                                NotificationService.DismissInAppNotification();
                            })
                        },

                    }
            };

            if (liveDesc.VideoInfo.Community != null)
            {
                payload.Commands.Add(new InAppNotificationCommand()
                {
                    Label = "コミュニティを開く",
                    Command = new DelegateCommand(() =>
                    {
                        PageManager.OpenPage(HohoemaPageType.Community, liveDesc.VideoInfo.Community.GlobalId);

                        NotificationService.DismissInAppNotification();
                    })
                });
            }

            return payload;
        }

        private async Task<InAppNotificationPayload> SubmitMylistContentSuggestion(string mylistId)
        {
            Mntone.Nico2.Mylist.MylistGroup.MylistGroupDetailResponse mylistDetail = null;
            try
            {
                mylistDetail = await MylistProvider.GetMylistGroupDetail(mylistId);
            }
            catch { }

            if (mylistDetail == null || !mylistDetail.IsOK) { return null; }

            var mylistGroup = mylistDetail.MylistGroup;
            return new InAppNotificationPayload()
            {
                Content = $"{mylistGroup.Name} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "マイリストを開く",
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPage(HohoemaPageType.Mylist, new MylistPagePayload(mylistId).ToParameterString());

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
                Content = $"{communityInfo.Name} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "コミュニティを開く",
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPage(HohoemaPageType.Community, communityId);

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
                Content = $"{user.ScreenName} をお探しですか？",
                ShowDuration = DefaultNotificationShowDuration,
                SymbolIcon = Symbol.Video,
                IsShowDismissButton = true,
                Commands = {
                        new InAppNotificationCommand()
                        {
                            Label = "ユーザー情報を開く",
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPage(HohoemaPageType.UserInfo, userId);

                                NotificationService.DismissInAppNotification();
                            })
                        },
                        new InAppNotificationCommand()
                        {
                            Label = "動画一覧を開く",
                            Command = new DelegateCommand(() =>
                            {
                                PageManager.OpenPage(HohoemaPageType.UserVideo, userId);

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
        
        public NotificationService(
            IEventAggregator ea
            )
		{
            EventAggregator = ea;
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

            ToastNotificationManager.CreateToastNotifier().Show(toast);
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

            ToastNotificationManager.CreateToastNotifier().Show(toast);
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



    }
}

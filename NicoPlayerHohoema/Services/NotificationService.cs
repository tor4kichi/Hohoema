using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Notifications;
using NicoPlayerHohoema.Models;
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
        public NiconicoContentProvider ContentProvider { get; }
        public NotificationService NotificationService { get; }

        public HohoemaNotificationService(
            PageManager pageManager,
            HohoemaPlaylist playlist,
            NiconicoContentProvider contentProvider,
            NotificationService notificationService
            )
        {
            PageManager = pageManager;
            Playlist = playlist;
            ContentProvider = contentProvider;
            NotificationService = notificationService;
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
            var nicoVideo = await ContentProvider.GetNicoVideoInfo(videoId);

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
                                Playlist.DefaultPlaylist.AddVideo(nicoVideo.RawVideoId, nicoVideo.Title);

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
            var liveDesc = await ContentProvider.GetLiveInfoAsync(liveId);

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
                mylistDetail = await ContentProvider.GetMylistGroupDetail(mylistId);
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
                communityDetail = await ContentProvider.GetCommunityDetail(communityId);
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
            var user = await ContentProvider.GetUserInfo(userId);

            if (user == null) { return null; }

            return new InAppNotificationPayload()
            {
                Content = $"{user.Nickname} をお探しですか？",
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
        public EventAggregator EventAggregator { get; }
        
        public NotificationService(
            EventAggregator ea
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

using Hohoema.NicoAlert;
using NicoPlayerHohoema.Views.Service;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Mntone.Nico2.NicoRepo;

namespace NicoPlayerHohoema.Models
{
    public sealed class HohoemaAlertClient
    {
        public bool IsLiveAlertEnabled { get; set; }

        NicoAlertClient AlertClient = new NicoAlertClient();

        bool _IsLoggedIn;

        public HohoemaAlertClient(ActivityFeedSettings feedSettings)
        {
            feedSettings.ObserveProperty(x => x.IsLiveAlertEnabled)
                .Subscribe(x => 
                {
                    IsLiveAlertEnabled = x;

                    if (IsLiveAlertEnabled)
                    {
                        StartAlert(NiconicoAlertServiceType.Live);
                    }
                    else
                    {
                        EndAlert();
                    }
                });

            AlertClient.VideoRecieved += (sender, args) =>
            {
                Debug.WriteLine("new video recieved!: " + args.Id);

                var toastService = App.Current.Container.Resolve<ToastNotificationService>();

                /*
                var nicoInfo = await ContentProvider.GetNicoVideoInfo(args.Id);
                if (nicoInfo != null)
                {
                    toastService.ShowText($"ニコニコ新着動画", $"{nicoInfo.Title} が投稿されました",
                        luanchContent: "niconico://" + nicoInfo.RawVideoId
                        );
                }
                */
            };

            AlertClient.LiveRecieved += async (sender, args) =>
            {
                var liveId = $"lv{args.Id}";

                Debug.WriteLine("new live recieved!: " + liveId);

                var toastService = App.Current.Container.Resolve<ToastNotificationService>();

                if (_CurrentUserFollows.GetFollowCommunities().Any(x => x == args.CommunityId))
                {
                    var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
                    var liveStatus = await hohoemaApp.NiconicoContext.Live.GetPlayerStatusAsync(liveId);
                    toastService.ShowText($"{liveStatus.Program.BroadcasterName} さんのニコ生開始", $"{liveStatus.Program.Title}",
                        luanchContent: "niconico://" + liveId
                        );
                }
            };

            AlertClient.Connected += (sender, _) =>
            {
                Debug.WriteLine("ニコニコアラートへの接続を開始");
            };

            AlertClient.Disconnected += (sender, _) =>
            {
                Debug.WriteLine("ニコニコアラートへの接続を終了");
            };

        }

        public async Task LoginAlertAsync(string mail, string password)
        {
            _IsLoggedIn = await AlertClient.LoginAsync(mail, password);

            if (IsLiveAlertEnabled)
            {
                StartAlert(NiconicoAlertServiceType.Live);
            }
        }


        FollowInfo _CurrentUserFollows;
        private async void StartAlert(params NiconicoAlertServiceType[] alertServiceTypes)
        {
            if (!_IsLoggedIn) { return; }

            _CurrentUserFollows = await AlertClient.GetFollowsAsync();

            await AlertClient.ConnectAlertWebScoketServerAsync(alertServiceTypes);

            // 既に始まっている生放送を検出して通知
            var hohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
            NicoRepoTimelineItem lastNicoRepoItem = null;
            
            var res = await hohoemaApp.NiconicoContext.NicoRepo.GetLoginUserNicoRepo(Mntone.Nico2.NicoRepo.NicoRepoTimelineType.all, lastNicoRepoItem?.Id);
            foreach (var item in res.TimelineItems)
            {
                var topicType = ViewModels.NicoRepoItemTopicExtension.ToNicoRepoTopicType(item.Topic);
                if (topicType == ViewModels.NicoRepoItemTopic.Live_Channel_Program_Onairs 
                    || topicType == ViewModels.NicoRepoItemTopic.Live_User_Program_OnAirs)
                {
                    if (item.Program != null)
                    {
                        try
                        {
                            var liveStatus = await hohoemaApp.NiconicoContext.Live.GetPlayerStatusAsync(item.Program.Id);
                            var toastService = App.Current.Container.Resolve<ToastNotificationService>();
                            if (liveStatus.Program.EndedAt.DateTime > DateTime.Now)
                            {
                                toastService.ShowText($"{liveStatus.Program.BroadcasterName} さんのニコ生開始", $"{liveStatus.Program.Title}",
                                    luanchContent: "niconico://" + item.Program.Id
                                    );
                            }
                        }
                        catch { }
                        await Task.Delay(250);
                    }
                }
            }
        }

        private void EndAlert()
        {
            AlertClient.Disconnect();
        }


    }
}

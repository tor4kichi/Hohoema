using Hohoema.NicoAlert;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Mntone.Nico2.NicoRepo;
using NicoPlayerHohoema.Services;

namespace NicoPlayerHohoema.Services
{
    public sealed class HohoemaAlertClient : IDisposable
    {
        public bool IsLiveAlertEnabled { get; set; }

        public static NicoAlertClient AlertClient = new NicoAlertClient();

        public FollowInfo CurrentUserFollows { get; private set; }
        public Models.ActivityFeedSettings FeedSettings { get; }
        public Models.NiconicoSession NiconicoSession { get; }
        public Models.Provider.NicoVideoProvider NicoVideoProvider { get; }
        public NotificationService NotificationService { get; }

        bool _IsLoggedIn;

        public HohoemaAlertClient(
            Models.ActivityFeedSettings feedSettings,
            Models.NiconicoSession niconicoSession,
            Models.Provider.NicoVideoProvider nicoVideoProvider,
            Services.NotificationService notificationService
            )
        {
            FeedSettings = feedSettings;
            NiconicoSession = niconicoSession;
            NicoVideoProvider = nicoVideoProvider;
            NotificationService = notificationService;

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

            AlertClient.VideoRecieved += async (sender, args) =>
            {
                Debug.WriteLine("new video recieved!: " + args.Id);

                var nicoInfo = await NicoVideoProvider.GetNicoVideoInfo(args.Id);
                if (nicoInfo != null)
                {
                    NotificationService.ShowToast($"ニコニコ新着動画", $"{nicoInfo.Title} が投稿されました",
                        luanchContent: "niconico://" + nicoInfo.RawVideoId
                        );
                }
            };

            AlertClient.LiveRecieved += async (sender, args) =>
            {
                var liveId = $"lv{args.Id}";

                Debug.WriteLine("new live recieved!: " + liveId);

                if (CurrentUserFollows.GetFollowCommunities().Any(x => x == args.CommunityId))
                {
                    var liveStatus = await NiconicoSession.Context.Live.GetPlayerStatusAsync(liveId);
                    NotificationService.ShowToast($"{liveStatus.Program.BroadcasterName} さんのニコ生開始", $"{liveStatus.Program.Title}",
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

            if (_IsLoggedIn)
            {
                CurrentUserFollows = await AlertClient.GetFollowsAsync();

                if (IsLiveAlertEnabled)
                {
                    StartAlert(NiconicoAlertServiceType.Live);
                }
            }
        }


        private async void StartAlert(params NiconicoAlertServiceType[] alertServiceTypes)
        {
            if (!_IsLoggedIn) { return; }

            await AlertClient.ConnectAlertWebScoketServerAsync(alertServiceTypes);

            // 既に始まっている生放送を検出して通知
            NicoRepoTimelineItem lastNicoRepoItem = null;
            
            var res = await NiconicoSession.Context.NicoRepo.GetLoginUserNicoRepo(Mntone.Nico2.NicoRepo.NicoRepoTimelineType.all, lastNicoRepoItem?.Id);
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
                            // TODO: GetPlayerStatusAsync を GetLiveVideoInfoAsyncに切り替えたい
                            var liveStatus = await NiconicoSession.Context.Live.GetPlayerStatusAsync(item.Program.Id);
                            var toastService = App.Current.Container.Resolve<NotificationService>();
                            if (liveStatus.Program.EndedAt.DateTime > DateTime.Now)
                            {
                                toastService.ShowToast($"{liveStatus.Program.BroadcasterName} さんのニコ生開始", $"{liveStatus.Program.Title}",
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

        public void Dispose()
        {
            EndAlert();

            AlertClient.Dispose();
        }
    }
}

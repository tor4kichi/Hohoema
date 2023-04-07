
using Hohoema.Helpers;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Models.Playlist;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Reactive.Subjects;
using Hohoema.Services;
using Microsoft.Toolkit.Uwp.Notifications;
using I18NPortable;
using Hohoema.Models.PageNavigation;
using Hohoema.Models.Application;
using LiteDB;
using Windows.System;
using Microsoft.Toolkit.Uwp;

namespace Hohoema.Models.UseCase.Subscriptions
{
    public sealed class NewFeedVideosProcesser
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly IScheduler _scheduler;
        private readonly IMessenger _messenger;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly SubscriptionSettings _subscriptionSettingsRepository;
        private readonly SubscFeedVideoRepository _subscFeedVideoRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly NotificationService _notificationService;
        List<SubscFeedVideo> Results = new List<SubscFeedVideo>();

        static readonly string OpenSubscriptionManagementPageParam = ToastNotificationConstants.MakeOpenPageToastArguments(HohoemaPageType.SubscriptionManagement).ToString();
        static readonly string PlayWithWatchAfterPlaylistParam = ToastNotificationConstants.MakePlayPlaylistToastArguments(PlaylistItemsSourceOrigin.Local, QueuePlaylist.Id.Id).ToString();


        Dictionary<ObjectId, DateTime> _subscLastUpdateAtMap = new();

        public NewFeedVideosProcesser(
            IScheduler scheduler,
            IMessenger messenger,
            SubscriptionManager subscriptionManager,
            SubscriptionSettings subscriptionSettingsRepository,
            SubscFeedVideoRepository subscFeedVideoRepository,
            NicoVideoProvider nicoVideoProvider,
            QueuePlaylist queuePlaylist,
            NotificationService notificationService
            )
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _scheduler = scheduler;
            _messenger = messenger;
            _subscriptionManager = subscriptionManager;
            _subscriptionSettingsRepository = subscriptionSettingsRepository;
            _subscFeedVideoRepository = subscFeedVideoRepository;
            _nicoVideoProvider = nicoVideoProvider;
            _queuePlaylist = queuePlaylist;
            _notificationService = notificationService;

            _subscLastUpdateAtMap = _subscriptionManager.GetAllSubscriptionSourceEntities().ToDictionary(x => x.Id, x => x.LastUpdateAt);

            _messenger.ObserveMessage<NewSubscFeedVideoMessage>(this)
                .Select(x => x.Value)
                .Do(x => Results.Add(x))
                .Throttle(TimeSpan.FromSeconds(5))                
                .Subscribe(async _ => 
                {
                    var items = Results.ToList();
                    Results.Clear();

                    // 成功した購読ソースのラベルを連結してトーストコンテンツとして表示する
                    var feedItemMapBySubscSource = items.GroupBy(x => x.SourceSubscId).ToDictionary(x => x.Key, x => x.ToList());

                    if (!feedItemMapBySubscSource.Any()) { return; }

                    var subscSourceMap = feedItemMapBySubscSource.ToImmutableDictionary(x => x.Key, x => _subscriptionManager.getSubscriptionSourceEntity(x.Key));

                    foreach (var (subscId, subsc) in subscSourceMap.OrderBy(x => x.Value.SortIndex))
                    {
                        var videos = feedItemMapBySubscSource[subscId];
                        if (_subscLastUpdateAtMap.TryGetValue(subscId, out var lastUpdateAt) is false)
                        {
                            // 初回更新としてスキップ
                            _subscLastUpdateAtMap.Add(subscId, subsc.LastUpdateAt);
                            feedItemMapBySubscSource.Remove(subscId);
                        }
                        else if (lastUpdateAt == DateTime.MinValue)
                        {
                            // 初回更新としてスキップ
                            _subscLastUpdateAtMap[subscId] = subsc.LastUpdateAt;
                            feedItemMapBySubscSource.Remove(subscId);
                        }
                        else
                        {
                            _subscLastUpdateAtMap[subscId] = subsc.LastUpdateAt;
                        }
                    }

                    try
                    {
                        await _dispatcherQueue.EnqueueAsync(() => 
                        {
                            foreach (var feed in feedItemMapBySubscSource.SelectMany(x => x.Value))
                            {
                                if (!_queuePlaylist.Contains(feed.VideoId))
                                {
                                    var video = _nicoVideoProvider.GetCachedVideoInfo(feed.VideoId);
                                    _queuePlaylist.Add(video, GetQueuePlaylistOrigin(subscSourceMap[feed.SourceSubscId]));

                                    Debug.WriteLine("[FeedResultAddToWatchLater] added: " + video.Label);
                                }
                            }
                        });                        
                    }
                    catch
                    {

                    }

                    var subscList = feedItemMapBySubscSource.Select(x => subscSourceMap[x.Key]);

                    if (subscList.Any())
                    {
                        var newVideoOwnersText = string.Join(" - ", subscList.Select(x => x.Label));
                        _notificationService.ShowToast(
                            $"Notification_SuccessAddToWatchLaterWithAddedCount".Translate(subscList.Count()),
                            newVideoOwnersText,
                            Microsoft.Toolkit.Uwp.Notifications.ToastDuration.Long,
                            //luanchContent: PlayWithWatchAfterPlaylistParam,
                            toastButtons: new[] {
                                new ToastButton("WatchVideo".Translate(), PlayWithWatchAfterPlaylistParam),
                                new ToastButton(HohoemaPageType.SubscriptionManagement.Translate(), OpenSubscriptionManagementPageParam)
                            }
                            );
                    }
                });

        }


        static PlaylistId? GetQueuePlaylistOrigin(SubscriptionSourceEntity source)
        {
            return new(source.SourceType switch
            {
                SubscriptionSourceType.Mylist => PlaylistItemsSourceOrigin.Mylist,
                SubscriptionSourceType.User => PlaylistItemsSourceOrigin.UserVideos,
                SubscriptionSourceType.Channel => PlaylistItemsSourceOrigin.ChannelVideos,
                SubscriptionSourceType.Series => PlaylistItemsSourceOrigin.Series,
                SubscriptionSourceType.SearchWithKeyword => PlaylistItemsSourceOrigin.SearchWithKeyword,
                SubscriptionSourceType.SearchWithTag => PlaylistItemsSourceOrigin.SearchWithTag,
                _ => throw new NotSupportedException(),
            }, source.SourceParameter);
        }

        void DoWatchItLater(IReadOnlyCollection<SubscFeedVideo> videos)
        {

        }

        void DoNotification(IReadOnlyCollection<SubscFeedVideo> videos)
        {

        }
    }
}

using I18NPortable;
using Microsoft.Toolkit.Uwp.Helpers;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Helpers;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Helpers;
using NicoPlayerHohoema.UseCase.Playlist;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Subscription
{
    public class SubscriptionManager : BindableBase
    {
        #region Migrate Feed to Subscription


        public static void MigrateFeedGroupToSubscriptionManager(SubscriptionManager instance)
        {
            var localObjectStorageHelper = new LocalObjectStorageHelper();

            if (!localObjectStorageHelper.Read(nameof(MigrateFeedGroupToSubscriptionManager), false))
            {
                Debug.WriteLine("フィードを購読に移行：開始");

                var feedGroups = Database.FeedDb.GetAll();

                var subsc = SubscriptionManager.CreateNewSubscription("旧 新着");

                subsc.Destinations.Add(new SubscriptionDestination(
                    "@view".Translate(),
                    SubscriptionDestinationTarget.LocalPlaylist,
                    HohoemaPlaylist.WatchAfterPlaylistId)
                    );

                foreach (var feedSource in feedGroups.SelectMany(x => x.Sources))
                {
                    SubscriptionSourceType? sourceType = null;
                    switch (feedSource.BookmarkType)
                    {
                        case Database.BookmarkType.User: sourceType = SubscriptionSourceType.User; break;
                        case Database.BookmarkType.Mylist: sourceType = SubscriptionSourceType.Mylist; break;
                        case Database.BookmarkType.SearchWithTag: sourceType = SubscriptionSourceType.TagSearch; break;
                        case Database.BookmarkType.SearchWithKeyword: sourceType = SubscriptionSourceType.KeywordSearch; break;
                    }
                    if (sourceType.HasValue)
                    {
                        subsc.Sources.Add(new SubscriptionSource(feedSource.Label, sourceType.Value, feedSource.Content));
                    }
                }

                if (subsc.Sources.Any())
                {
                    instance._subscriptions.Add(subsc);

                    localObjectStorageHelper.Save(nameof(MigrateFeedGroupToSubscriptionManager), true);
                }

                Debug.WriteLine("フィードを購読に移行：完了!");

            }

        }


        #endregion



       
        public SubscriptionManager(
            IScheduler scheduler,
            Provider.ChannelProvider channelProvider,
            Provider.SearchProvider searchProvider,
            Provider.UserProvider userProvider,
            Provider.MylistProvider mylistProvider,
            Services.NotificationService notificationService
            
            )
        {
            _scheduler = scheduler;
            ChannelProvider = channelProvider;
            SearchProvider = searchProvider;
            UserProvider = userProvider;
            MylistProvider = mylistProvider;
            NotificationService = notificationService;

            var storedSubscriptions = Database.Local.Subscription.SubscriptionDb.GetOrderedSubscriptions();
            _subscriptions = new ObservableCollection<Subscription>(storedSubscriptions);
            Subscriptions = new ReadOnlyObservableCollection<Subscription>(_subscriptions);

            Subscriptions.CollectionChangedAsObservable()
                .Subscribe(arg =>
                {
                    switch (arg.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            foreach (var item in arg.NewItems.Cast<Subscription>())
                            {
                                AddOrUpdateToSubscriptionDatabase(item);

                                SubscribeSubscriptionChanged(item);

                                item.IsDeleted = false;
                            }
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                            // TODO: 購読グループの優先度変更に対応する
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            foreach (var item in arg.OldItems.Cast<Subscription>())
                            {
                                RemoveFromSubscriptionDatabase(item);

                                UnsubscribeSubscriptionChanged(item);

                                item.IsDeleted = true;
                            }
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                            break;
                        default:
                            break;
                    }
                });

            foreach (var firstItem in storedSubscriptions)
            {
                SubscribeSubscriptionChanged(firstItem);
            }

            
        }


        Helpers.AsyncLock _UpdateLock = new Helpers.AsyncLock();

        private readonly ObservableCollection<Subscription> _subscriptions;
        public ReadOnlyObservableCollection<Subscription> Subscriptions { get; }
        private readonly IScheduler _scheduler;
        public Provider.ChannelProvider ChannelProvider { get; }
        public Provider.SearchProvider SearchProvider { get; }
        public Provider.UserProvider UserProvider { get; }
        public Provider.MylistProvider MylistProvider { get; }
        public Services.NotificationService NotificationService { get; }


        static public Subscription CreateNewSubscription(string label)
        {
            return new Subscription(Guid.NewGuid(), label);
        }


        public Subscription CreateSusbcription(string label)
        {
            var newSubsc = SubscriptionManager.CreateNewSubscription(label);
           
            newSubsc.Destinations.Add(new SubscriptionDestination("@view".Translate(), SubscriptionDestinationTarget.LocalPlaylist, HohoemaPlaylist.WatchAfterPlaylistId));
            _subscriptions.Insert(0, newSubsc);

            return newSubsc;
        }




        #region Commands


        private DelegateCommand<string> _AddSubscription;
        public DelegateCommand<string> AddSubscription
        {
            get
            {
                return _AddSubscription
                    ?? (_AddSubscription = new DelegateCommand<string>((subscriptionLabel) =>
                    {
                        if (subscriptionLabel == null) { return; }

                        var newSubscription = SubscriptionManager.CreateNewSubscription(subscriptionLabel);

                        // "あとで見る"の多言語対応
                        newSubscription.Destinations.Add(new SubscriptionDestination("@view".Translate(), SubscriptionDestinationTarget.LocalPlaylist, HohoemaPlaylist.WatchAfterPlaylistId));
                        _subscriptions.Insert(0, newSubscription);

                        // TODO: ソースアイテムの選択ダイアログ？を表示する

                        // Note: 直前に見ていたユーザー情報ページやマイリストなどを選択できるようにしたい
                    },
                    (subscriptionLabel) =>
                    {
                        return !string.IsNullOrWhiteSpace(subscriptionLabel);
                    }
                    ));
            }
        }


        private DelegateCommand<Subscription> _RemoveSubscription;
        public DelegateCommand<Subscription> RemoveSubscription
        {
            get
            {
                return _RemoveSubscription
                    ?? (_RemoveSubscription = new DelegateCommand<Subscription>((subscription) =>
                    {
                        _subscriptions.Remove(subscription);
                    }
                    , (subscription) => Subscriptions.Contains(subscription)
                    ));
            }
        }



        #endregion




        #region Handle subscription items property changed

        Dictionary<Guid, IDisposable> _SubscriptionSubscribeDiposerMap = new Dictionary<Guid, IDisposable>();

        private void SubscribeSubscriptionChanged(Subscription subscription)
        {
            CompositeDisposable disposables = new CompositeDisposable();
            new[]
            {
                subscription.Sources.ToCollectionChanged().ToUnit(),
                subscription.Destinations.ToCollectionChanged().ToUnit(),
                subscription.ObserveProperty(x => x.Label).ToUnit(),
                subscription.ObserveProperty(x => x.IsEnabled).ToUnit(),
                subscription.ObserveProperty(x => x.DoNotNoticeKeyword).ToUnit(),
                subscription.ObserveProperty(x => x.DoNotNoticeKeywordAsRegex).ToUnit(),
            }
            .Merge()
            .Throttle(TimeSpan.FromSeconds(1))
            .Skip(1)
            .Subscribe(_ => AddOrUpdateToSubscriptionDatabase(subscription))
            .AddTo(disposables)
            ;

            // Resolve SubscriptionSource Label if not valid.
            subscription.Sources.ObserveAddChanged()
                .DelaySubscription(TimeSpan.FromSeconds(0.1))
                .Subscribe(async item =>
                {
                    // ラベルが空の場合は名前を解決
                    // マイリストの場合は保有者名をOptionalLabelとして取得するよう追加判定
                    if (string.IsNullOrEmpty(item.Label) || (item.SourceType == SubscriptionSourceType.Mylist && string.IsNullOrEmpty(item.OptionalLabel)))
                    {
                        try
                        {
                            Debug.WriteLine($"購読ソース: {item.Parameter}({item.SourceType}) のラベルが無いのでオンラインから取得します。");
                            var tuple = await ResolveSubscriptionSourceLabel(item);

                            
                            if (tuple != null)
                            {
                                var label = tuple.Item1;
                                var optionLabel = tuple.Item2;

                                subscription.Sources.Remove(item);
                                subscription.Sources.Add(new SubscriptionSource(label, item.SourceType, item.Parameter, optionLabel));

                                Debug.WriteLine($"購読ソース: {item.Parameter}({item.SourceType}) -> {label}/{optionLabel}");

                                // ラベル再取得後の動作が並べ替え時に追加通知を抑制する動作と競合することへの対応
                                _prevRemovedSourceHashId = 0;
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        catch
                        {
                            Debug.WriteLine($"購読ソース: {item.Parameter}({item.SourceType}) のラベル解決に失敗");
                        }
                    }



                })
                .AddTo(disposables)
                ;


            // 追加・削除時の通知
            subscription.Sources.CollectionChangedAsObservable()
                .Do(e =>
                {
                    switch (e.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            _prevRemovedSourceHashId = ((SubscriptionSource)e.OldItems[0]).GetHashCode();
                            break;
                    }
                })
                .Throttle(TimeSpan.FromSeconds(0.5))
                .Subscribe(e =>
                {
                    var action = e.Action;
                    switch (e.Action)
                    {
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                            var newItem = (SubscriptionSource)e.NewItems[0];
                            if (newItem.GetHashCode() != _prevRemovedSourceHashId)
                            {
                                // Add
                                NotificationService.ShowInAppNotification(
                                    Services.InAppNotificationPayload.CreateReadOnlyNotification(
                                        content: "InAppNotification_AddItem1_ToSubsc0".Translate(subscription.Label, $"{newItem.Label}({newItem.SourceType.Translate()})"),
                                        showDuration: TimeSpan.FromSeconds(3)
                                        ));
                            }
                            else
                            {
                                // Move
                                // Do nothing.
                            }
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                            var oldItem = (SubscriptionSource)e.OldItems[0];
                            NotificationService.ShowInAppNotification(
                                    Services.InAppNotificationPayload.CreateReadOnlyNotification(
                                    content: "InAppNotification_RemoveItem1_FromSubsc0".Translate(subscription.Label, $"{oldItem.Label}({oldItem.SourceType.Translate()})")
                                    ));
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                            break;
                        case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                            break;
                        default:
                            break;
                    }


                })
                .AddTo(disposables)
                ;

            _SubscriptionSubscribeDiposerMap.Add(subscription.Id, disposables);
        }

        int _prevRemovedSourceHashId;

        private async Task<Tuple<string, string>> ResolveSubscriptionSourceLabel(SubscriptionSource source)
        {
            switch (source.SourceType)
            {
                case SubscriptionSourceType.User:
                    var info = await UserProvider.GetUser(source.Parameter);
                    return new Tuple<string, string>(info.ScreenName, null);
                case SubscriptionSourceType.Channel:
                    var channelInfo = await ChannelProvider.GetChannelInfo(source.Parameter);
                    return new Tuple<string, string>(channelInfo.Name, null);
                case SubscriptionSourceType.Mylist:
                    var mylistInfo = await MylistProvider.GetMylistGroupDetail(source.Parameter);
                    var mylistOwner = await UserProvider.GetUser(mylistInfo.Owner.Id);

                    return new Tuple<string, string>(mylistInfo.Name, mylistOwner.ScreenName);
                case SubscriptionSourceType.TagSearch:
                    return new Tuple<string, string>(source.Parameter, null);
                case SubscriptionSourceType.KeywordSearch:
                    return new Tuple<string, string>(source.Parameter, null);
                default:
                    break;
            }

            return null;
        }


        private void UnsubscribeSubscriptionChanged(Subscription subscription)
        {
            if (_SubscriptionSubscribeDiposerMap.TryGetValue(subscription.Id, out var disposer))
            {
                disposer.Dispose();
                _SubscriptionSubscribeDiposerMap.Remove(subscription.Id);
            }
        }

        #endregion




        #region Subscription database access

        private void AddOrUpdateToSubscriptionDatabase(Subscription subscription)
        {
            var order = Subscriptions.IndexOf(subscription);
            Database.Local.Subscription.SubscriptionDb.AddOrUpdateSubscription(subscription, order);

            foreach (var subsc in Subscriptions.Where(x => x.Id != subscription.Id))
            {
                order = Subscriptions.IndexOf(subsc);
                Database.Local.Subscription.SubscriptionDb.AddOrUpdateSubscription(subsc, order);

            }
#if DEBUG
            Debug.WriteLine($"購読 {subscription.Label} をローカルDBに保存 ({DateTime.Now})");
#endif
        }

        private bool RemoveFromSubscriptionDatabase(Subscription subscription)
        {
#if DEBUG
            Debug.WriteLine($"購読 {subscription.Label} をローカルDBから削除 ({DateTime.Now})");
#endif

            return Database.Local.Subscription.SubscriptionDb.RemoveSubscription(subscription);
        }

        #endregion




        #region Feed Result

        static readonly Action<Subscription, int> SetSubscriptionStatusToPendingOnUI = (subsc, updateTargetCount) =>
        {
            if (SubscriptionUpdateStatus.UpdatePending == subsc.Status) { return; }

            subsc.UpdateTargetCount = updateTargetCount;
            subsc.UpdateCompletedCount = 0;
            subsc.Status = updateTargetCount > 0 ? SubscriptionUpdateStatus.UpdatePending : SubscriptionUpdateStatus.Complete;

            Debug.WriteLine($"{subsc.Label} : {subsc.Status}");
        };
        static readonly Action<Subscription> SetSubscriptionStatusToUpdatingOnUI = (subsc) =>
        {
            if (SubscriptionUpdateStatus.NowUpdating == subsc.Status) { return; }

            subsc.Status = SubscriptionUpdateStatus.NowUpdating;

            Debug.WriteLine($"{subsc.Label} : {subsc.Status}");
        };

        static readonly Action<Subscription> SetSubscriptionUpdateCompletedOnUI = async (subsc) =>
        {
            if (SubscriptionUpdateStatus.Complete == subsc.Status) { return; }

            subsc.UpdateCompletedCount++;

            if (subsc.UpdateCompletedCount >= subsc.UpdateTargetCount)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                subsc.Status = SubscriptionUpdateStatus.Complete;
            }

            Debug.WriteLine($"{subsc.Label} : {subsc.Status}");
        };

        Helpers.AsyncLock _FeedResultLock = new Helpers.AsyncLock();

        public IObservable<SubscriptionUpdateInfo> GetSubscriptionFeedResultAsObservable(bool withoutDisabled = false)
        {
            var targetSubscriptions = withoutDisabled
                ? Subscriptions.Where(x => x.IsEnabled)
                : Subscriptions;

            foreach (var subsc in targetSubscriptions)
            {
                SetSubscriptionStatusToPendingOnUI(subsc, subsc.Sources.Count);
            }

            return Observable.Concat(targetSubscriptions.Select(x => GetSubscriptionFeedResultAsObservable(x)));
        }

        public IObservable<SubscriptionUpdateInfo> GetSubscriptionFeedResultAsObservable(Subscription subscription)
        {
            return GetSubscriptionFeedResultAsObservable(subscription, subscription.Sources);
        }

        public IObservable<SubscriptionUpdateInfo> GetSubscriptionFeedResultAsObservable(Subscription subscription, IEnumerable<SubscriptionSource> sources)
        {
            if (!Helpers.InternetConnection.IsInternet())
            {
                subscription.Status = SubscriptionUpdateStatus.Complete;
                return Observable.Empty<SubscriptionUpdateInfo>();
            }

            var subscriptionAndSourceSets = subscription.Sources
                .Where(x => sources.Any(y => x.SourceType == y.SourceType && x.Parameter == y.Parameter))
                .Select(source => new { subscription, source })
                .ToArray();

            SetSubscriptionStatusToPendingOnUI(subscription, subscriptionAndSourceSets.Length);

            var count = 0;
            return Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3))
                    .Select(_ => count++)
                    .Take(subscriptionAndSourceSets.Length)
                    .Select(x => subscriptionAndSourceSets[x])
                    .SelectMany(async x =>
                    {
                        using (var releaser = await _FeedResultLock.LockAsync())
                        {
                            _scheduler.Schedule(() => SetSubscriptionStatusToUpdatingOnUI(x.subscription));

                            var info = await GetFeedResult(x.subscription, x.source);
                            AddOrUpdateFeedResult(ref info);

                            _scheduler.Schedule(() => SetSubscriptionUpdateCompletedOnUI(subscription));
                            return info;
                        }
                    });
        }

        private async Task<SubscriptionUpdateInfo> GetFeedResult(Subscription subscription, SubscriptionSource source)
        {
            if (subscription.IsDeleted) { return new SubscriptionUpdateInfo() { Subscription = subscription, Source = source }; }

            TimeSpan timeDiffarenceFromJapan = +TimeSpan.FromHours(9) - DateTimeOffset.Now.Offset;

            var feedResult = Database.Local.Subscription.SubscriptionFeedResultDb.GetEnsureFeedResult(subscription);
            var feedResultSet = feedResult.GetFeedResultSet(source);
            var lastUpdated = feedResultSet != null ? feedResultSet.LastUpdated + timeDiffarenceFromJapan : DateTime.MinValue;
            var isFirstUpdate = feedResultSet == null;
            List<IVideoContent> items = null;
            switch (source.SourceType)
            {
                case SubscriptionSourceType.User:
                    items = await GetUserVideosFeedResult(source.Parameter, UserProvider);
                    break;
                case SubscriptionSourceType.Channel:
                    items = await GetChannelVideosFeedResult(source.Parameter, ChannelProvider);
                    break;
                case SubscriptionSourceType.Mylist:
                    items = await GetMylistFeedResult(source.Parameter, MylistProvider);
                    break;
                case SubscriptionSourceType.TagSearch:
                    items = await GetTagSearchFeedResult(source.Parameter, SearchProvider);
                    break;
                case SubscriptionSourceType.KeywordSearch:
                    items = await GetKeywordSearchFeedResult(source.Parameter, SearchProvider);
                    break;
                default:
                    break;
            }

            // 降順（新しい動画を先に）にしてから、前回更新時までのアイテムを取得する
            var newItems = items.OrderByDescending(x => x.PostedAt).TakeWhile(x => x.PostedAt > lastUpdated);

            Database.Local.Subscription.SubscriptionFeedResultDb.AddOrUpdateFeedResult(subscription, source, newItems.Select(x => x.Id));


            return new SubscriptionUpdateInfo()
            {
                Subscription = subscription,
                Source = source,
                FeedItems = items,
                NewFeedItems = newItems,
                IsFirstUpdate = isFirstUpdate,
            };
            
        }


        // 取得数が40になるか、lastUpdatedよりも古いアイテムが見つかるまでデータ取得する

       
        static private async Task<List<IVideoContent>> GetUserVideosFeedResult(string userId, Provider.UserProvider userProvider)
        {
            var id = uint.Parse(userId);
            List<IVideoContent> items = new List<IVideoContent>();
            uint page = 1;
            
            var res = await userProvider.GetUserVideos(id, page);

            var videoItems = res.Data.Items;
            var currentItemsCount = videoItems?.Count ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {

            }
            else
            {
                foreach (var item in videoItems)
                {
                    var video = Database.NicoVideoDb.Get(item.Id);

                    video.Title = item.Title;
                    video.PostedAt = item.RegisteredAt.DateTime;

                    items.Add(video);
                }
            }
            

            return items;
        }

        static private async Task<List<IVideoContent>> GetChannelVideosFeedResult(string channelId, Provider.ChannelProvider channelProvider)
        {
            List<IVideoContent> items = new List<IVideoContent>();
            int page = 0;
            var res = await channelProvider.GetChannelVideo(channelId, page);

            var videoItems = res.Videos;
            var currentItemsCount = videoItems?.Count ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {
                
            }
            else
            {
                foreach (var item in videoItems)
                {
                    var video = Database.NicoVideoDb.Get(item.ItemId);

                    video.Title = item.Title;
                    video.PostedAt = item.PostedAt;

                    items.Add(video);
                }
            }

            return items;
        }

        static private async Task<List<IVideoContent>> GetMylistFeedResult(string mylistId, Provider.MylistProvider mylistProvider)
        {
            List<IVideoContent> items = new List<IVideoContent>();
            uint page = 0;
            const uint itemGetCountPerPage = 50;
            var result = await mylistProvider.GetMylistGroupVideo(mylistId, Mntone.Nico2.Users.Mylist.MylistSortKey.AddedAt, Mntone.Nico2.Users.Mylist.MylistSortOrder.Desc, itemGetCountPerPage, page);

            var videoItems = result.Items;
            var currentItemsCount = videoItems?.Count ?? 0;
            if (result.IsSuccess)
            {
                items.AddRange(videoItems);
            }

            return items;
        }

        static private async Task<List<IVideoContent>> GetKeywordSearchFeedResult(string keyword, Provider.SearchProvider searchProvider)
        {
            List<IVideoContent> items = new List<IVideoContent>();
            int page = 0;
            const int itemGetCountPerPage = 50;
            
            var head = page * itemGetCountPerPage;
            var res = await searchProvider.GetKeywordSearch(keyword, (uint)head, itemGetCountPerPage);

            var videoItems = res.VideoInfoItems;
            var currentItemsCount = videoItems?.Count ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {
                
            }
            else
            {
                foreach (var item in videoItems)
                {
                    var video = Database.NicoVideoDb.Get(item.Video.Id);

                    video.Title = item.Video.Title;
                    video.PostedAt = item.Video.FirstRetrieve;

                    items.Add(video);
                }
            }
            

            return items;
        }

        static private async Task<List<IVideoContent>> GetTagSearchFeedResult(string tag, Provider.SearchProvider searchProvider)
        {
            List<IVideoContent> items = new List<IVideoContent>();
            int page = 0;
            const int itemGetCountPerPage = 50;

            var head = page * itemGetCountPerPage;
            var res = await searchProvider.GetTagSearch(tag, (uint)head, itemGetCountPerPage);

            var videoItems = res.VideoInfoItems;
            var currentItemsCount = videoItems?.Count ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {

            }
            else
            {
                foreach (var item in videoItems)
                {
                    var video = Database.NicoVideoDb.Get(item.Video.Id);

                    video.Title = item.Video.Title;
                    video.PostedAt = item.Video.FirstRetrieve;

                    items.Add(video);
                }
            }


            return items;
        }

        #endregion




        #region FeedResult database access

        private void AddOrUpdateFeedResult(ref SubscriptionUpdateInfo info)
        {
            if (info.Subscription.IsDeleted) { return; }


#if DEBUG
            Debug.WriteLine($"{info.Subscription.Label} のフィード結果をテンポラリDBに保存 ({DateTime.Now})");
#endif
        }


        #endregion
    }

}

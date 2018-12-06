using NicoPlayerHohoema.Helpers;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Subscription
{
    public class SubscriptionManager : BindableBase
    {
        #region Singleton Pattern

        public static SubscriptionManager Instance { get; }



        public bool IsInitialized { get; private set; } = false;

        public static void Initialize(Models.NiconicoContentProvider contentProvider)
        {
            Instance.SetContentProvider(contentProvider);

            Instance.IsInitialized = true;
        }


        void ThrowExceptionIfNotInitialized()
        {
            if (!IsInitialized) { throw new Exception("SubscriptionManager is not initialized. "); }
        }

        static SubscriptionManager()
        {
            var storedSubscriptions = Database.Local.Subscription.SubscriptionDb.GetOrderedSubscriptions();
            Instance = new SubscriptionManager(storedSubscriptions);
        }



        #endregion


        private void SetContentProvider(Models.NiconicoContentProvider contentProvider)
        {
            _ContentProvider = contentProvider;
        }

        Models.NiconicoContentProvider _ContentProvider;
        AsyncLock _UpdateLock = new AsyncLock();

        public ObservableCollection<Subscription> Subscriptions { get; }

        SubscriptionManager(IEnumerable<Subscription> storedSubscriptions)
        {
            Subscriptions = new ObservableCollection<Subscription>(storedSubscriptions);

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

        public static Subscription CreateNewSubscription(string label)
        {
            return new Subscription(Guid.NewGuid(), label);
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

                        // TODO: "あとで見る"の多言語対応
                        newSubscription.Destinations.Add(new SubscriptionDestination("あとで見る", SubscriptionDestinationTarget.LocalPlaylist, HohoemaPlaylist.WatchAfterPlaylistId));
                        SubscriptionManager.Instance.Subscriptions.Insert(0, newSubscription);

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

            subscription.Sources.ObserveAddChanged()
                .DelaySubscription(TimeSpan.FromSeconds(2))
                .Subscribe(async item => 
                {
                    if (string.IsNullOrEmpty(item.Label))
                    {
                        try
                        {
                            Debug.WriteLine($"購読ソース: {item.Parameter}({item.SourceType}) のラベルが無いのでオンラインから取得します。");
                            var label = await SubscriptionManager.ResolveSubscriptionSourceLabel(item, _ContentProvider);

                            if (!string.IsNullOrEmpty(label))
                            {
                                subscription.Sources.Remove(item);
                                subscription.Sources.Add(new SubscriptionSource(label, item.SourceType, item.Parameter));

                                Debug.WriteLine($"購読ソース: {item.Parameter}({item.SourceType}) -> {label}");
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

            _SubscriptionSubscribeDiposerMap.Add(subscription.Id, disposables);
        }


        private static async Task<string> ResolveSubscriptionSourceLabel(SubscriptionSource source, NiconicoContentProvider contentProvider)
        {
            switch (source.SourceType)
            {
                case SubscriptionSourceType.User:
                    var info = await contentProvider.GetUserInfo(source.Parameter);
                    return info.Nickname;
                case SubscriptionSourceType.Channel:
                    var channelInfo = await contentProvider.GetChannelInfo(source.Parameter);
                    return channelInfo.Name;
                case SubscriptionSourceType.Mylist:
                    var mylistInfo = await contentProvider.GetMylistGroupDetail(source.Parameter);
                    return mylistInfo.MylistGroup.Name;
                case SubscriptionSourceType.TagSearch:
                    return source.Parameter;
                case SubscriptionSourceType.KeywordSearch:
                    return source.Parameter;
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


        AsyncLock _FeedResultLock = new AsyncLock();

        public IObservable<SubscriptionUpdateInfo> GetSubscriptionFeedResultAsObservable(bool withoutDisabled = false)
        {
            if (withoutDisabled)
            {
                return Observable.Concat(Subscriptions.Where(x => x.IsEnabled).Select(x => GetSubscriptionFeedResultAsObservable(x)));
            }
            else
            {
                return Observable.Concat(Subscriptions.Select(x => GetSubscriptionFeedResultAsObservable(x)));
            }
        }

        public IObservable<SubscriptionUpdateInfo> GetSubscriptionFeedResultAsObservable(Subscription subscription)
        {
            return GetSubscriptionFeedResultAsObservable(subscription, subscription.Sources);
        }

        public IObservable<SubscriptionUpdateInfo> GetSubscriptionFeedResultAsObservable(Subscription subscription, IEnumerable<SubscriptionSource> sources)
        {
            ThrowExceptionIfNotInitialized();

            var subscriptionAndSourceSets = subscription.Sources
                .Where(x => sources.Any(y => x.SourceType == y.SourceType && x.Parameter == y.Parameter))
                .Select(source => new { subscription, source });

            return subscriptionAndSourceSets.ToObservable()
                    .SelectMany(async x =>
                    {
                        using (var releaser = await _FeedResultLock.LockAsync())
                        {
                            await Task.Delay(TimeSpan.FromSeconds(3));

                            return await GetFeedResult(x.subscription, x.source);
                        }
                    })
                    .Do(info => AddOrUpdateFeedResult(ref info))
                ;
        }

        private async Task<SubscriptionUpdateInfo> GetFeedResult(Subscription subscription, SubscriptionSource source)
        {
            if (subscription.IsDeleted) { return new SubscriptionUpdateInfo() { Subscription = subscription, Source = source }; }

            TimeSpan timeDiffarenceFromJapan = +TimeSpan.FromHours(9) - DateTimeOffset.Now.Offset;

            var feedResult = Database.Local.Subscription.SubscriptionFeedResultDb.GetEnsureFeedResult(subscription);
            var feedResultSet = feedResult.GetFeedResultSet(source);
            var lastUpdated = feedResultSet != null ? feedResultSet.LastUpdated + timeDiffarenceFromJapan : DateTime.MinValue;
            var isFirstUpdate = feedResultSet == null;
            List<Database.NicoVideo> items = null;
            switch (source.SourceType)
            {
                case SubscriptionSourceType.User:
                    items = await GetUserVideosFeedResult(source.Parameter);
                    break;
                case SubscriptionSourceType.Channel:
                    items = await GetChannelVideosFeedResult(source.Parameter);
                    break;
                case SubscriptionSourceType.Mylist:
                    items = await GetMylistFeedResult(source.Parameter);
                    break;
                case SubscriptionSourceType.TagSearch:
                    items = await GetTagSearchFeedResult(source.Parameter);
                    break;
                case SubscriptionSourceType.KeywordSearch:
                    items = await GetKeywordSearchFeedResult(source.Parameter);
                    break;
                default:
                    break;
            }

            // 降順（新しい動画を先に）にしてから、前回更新時までのアイテムを取得する
            var newItems = items.OrderByDescending(x => x.PostedAt).TakeWhile(x => x.PostedAt > lastUpdated);

            Database.Local.Subscription.SubscriptionFeedResultDb.AddOrUpdateFeedResult(subscription, source, newItems.Select(x => x.VideoId));


            return new SubscriptionUpdateInfo()
            {
                Subscription = subscription,
                Source = source,
                FeedItems = items,
                NewFeedItems = newItems,
                IsFirstUpdate = isFirstUpdate
            };
            
        }


        // 取得数が40になるか、lastUpdatedよりも古いアイテムが見つかるまでデータ取得する

       
        private async Task<List<Database.NicoVideo>> GetUserVideosFeedResult(string userId)
        {
            var id = uint.Parse(userId);
            List<Database.NicoVideo> items = new List<Database.NicoVideo>();
            uint page = 0;
            
            var res = await _ContentProvider.GetUserVideos(id, page);

            var videoItems = res.Items;
            var currentItemsCount = videoItems?.Count ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {

            }
            else
            {
                foreach (var item in videoItems)
                {
                    var video = Database.NicoVideoDb.Get(item.VideoId);

                    video.Title = item.Title;
                    video.PostedAt = item.SubmitTime;

                    items.Add(video);
                }
            }
            

            return items;
        }

        private async Task<List<Database.NicoVideo>> GetChannelVideosFeedResult(string channelId)
        {
            List<Database.NicoVideo> items = new List<Database.NicoVideo>();
            int page = 0;
            var res = await _ContentProvider.GetChannelVideo(channelId, page);

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

        private async Task<List<Database.NicoVideo>> GetMylistFeedResult(string mylistId)
        {
            List<Database.NicoVideo> items = new List<Database.NicoVideo>();
            int page = 0;
            const int itemGetCountPerPage = 50;
            var head = page * itemGetCountPerPage;
            var tail = head + itemGetCountPerPage;
            var res = await _ContentProvider.GetMylistGroupVideo(mylistId, (uint)head, (uint)itemGetCountPerPage);

            var videoItems = res.MylistVideoInfoItems;
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

        private async Task<List<Database.NicoVideo>> GetKeywordSearchFeedResult(string keyword)
        {
            List<Database.NicoVideo> items = new List<Database.NicoVideo>();
            int page = 0;
            const int itemGetCountPerPage = 50;
            
            var head = page * itemGetCountPerPage;
            var res = await _ContentProvider.GetKeywordSearch(keyword, (uint)head, itemGetCountPerPage);

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

        private async Task<List<Database.NicoVideo>> GetTagSearchFeedResult(string tag)
        {
            List<Database.NicoVideo> items = new List<Database.NicoVideo>();
            int page = 0;
            const int itemGetCountPerPage = 50;

            var head = page * itemGetCountPerPage;
            var res = await _ContentProvider.GetTagSearch(tag, (uint)head, itemGetCountPerPage);

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

using NicoPlayerHohoema.Helpers;
using Prism.Commands;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NicoPlayerHohoema.Models.Subscription
{

    public interface ISubscriptionFeedResult : Interfaces.INiconicoContent
    {

    }


    public enum SubscriptionSourceType
    {
        User,
        Channel,
        Mylist,
        TagSearch,
        KeywordSearch,
    }


    public enum SubscriptionUpdateStatus
    {
        Ready,
        PendingUpdate,
        NowUpdating,
    }

    public struct SubscriptionSource 
    {
        public string Label { get; set; }
        public SubscriptionSourceType SourceType { get; }
        public string Parameter { get; }

        public SubscriptionSource(string label, SubscriptionSourceType sourceType, string parameter)
        {
            _HashCode = null;

            Label = label;
            SourceType = sourceType;
            Parameter = parameter;

        }

        public override bool Equals(object obj)
        {
            if (obj is SubscriptionSource other)
            {
                return this.SourceType == other.SourceType
                    && this.Parameter == other.Parameter;
            }

            return base.Equals(obj);
        }

        int? _HashCode;
        public override int GetHashCode()
        {
            return _HashCode ?? (_HashCode = (Parameter + SourceType.ToString()).GetHashCode()).Value;
        }
    }

    public enum SubscriptionDestinationTarget
    {
        LoginUserMylist,
        LocalPlaylist,
    }

    public struct SubscriptionDestination
    {
        public string Label { get; }
        public string PlaylistId { get; }
        public SubscriptionDestinationTarget Target { get; }

        public SubscriptionDestination(string label, SubscriptionDestinationTarget target, string playlistId)
        {
            PlaylistId = playlistId;
            Target = target;
            Label = label;
        }

    }

    public sealed class Subscription : BindableBase, IDisposable
    {
        public Guid Id { get; }

        private string _Label;
        public string Label
        {
            get { return _Label; }
            set { SetProperty(ref _Label, value); }
        }


        private bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { SetProperty(ref _IsEnabled, value); }
        }


        public ObservableCollection<SubscriptionSource> Sources { get; } = new ObservableCollection<SubscriptionSource>();

        public ObservableCollection<SubscriptionDestination> Destinations { get; } = new ObservableCollection<SubscriptionDestination>();


        private string _DoNotNoticeKeyword = string.Empty;
        public string DoNotNoticeKeyword
        {
            get { return _DoNotNoticeKeyword; }
            set { SetProperty(ref _DoNotNoticeKeyword, value); }
        }



        private bool _DoNotNoticeKeywordAsRegex = false;
        public bool DoNotNoticeKeywordAsRegex
        {
            get { return _DoNotNoticeKeywordAsRegex; }
            set { SetProperty(ref _DoNotNoticeKeywordAsRegex, value); }
        }

        private Regex _doNotNoticeKeywordRegex;
        private Regex DoNotNoticeeKeywordRegex
        {
            get
            {
                return _doNotNoticeKeywordRegex
                    ?? (_doNotNoticeKeywordRegex = new Regex(DoNotNoticeKeyword));
            }
        }

        private DelegateCommand<string> _UpdateDoNotNoticeKeyword;
        public DelegateCommand<string> UpdateDoNotNoticeKeyword
        {
            get
            {
                return _UpdateDoNotNoticeKeyword
                    ?? (_UpdateDoNotNoticeKeyword = new DelegateCommand<string>(doNotNoticeKeyword =>
                    {
                        DoNotNoticeKeyword = doNotNoticeKeyword;
                    },
                    doNotNoticeKeyword =>
                    {
                        if (DoNotNoticeKeywordAsRegex)
                        {
                            if (string.IsNullOrWhiteSpace(doNotNoticeKeyword))
                            {
                                return false;
                            }

                            try
                            {
                                return DoNotNoticeeKeywordRegex != null;
                            }
                            catch
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return !string.IsNullOrWhiteSpace(doNotNoticeKeyword);
                        }
                    }
                    ));
            }
        }



        public bool IsContainDoNotNoticeKeyword(string title)
        {
            if (string.IsNullOrWhiteSpace(DoNotNoticeKeyword)) { return false; }

            if (DoNotNoticeKeywordAsRegex)
            {
                return DoNotNoticeeKeywordRegex.IsMatch(title);
            }
            else
            {
                return title.Contains(DoNotNoticeKeyword);
            }
        }

        


        private SubscriptionUpdateStatus _Status = SubscriptionUpdateStatus.Ready;
        public SubscriptionUpdateStatus Status
        {
            get { return _Status; }
            internal set { SetProperty(ref _Status, value); }
        }


        public bool IsDeleted { get; internal set; } = false;


        CompositeDisposable _disposables = new CompositeDisposable();

        // instantiate from only on SubscriptionManager.
        internal Subscription(Guid id, string label)
        {
            Id = id;
            Label = label;

            new[] {
                this.ObserveProperty(x => x.DoNotNoticeKeywordAsRegex).ToUnit(),
                this.ObserveProperty(x => x.DoNotNoticeKeyword).ToUnit(),
            }
            .Merge()
            .Subscribe(x => 
            {
                _doNotNoticeKeywordRegex = null;
                UpdateDoNotNoticeKeyword.RaiseCanExecuteChanged();
            });
            
        }

        public void Dispose()
        {
            _disposables?.Dispose();
        }


        private DelegateCommand<string> _Rename;
        public DelegateCommand<string> Rename
        {
            get
            {
                return _Rename
                    ?? (_Rename = new DelegateCommand<string>(rename => 
                    {
                        Label = rename;
                    }, 
                    rename => 
                    {
                        return !string.IsNullOrWhiteSpace(rename);
                    }
                    ));
            }
        }


        private DelegateCommand _Remove;
        public DelegateCommand Remove
        {
            get
            {
                return _Remove
                    ?? (_Remove = new DelegateCommand(() =>
                    {
                        SubscriptionManager.Instance.Subscriptions.Remove(this);
                    },
                    () =>
                    {
                        return !this.IsDeleted;
                    }
                    ));
            }
        }


        private DelegateCommand<SubscriptionSource?> _RemoveSource;
        public DelegateCommand<SubscriptionSource?> RemoveSource
        {
            get
            {
                return _RemoveSource
                    ?? (_RemoveSource = new DelegateCommand<SubscriptionSource?>((source) =>
                    {
                        this.Sources.Remove(source.Value);
                    },
                    (source) =>
                    {
                        return source != null;
                    }
                    ));
            }
        }

    }


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
            var disposer = new[] 
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
            ;

            _SubscriptionSubscribeDiposerMap.Add(subscription.Id, disposer);
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
                    items = await GetUserVideosFeedResult(source.Parameter, lastUpdated);
                    break;
                case SubscriptionSourceType.Channel:
                    items = await GetChannelVideosFeedResult(source.Parameter, lastUpdated);
                    break;
                case SubscriptionSourceType.Mylist:
                    items = await GetMylistFeedResult(source.Parameter, lastUpdated);
                    break;
                case SubscriptionSourceType.TagSearch:
                    items = await GetTagSearchFeedResult(source.Parameter, lastUpdated);
                    break;
                case SubscriptionSourceType.KeywordSearch:
                    items = await GetKeywordSearchFeedResult(source.Parameter, lastUpdated);
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

       
        private async Task<List<Database.NicoVideo>> GetUserVideosFeedResult(string userId, DateTimeOffset lastUpdated)
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

        private async Task<List<Database.NicoVideo>> GetChannelVideosFeedResult(string channelId, DateTimeOffset lastUpdated)
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

        private async Task<List<Database.NicoVideo>> GetMylistFeedResult(string mylistId, DateTimeOffset lastUpdated)
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

        private async Task<List<Database.NicoVideo>> GetKeywordSearchFeedResult(string keyword, DateTimeOffset lastUpdated)
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

        private async Task<List<Database.NicoVideo>> GetTagSearchFeedResult(string tag, DateTimeOffset lastUpdated)
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

    public struct SubscriptionUpdateInfo
    {
        public Subscription Subscription { get; set; }
        public SubscriptionSource Source { get; set; }
        public IEnumerable<Database.NicoVideo> FeedItems { get; set; }
        public IEnumerable<Database.NicoVideo> NewFeedItems { get; set; }

        public bool IsUpdateComplete => FeedItems != null;

        public bool IsFirstUpdate { get; set; }

    }

}

using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.Repository.NicoVideo;
using NicoPlayerHohoema.Repository.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Models.Subscriptions
{
    public class SubscriptionFeedUpdateResult
    {
        public bool IsSuccessed { get; set; }
        public SubscriptionSourceEntity Entity { get; set; }
        public List<NicoVideo> Videos { get; set; }
        public List<NicoVideo> NewVideos { get; set; }

    }
    public sealed class SubscriptionManager
    {
        private readonly SubscriptionRegistrationRepository _subscriptionRegistrationRepository;
        private readonly SubscriptionFeedResultRepository _subscriptionFeedResultRepository;
        private readonly ChannelProvider _channelProvider;
        private readonly SearchProvider _searchProvider;
        private readonly UserProvider _userProvider;
        private readonly MylistProvider _mylistProvider;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly SeriesRepository _seriesRepository;

        public event EventHandler<SubscriptionFeedUpdateResult> Updated;

        public event EventHandler<SubscriptionSourceEntity> Added;
        public event EventHandler<SubscriptionSourceEntity> Removed;


        public SubscriptionManager(
            SubscriptionRegistrationRepository subscriptionRegistrationRepository,
            SubscriptionFeedResultRepository subscriptionFeedResultRepository,
            Provider.ChannelProvider channelProvider,
            Provider.SearchProvider searchProvider,
            Provider.UserProvider userProvider,
            Provider.MylistProvider mylistProvider,
            NicoVideoProvider nicoVideoProvider,
            SeriesRepository seriesRepository
            )
        {
            _subscriptionRegistrationRepository = subscriptionRegistrationRepository;
            _subscriptionFeedResultRepository = subscriptionFeedResultRepository;
            _channelProvider = channelProvider;
            _searchProvider = searchProvider;
            _userProvider = userProvider;
            _mylistProvider = mylistProvider;
            _nicoVideoProvider = nicoVideoProvider;
            _seriesRepository = seriesRepository;
        }

        public SubscriptionSourceEntity AddSubscription(IVideoContent video)
        {
            var owner = Database.NicoVideoOwnerDb.Get(video.ProviderId);
            if (owner == null)
            {
                throw new Exception("cannot resolve name for video provider from local DB.");
            }

            if (video.ProviderType == Database.NicoVideoUserType.Channel)
            {                
                return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = owner.ScreenName, SourceParameter = video.ProviderId, SourceType = SubscriptionSourceType.Channel });
            }
            else if (video.ProviderType == Database.NicoVideoUserType.User)
            {
                return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = owner.ScreenName, SourceParameter = video.ProviderId, SourceType = SubscriptionSourceType.User });
            }
            else
            {
                throw new NotSupportedException(video.ProviderType.ToString());
            }
        }

        public SubscriptionSourceEntity AddSubscription(IMylist mylist)
        {
            return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = mylist.Label, SourceParameter = mylist.Id, SourceType = SubscriptionSourceType.Mylist });
        }

        public SubscriptionSourceEntity AddKeywordSearchSubscription(string keyword)
        {
            return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = keyword, SourceParameter = keyword, SourceType = SubscriptionSourceType.SearchWithKeyword });
        }
        public SubscriptionSourceEntity AddTagSearchSubscription(string tag)
        {
            return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = tag, SourceParameter = tag, SourceType = SubscriptionSourceType.SearchWithTag });
        }


        public SubscriptionSourceEntity AddSubscription(SubscriptionSourceType sourceType, string sourceParameter, string label)
        {
            return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = label, SourceParameter = sourceParameter, SourceType = sourceType });
        }

        SubscriptionSourceEntity AddSubscription_Internal(SubscriptionSourceEntity newEntity)
        {
            var entity = _subscriptionRegistrationRepository.CreateItem(newEntity);

            Added?.Invoke(this, entity);

            return entity;
        }

        public void UpdateSubscription(SubscriptionSourceEntity entity)
        {
            _subscriptionRegistrationRepository.UpdateItem(entity);
        }

        public void RemoveSubscription(SubscriptionSourceEntity entity)
        {
            var registrationRemoved = _subscriptionRegistrationRepository.DeleteItem(entity.Id);
            Debug.WriteLine("[SubscriptionSource Remove] registration removed: " + registrationRemoved);

            var feedResultRemoved = _subscriptionFeedResultRepository.DeleteItem(entity.Id);
            Debug.WriteLine("[SubscriptionSource Remove] feed result removed: " + feedResultRemoved);

            if (registrationRemoved || feedResultRemoved)
            {
                Removed?.Invoke(this, entity);
            }
        }

        public IList<SubscriptionSourceEntity> GetAllSubscriptionSourceEntities()
        {
            return _subscriptionRegistrationRepository.ReadAllItems();
        }

        // TODO: 表示向けのFeedResultの取得
        public List<(SubscriptionSourceEntity entity, SubscriptionFeedResult feedResult)> GetAllSubscriptionInfo()
        {
            List<SubscriptionSourceEntity> items = _subscriptionRegistrationRepository.ReadAllItems();
            List<SubscriptionFeedResult> feedResults =_subscriptionFeedResultRepository.ReadAllItems();
            Dictionary<SubscriptionSourceEntity, SubscriptionFeedResult> map = new Dictionary<SubscriptionSourceEntity, SubscriptionFeedResult>();
            foreach (var entity in items)
            {
                var result = feedResults.Find(x => x.SourceType == entity.SourceType && x.SourceParamater == entity.SourceParameter);
                if (result != null)
                {
                    map.Add(entity, result);
                }
                else
                {
                    map.Add(entity, new SubscriptionFeedResult() { SourceType = entity.SourceType, SourceParamater = entity.SourceParameter, Videos = new List<FeedResultVideoItem>() });
                }
            }

            return map.Select(x => (entity: x.Key, feedResult: x.Value)).ToList();
        }


        private static readonly TimeSpan _FeedResultUpdateInterval = TimeSpan.FromMinutes(5);

        static bool IsExpiredFeedResultUpdatedTime(DateTime lastUpdatedAt)
        {
            return lastUpdatedAt + _FeedResultUpdateInterval < DateTime.Now;
        }

        public async Task RefreshAllFeedUpdateResultAsync(CancellationToken cancellationToken = default)
        {
            IList<SubscriptionSourceEntity> entities = _subscriptionRegistrationRepository.ReadAllItems();
            foreach(var entity in entities.Where(x => x.IsEnabled).OrderBy(x => x.SortIndex))
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<Exception> exceptions = new List<Exception>();
                try
                {
                    await RefreshFeedUpdateResultAsync(entity, cancellationToken);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }

                if (exceptions.Any())
                {
                    throw new AggregateException("failed update subscrition.", exceptions);
                }
            }
        }


        public async Task<bool> RefreshFeedUpdateResultAsync(SubscriptionSourceEntity entity, CancellationToken cancellationToken = default)
        {
            var prevResult = _subscriptionFeedResultRepository.GetFeedResult(entity);
            if (prevResult != null && !IsExpiredFeedResultUpdatedTime(prevResult.LastUpdatedAt))
            {
                // 前回更新から時間経っていない場合はスキップする
                Debug.WriteLine("[FeedUpdate] update skip: " + entity.Label);
                return false;
            }

            Debug.WriteLine("[FeedUpdate] start: " + entity.Label);

            // オンラインソースから情報を取得して
            var result = await GetFeedResultAsync(entity);

            cancellationToken.ThrowIfCancellationRequested();

            // 新規動画を抽出する
            // 初回更新時は新着抽出をスキップする
            if (prevResult != null)
            {
                var prevContainVideoIds = prevResult.Videos.Select(x => x.VideoId).ToHashSet();
                var newVideos = result.Videos.TakeWhile(x => !prevContainVideoIds.Contains(x.Id));
                result.NewVideos = newVideos.ToList();
            }
            else
            {
                result.NewVideos = new List<NicoVideo>();
            }

            // 成功したら前回までの内容に追記して保存する
            if (result.IsSuccessed && (result.Videos?.Any() ?? false))
            {
                var updatedResult = _subscriptionFeedResultRepository.MargeFeedResult(prevResult, entity, result.Videos);
            }

            // 更新を通知する
            Updated?.Invoke(this, result);


            Debug.WriteLine("[FeedUpdate] complete: " + entity.Label);

            return true;
        }

        async Task<SubscriptionFeedUpdateResult> GetFeedResultAsync(SubscriptionSourceEntity entity)
        {
            try
            {
                var videos = entity.SourceType switch
                {
                    SubscriptionSourceType.Mylist => await GetMylistFeedResult(entity.SourceParameter, _mylistProvider),
                    SubscriptionSourceType.User => await GetUserVideosFeedResult(entity.SourceParameter, _userProvider),
                    SubscriptionSourceType.Channel => await GetChannelVideosFeedResult(entity.SourceParameter, _channelProvider, _nicoVideoProvider),
                    SubscriptionSourceType.Series => await GetSeriesVideosFeedResult(entity.SourceParameter, _seriesRepository),
                    SubscriptionSourceType.SearchWithKeyword => await GetKeywordSearchFeedResult(entity.SourceParameter, _searchProvider),
                    SubscriptionSourceType.SearchWithTag => await GetTagSearchFeedResult(entity.SourceParameter, _searchProvider),
                    _ => throw new NotSupportedException(entity.SourceType.ToString())
                };

                return new SubscriptionFeedUpdateResult()
                {
                    IsSuccessed = true,
                    Videos = videos,
                    Entity = entity
                };
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());

                return new SubscriptionFeedUpdateResult()
                {
                    IsSuccessed = false,
                    Videos = new List<NicoVideo>(),
                    Entity = entity
                };
            }
        }


        static private async Task<List<NicoVideo>> GetUserVideosFeedResult(string userId, Provider.UserProvider userProvider)
        {
            var id = uint.Parse(userId);
            List<NicoVideo> items = new List<NicoVideo>();
            uint page = 0;

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
                    video.ThumbnailUrl = item.Thumbnail.ListingUrl.OriginalString;
                    video.Length = TimeSpan.FromSeconds(item.Duration);
                    video.Owner = video.Owner ?? new Database.NicoVideoOwner() 
                    {
                        OwnerId = userId,
                        UserType = Database.NicoVideoUserType.User
                    };
                    
                    Database.NicoVideoDb.AddOrUpdate(video);
                    items.Add(video);
                }
            }


            return items;
        }

        static private async Task<List<NicoVideo>> GetChannelVideosFeedResult(string channelId, Provider.ChannelProvider channelProvider, NicoVideoProvider nicoVideoProvider)
        {
            List<NicoVideo> items = new List<NicoVideo>();
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
                    if (video.VideoId == null)
                    {
                        video = await nicoVideoProvider.GetNicoVideoInfo(item.ItemId);
                    }
                    items.Add(video);
                }
            }

            return items;
        }


        static private async Task<List<NicoVideo>> GetSeriesVideosFeedResult(string seriesId, SeriesRepository seriesRepository)
        {
            var result = await seriesRepository.GetSeriesVideosAsync(seriesId);

            return result.Videos.OrderByDescending(x => x.PostAt).Select(video =>
            {
                var v = NicoVideoDb.Get(video.Id);

                v.Title = video.Title;
                v.VideoId = video.Id;
                v.PostedAt = video.PostAt;
                v.Length = video.Duration;
                v.MylistCount = video.MylistCount;
                v.ViewCount = video.WatchCount;
                v.CommentCount = video.CommentCount;
                v.ThumbnailUrl = video.ThumbnailUrl.OriginalString;

                NicoVideoDb.AddOrUpdate(v);
                return v;
            }).ToList()
            ;
        }

        static private async Task<List<NicoVideo>> GetMylistFeedResult(string mylistId, Provider.MylistProvider mylistProvider)
        {
            List<NicoVideo> items = new List<NicoVideo>();
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

        static private async Task<List<NicoVideo>> GetKeywordSearchFeedResult(string keyword, Provider.SearchProvider searchProvider)
        {
            List<NicoVideo> items = new List<NicoVideo>();
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
                    video.Length = item.Video.Length;
                    video.PostedAt = item.Video.FirstRetrieve;

                    video.Owner = video.Owner ?? item.Video.ProviderType switch
                    {
                        "channel" => new NicoVideoOwner() { OwnerId = item.Video.CommunityId, UserType = NicoVideoUserType.Channel },
                        _ => new NicoVideoOwner() { OwnerId = item.Video.UserId, UserType = NicoVideoUserType.User }
                    };
                    Database.NicoVideoDb.AddOrUpdate(video);
                    items.Add(video);
                }
            }


            return items;
        }

        static private async Task<List<NicoVideo>> GetTagSearchFeedResult(string tag, Provider.SearchProvider searchProvider)
        {
            List<NicoVideo> items = new List<NicoVideo>();
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
                    video.Length = item.Video.Length;
                    video.PostedAt = item.Video.FirstRetrieve;
                    video.Description = item.Video.Description;
                    video.IsDeleted = item.Video.IsDeleted;
                    video.Owner = video.Owner ?? item.Video.ProviderType switch
                    {
                        "channel" => new NicoVideoOwner() { OwnerId = item.Video.CommunityId, UserType = NicoVideoUserType.Channel },
                        _ => new NicoVideoOwner() { OwnerId = item.Video.UserId, UserType = NicoVideoUserType.User }
                    };
                    Database.NicoVideoDb.AddOrUpdate(video);
                    items.Add(video);
                }
            }


            return items;
        }

    }
}

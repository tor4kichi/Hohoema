using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Provider;
using NicoPlayerHohoema.Models.Subscriptions;
using NicoPlayerHohoema.Repository.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;

namespace NicoPlayerHohoema.Models.Subscriptions
{
    public class SubscriptionFeedUpdateResult
    {
        public bool IsSuccessed { get; set; }
        public SubscriptionSourceEntity Entity { get; set; }
        public List<IVideoContent> Videos { get; set; }

    }
    public sealed class SubscriptionManager
    {
        private readonly SubscriptionRegistrationRepository _subscriptionRegistrationRepository;
        private readonly SubscriptionFeedResultRepository _subscriptionFeedResultRepository;
        private readonly ChannelProvider _channelProvider;
        private readonly SearchProvider _searchProvider;
        private readonly UserProvider _userProvider;
        private readonly MylistProvider _mylistProvider;


        public event EventHandler<SubscriptionFeedUpdateResult> Updated;

        public event EventHandler<SubscriptionSourceEntity> Added;
        public event EventHandler<SubscriptionSourceEntity> Removed;


        public SubscriptionManager(
            SubscriptionRegistrationRepository subscriptionRegistrationRepository,
            SubscriptionFeedResultRepository subscriptionFeedResultRepository,
            Provider.ChannelProvider channelProvider,
            Provider.SearchProvider searchProvider,
            Provider.UserProvider userProvider,
            Provider.MylistProvider mylistProvider

            )
        {
            _subscriptionRegistrationRepository = subscriptionRegistrationRepository;
            _subscriptionFeedResultRepository = subscriptionFeedResultRepository;
            _channelProvider = channelProvider;
            _searchProvider = searchProvider;
            _userProvider = userProvider;
            _mylistProvider = mylistProvider;
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
            var registrationRemoved = _subscriptionRegistrationRepository.DeleteItem(entity);
            Debug.WriteLine("[SubscriptionSource Remove] registration removed: " + registrationRemoved);

            var feedResultRemoved = _subscriptionFeedResultRepository.DeleteItem(entity);
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
            foreach(var entity in entities)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await RefreshFeedUpdateResultAsync(entity, cancellationToken);
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

            // 成功したら前回までの内容に追記して保存する
            if (result.IsSuccessed && (result.Videos?.Any() ?? false))
            {
                _ = Task.Run(() => _subscriptionFeedResultRepository.MargeFeedResult(prevResult, entity, result.Videos));
            }

            try
            {
                Updated?.Invoke(this, result);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Debug.WriteLine("[FeedUpdate] Failed Updated. " + entity.Label);
            }

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
                    SubscriptionSourceType.Channel => await GetChannelVideosFeedResult(entity.SourceParameter, _channelProvider),
                    SubscriptionSourceType.Series => throw new NotSupportedException(entity.SourceType.ToString()),
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
                    Videos = new List<IVideoContent>(),
                    Entity = entity
                };
            }
        }


        static private async Task<List<IVideoContent>> GetUserVideosFeedResult(string userId, Provider.UserProvider userProvider)
        {
            var id = uint.Parse(userId);
            List<IVideoContent> items = new List<IVideoContent>();
            uint page = 1;

            var res = await userProvider.GetUserVideos(id, page);

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
                    video.ThumbnailUrl = item.ThumbnailUrl.OriginalString;
                    video.Length = item.Length;
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
                    video.Length = item.Length;
                    video.LastUpdated = item.PostedAt;
                    video.ThumbnailUrl = item.ThumbnailUrl;

                    Database.NicoVideoDb.AddOrUpdate(video);
                    items.Add(video);
                }
            }

            return items;
        }

        static private async Task<List<IVideoContent>> GetMylistFeedResult(string mylistId, Provider.MylistProvider mylistProvider)
        {
            List<IVideoContent> items = new List<IVideoContent>();
            int page = 0;
            const int itemGetCountPerPage = 50;
            var head = page * itemGetCountPerPage;
            var tail = head + itemGetCountPerPage;
            var result = await mylistProvider.GetMylistGroupVideo(mylistId, head, itemGetCountPerPage);

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

using Hohoema.Models.Domain.Niconico.Channel;
using Hohoema.Models.Domain.Niconico.Search;
using Hohoema.Models.Domain.Niconico.User;
using Hohoema.Models.Domain.Niconico.Mylist;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Video.Series;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.Video;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.User;
using LiteDB;

namespace Hohoema.Models.Domain.Subscriptions
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
        private readonly SubscFeedVideoRepository _subscFeedVideoRepository;
        private readonly ChannelProvider _channelProvider;
        private readonly SearchProvider _searchProvider;
        private readonly UserProvider _userProvider;
        private readonly MylistProvider _mylistProvider;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly SeriesProvider _seriesRepository;
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;
        
        public SubscriptionManager(
            SubscriptionRegistrationRepository subscriptionRegistrationRepository,
            SubscFeedVideoRepository subscFeedVideoRepository,
            ChannelProvider channelProvider,
            SearchProvider searchProvider,
            UserProvider userProvider,
            MylistProvider mylistProvider,
            NicoVideoProvider nicoVideoProvider,
            SeriesProvider seriesRepository,
            NicoVideoOwnerCacheRepository nicoVideoOwnerRepository
            )
        {
            _subscriptionRegistrationRepository = subscriptionRegistrationRepository;
            _subscFeedVideoRepository = subscFeedVideoRepository;
            _channelProvider = channelProvider;
            _searchProvider = searchProvider;
            _userProvider = userProvider;
            _mylistProvider = mylistProvider;
            _nicoVideoProvider = nicoVideoProvider;
            _seriesRepository = seriesRepository;
            _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
        }

        public SubscriptionSourceEntity AddSubscription(IVideoContentProvider video)
        {
            var owner = _nicoVideoOwnerRepository.Get(video.ProviderId);
            if (owner == null)
            {
                throw new Models.Infrastructure.HohoemaExpception("cannot resolve name for video provider from local DB.");
            }

            if (video.ProviderType == OwnerType.Channel)
            {                
                return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = owner.ScreenName, SourceParameter = video.ProviderId, SourceType = SubscriptionSourceType.Channel });
            }
            else if (video.ProviderType == OwnerType.User)
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
            return AddSubscription_Internal(new SubscriptionSourceEntity() { Label = mylist.Name, SourceParameter = mylist.PlaylistId.Id, SourceType = SubscriptionSourceType.Mylist });
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
            _subscriptionRegistrationRepository.CreateItem(newEntity);

            return newEntity;
        }

        public void UpdateSubscription(SubscriptionSourceEntity entity)
        {
            _subscriptionRegistrationRepository.UpdateItem(entity);
        }

        public void RemoveSubscription(SubscriptionSourceEntity entity)
        {
            var registrationRemoved = _subscriptionRegistrationRepository.DeleteItem(entity.Id);
            Debug.WriteLine("[SubscriptionSource Remove] registration removed: " + registrationRemoved);

            var feedResultRemoved = _subscFeedVideoRepository.DeleteSubsc(entity);
            Debug.WriteLine("[SubscriptionSource Remove] feed result removed: " + feedResultRemoved);
        }

        public IList<SubscriptionSourceEntity> GetAllSubscriptionSourceEntities()
        {
            return _subscriptionRegistrationRepository.ReadAllItems();
        }

        public SubscriptionSourceEntity getSubscriptionSourceEntity(ObjectId id)
        {
            return _subscriptionRegistrationRepository.FindById(id);
        }


        private static readonly TimeSpan _FeedResultUpdateInterval = TimeSpan.FromMinutes(60);

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
                catch (OperationCanceledException)
                {
                    return;
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
            var result = await Task.Run(async () => 
            {                                
                if (!IsExpiredFeedResultUpdatedTime(entity.LastUpdateAt))
                {
                    // 前回更新から時間経っていない場合はスキップする
                    Debug.WriteLine("[FeedUpdate] update skip: " + entity.Label);
                    return null;
                }

                Debug.WriteLine("[FeedUpdate] start: " + entity.Label);

                // オンラインソースから情報を取得して
                var result = await GetFeedResultAsync(entity);

                cancellationToken.ThrowIfCancellationRequested();

                DateTime now = DateTime.Now;
                var newVideos = _subscFeedVideoRepository.RegisteringVideosIfNotExist(entity.Id, now, result.Videos).ToList();
                if (entity.LastUpdateAt != DateTime.MinValue)
                {
                    result.NewVideos = newVideos.ToList();
                }
                else
                {
                    result.NewVideos = new List<NicoVideo>();
                }

                entity.LastUpdateAt = now;
                _subscriptionRegistrationRepository.UpdateItem(entity);

                return result;

            }, cancellationToken);

            if (result != null)
            {
                // 更新を通知する
                Debug.WriteLine("[FeedUpdate] complete: " + entity.Label);

                return true;
            }
            else
            {
                Debug.WriteLine("[FeedUpdate] complete: " + entity.Label);
                return false;
            }
        }


        public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(SubscriptionSourceEntity source, int skip = 0, int limit = int.MaxValue)
        {
            return _subscFeedVideoRepository.GetVideo(source.Id, skip, limit);
        }

        public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(int skip = 0, int limit = int.MaxValue)
        {
            return _subscFeedVideoRepository.GetVideos(skip, limit);
        }

        public void UpdateFeedVideos(IEnumerable<SubscFeedVideo> videos)
        {
            _subscFeedVideoRepository.UpdateVideos(videos);
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


        private async Task<List<NicoVideo>> GetUserVideosFeedResult(UserId userId, UserProvider userProvider)
        {
            var id = uint.Parse(userId);
            List<NicoVideo> items = new List<NicoVideo>();
            int page = 0;

            var res = await userProvider.GetUserVideosAsync(id, page, 50);

            var videoItems = res.Data.Items;
            var currentItemsCount = videoItems?.Length ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {

            }
            else
            {
                foreach (var item in videoItems)
                {
                    var videoItem = item.Essential;
                    var video = _nicoVideoProvider.UpdateCache(videoItem.Id, videoItem);

                    items.Add(video);
                }
            }


            return items;
        }

        private async Task<List<NicoVideo>> GetChannelVideosFeedResult(string channelId,ChannelProvider channelProvider, NicoVideoProvider nicoVideoProvider)
        {
            int page = 0;
            var res = await channelProvider.GetChannelVideo(channelId, page);

            var videoItems = res.Data.Videos;
            var currentItemsCount = videoItems?.Length ?? 0;
            
            if (videoItems == null || currentItemsCount == 0)
            {
                return new List<NicoVideo>();
            }

            List<NicoVideo> items = new List<NicoVideo>();
            foreach (var item in videoItems)
            {
                var video = nicoVideoProvider.UpdateCache(item.ItemId, video =>
                {
                    video.Title = item.Title;
                    video.PostedAt = item.PostedAt;
                    video.Length = item.Length;
                    video.Description ??= item.ShortDescription;
                    video.ThumbnailUrl = item.ThumbnailUrl;

                    return default;
                });

                items.Add(video);
            }

            return items;
        }


        private async Task<List<NicoVideo>> GetSeriesVideosFeedResult(string seriesId, SeriesProvider seriesRepository)
        {
            var result = await seriesRepository.GetSeriesVideosAsync(seriesId);

            return result.Videos.OrderByDescending(x => x.PostAt).Select(video =>
            {
                return _nicoVideoProvider.UpdateCache(video.Id, v => 
                {
                    v.Title = video.Title;
                    v.VideoAliasId = video.Id;
                    v.PostedAt = video.PostAt;
                    v.Length = video.Duration;
                    v.ThumbnailUrl = video.ThumbnailUrl.OriginalString;

                    return (false, default);
                });
            }).ToList()
            ;
        }

        static private async Task<List<NicoVideo>> GetMylistFeedResult(MylistId mylistId, MylistProvider mylistProvider)
        {
            List<NicoVideo> items = new List<NicoVideo>();
            int page = 0;
            const int itemGetCountPerPage = 50;
            var result = await mylistProvider.GetMylistVideoItems(mylistId, page, itemGetCountPerPage, MylistSortKey.AddedAt, MylistSortOrder.Desc);

            var videoItems = result.Items;
            var currentItemsCount = videoItems?.Count ?? 0;
            if (result.IsSuccess)
            {
                items.AddRange(result.NicoVideoItems);
            }

            return items;
        }

        private async Task<List<NicoVideo>> GetKeywordSearchFeedResult(string keyword, SearchProvider searchProvider)
        {
            List<NicoVideo> items = new List<NicoVideo>();
            int page = 0;
            const int itemGetCountPerPage = 50;

            var head = page * itemGetCountPerPage;
            var res = await searchProvider.GetKeywordSearch(keyword, head, itemGetCountPerPage);

            var videoItems = res.Videos;
            var currentItemsCount = videoItems?.Length ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {

            }
            else
            {
                foreach (var item in videoItems)
                {
                    var video = _nicoVideoProvider.UpdateCache(item.Video.Id, video => 
                    {
                        video.VideoAliasId = item.Video.Id;
                        video.Title = item.Video.Title;
                        video.PostedAt = item.Video.FirstRetrieve.DateTime;
                        video.Length = item.Video.Duration;
                        video.Description = item.Video.Description;
                        video.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
                        
                        video.Owner ??= item.Video.ProviderType switch
                        {
                            VideoProviderType.Channel => new NicoVideoOwner() { OwnerId = item.Video.CommunityId, UserType = OwnerType.Channel },
                            _ => new NicoVideoOwner() { OwnerId = item.Video.UserId.ToString(), UserType = OwnerType.User }
                        };

                        return (item.Video.IsDeleted, item.Video.Deleted);
                    });

                   
                    items.Add(video);
                }
            }


            return items;
        }

        private async Task<List<NicoVideo>> GetTagSearchFeedResult(string tag,SearchProvider searchProvider)
        {
            List<NicoVideo> items = new List<NicoVideo>();
            int page = 0;
            const int itemGetCountPerPage = 50;

            var head = page * itemGetCountPerPage;
            var res = await searchProvider.GetTagSearch(tag, head, itemGetCountPerPage);

            var videoItems = res.Videos;
            var currentItemsCount = videoItems?.Length ?? 0;
            if (videoItems == null || currentItemsCount == 0)
            {

            }
            else
            {
                foreach (var item in videoItems)
                {
                    var video = _nicoVideoProvider.UpdateCache(item.Video.Id, video => 
                    {
                        video.Title = item.Video.Title;
                        video.PostedAt = item.Video.FirstRetrieve.DateTime;
                        video.Length = item.Video.Duration;
                        video.Description = item.Video.Description;
                        video.ThumbnailUrl = item.Video.ThumbnailUrl.OriginalString;
                        video.Owner = video.Owner ?? item.Video.ProviderType switch
                        {
                            VideoProviderType.Channel => new NicoVideoOwner() { OwnerId = item.Video.CommunityId, UserType = OwnerType.Channel },
                            _ => new NicoVideoOwner() { OwnerId = item.Video.UserId.ToString(), UserType = OwnerType.User }
                        };

                        return (item.Video.IsDeleted, default);
                    });

                    items.Add(video);
                }
            }


            return items;
        }

    }
}

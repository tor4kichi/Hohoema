#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using LiteDB;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Subscriptions;

public class SubscriptionFeedUpdateResult
{
    public bool IsSuccessed { get; set; }
    public SubscriptionSourceEntity Entity { get; set; }
    public List<NicoVideo> Videos { get; set; }
    public List<NicoVideo> NewVideos { get; set; }

}
public sealed class SubscriptionManager
{
    private readonly IMessenger _messenger;
    private readonly SubscriptionRegistrationRepository _subscriptionRegistrationRepository;
    private readonly SubscFeedVideoRepository _subscFeedVideoRepository;
    private readonly ChannelProvider _channelProvider;
    private readonly SearchProvider _searchProvider;
    private readonly UserProvider _userProvider;
    private readonly MylistProvider _mylistProvider;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly SeriesProvider _seriesRepository;
    private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;
    private readonly SubscriptionGroupRepository _subscriptionGroupRepository;

    public SubscriptionManager(
        IMessenger messenger,
        SubscriptionRegistrationRepository subscriptionRegistrationRepository,
        SubscFeedVideoRepository subscFeedVideoRepository,
        ChannelProvider channelProvider,
        SearchProvider searchProvider,
        UserProvider userProvider,
        MylistProvider mylistProvider,
        NicoVideoProvider nicoVideoProvider,
        SeriesProvider seriesRepository,
        NicoVideoOwnerCacheRepository nicoVideoOwnerRepository,
        SubscriptionGroupRepository subscriptionGroupRepository
        )
    {
        _messenger = messenger;
        _subscriptionRegistrationRepository = subscriptionRegistrationRepository;
        _subscFeedVideoRepository = subscFeedVideoRepository;
        _channelProvider = channelProvider;
        _searchProvider = searchProvider;
        _userProvider = userProvider;
        _mylistProvider = mylistProvider;
        _nicoVideoProvider = nicoVideoProvider;
        _seriesRepository = seriesRepository;
        _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
        _subscriptionGroupRepository = subscriptionGroupRepository;
    }

    public List<SubscriptionGroup> GetSubscGroups()
    {
        return _subscriptionGroupRepository.ReadAllItems();
    }

    public SubscriptionGroup CreateSubscriptionGroup(string name)
    {
        SubscriptionGroup group = new SubscriptionGroup(name);
        _subscriptionGroupRepository.CreateItem(group);       
        _messenger.Send(new SubscriptionGroupCreatedMessage(group));
        return group;
    }

    public bool DeleteSubscriptionGroup(SubscriptionGroup group)
    {
        var sources = _subscriptionRegistrationRepository.Find(x => x.Group!.Id == group.Id);
        foreach (var source in sources)
        {
            source.Group = null;
            UpdateSubscription(source);
        }

        bool result = _subscriptionGroupRepository.DeleteItem(group.Id);
        if (result) 
        {
            _messenger.Send(new SubscriptionGroupDeletedMessage(group));
        }

        return result;
    }

    public void ReoderSubscriptionGroups(IEnumerable<SubscriptionGroup> groups)
    {
        foreach (var (group, index) in groups.Select((x, i) => (x, i)))
        {
            group.Order = index;
        }
        _subscriptionGroupRepository.UpdateItem(groups);

        _messenger.Send(new SubscriptionGroupReorderedMessage(groups.ToList()));
    }

    public SubscriptionSourceEntity AddSubscription(IVideoContentProvider video, SubscriptionGroup? group = null)
    {
        NicoVideoOwner owner = _nicoVideoOwnerRepository.Get(video.ProviderId);
        if (owner == null)
        {
            throw new Infra.HohoemaException("cannot resolve name for video provider from local DB.");
        }

        var sourceType = video.ProviderType switch
        {
            OwnerType.Channel => SubscriptionSourceType.Channel,
            OwnerType.User => SubscriptionSourceType.User,
            _ => throw new NotSupportedException(video.ProviderType.ToString())
        };

        return AddSubscription_Internal(new SubscriptionSourceEntity()
        {
            Label = owner.ScreenName,
            SourceParameter = video.ProviderId,
            SourceType = sourceType,
            Group = group
        });
    }

    public SubscriptionSourceEntity AddSubscription(IMylist mylist, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new SubscriptionSourceEntity()
        {
            Label = mylist.Name,
            SourceParameter = mylist.PlaylistId.Id,
            SourceType = SubscriptionSourceType.Mylist,
            Group = group
        });
    }

    public SubscriptionSourceEntity AddKeywordSearchSubscription(string keyword, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new SubscriptionSourceEntity()
        {
            Label = keyword,
            SourceParameter = keyword,
            SourceType = SubscriptionSourceType.SearchWithKeyword,
            Group = group,
        });
    }
    public SubscriptionSourceEntity AddTagSearchSubscription(string tag, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new SubscriptionSourceEntity()
        {
            Label = tag,
            SourceParameter = tag,
            SourceType = SubscriptionSourceType.SearchWithTag,
            Group = group,
        });
    }


    public SubscriptionSourceEntity AddSubscription(SubscriptionSourceType sourceType, string sourceParameter, string label, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new SubscriptionSourceEntity()
        {
            Label = label,
            SourceParameter = sourceParameter,
            SourceType = sourceType,
            Group = group,
        });
    }

    private SubscriptionSourceEntity AddSubscription_Internal(SubscriptionSourceEntity newEntity)
    {
        _ = _subscriptionRegistrationRepository.CreateItem(newEntity);

        return newEntity;
    }

    public void UpdateSubscription(SubscriptionSourceEntity entity)
    {
        _ = _subscriptionRegistrationRepository.UpdateItem(entity);
    }

    public void UpdateSubscriptionGroup(SubscriptionGroup group)
    {
        _subscriptionGroupRepository.UpdateItem(group);
    }

    public void RemoveSubscription(SubscriptionSourceEntity entity)
    {
        bool registrationRemoved = _subscriptionRegistrationRepository.DeleteItem(entity.Id);
        Debug.WriteLine("[SubscriptionSource Remove] registration removed: " + registrationRemoved);

        bool feedResultRemoved = _subscFeedVideoRepository.DeleteSubsc(entity);
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

    public bool TryGetSubscriptionGroup(SubscriptionSourceType sourceType, string id, out SubscriptionGroup? outGroup)
    {
        return _subscriptionRegistrationRepository.TryGetSubscriptionGroup(sourceType, id, out outGroup);
    }

    public bool TryGetSubscriptionGroup(SubscriptionSourceType sourceType, string id, out SubscriptionSourceEntity outSourceEntity, out SubscriptionGroup? outGroup)
    {
        return _subscriptionRegistrationRepository.TryGetSubscriptionGroup(sourceType, id, out outSourceEntity, out outGroup);
    }


    private static readonly TimeSpan _FeedResultUpdateInterval = TimeSpan.FromMinutes(60);

    private static bool IsExpiredFeedResultUpdatedTime(DateTime lastUpdatedAt)
    {
        return lastUpdatedAt + _FeedResultUpdateInterval < DateTime.Now;
    }

    public async ValueTask RefreshAllFeedUpdateResultAsync(CancellationToken cancellationToken = default)
    {
        IList<SubscriptionSourceEntity> entities = _subscriptionRegistrationRepository.ReadAllItems();
        foreach (SubscriptionSourceEntity entity in entities.Where(x => x.IsEnabled).OrderBy(x => x.SortIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Exception> exceptions = new();
            try
            {
                _ = await RefreshFeedUpdateResultAsync(entity, cancellationToken);
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


    public async ValueTask<bool> RefreshFeedUpdateResultAsync(SubscriptionSourceEntity entity, CancellationToken cancellationToken = default)
    {
        if (!IsExpiredFeedResultUpdatedTime(entity.LastUpdateAt))
        {
            // 前回更新から時間経っていない場合はスキップする
            Debug.WriteLine("[FeedUpdate] update skip: " + entity.Label);
            return false;
        }

        SubscriptionFeedUpdateResult result = await Task.Run(async () =>
        {
            Debug.WriteLine("[FeedUpdate] start: " + entity.Label);

            // オンラインソースから情報を取得して
            SubscriptionFeedUpdateResult result = await GetFeedResultAsync(entity);

            cancellationToken.ThrowIfCancellationRequested();

            DateTime now = DateTime.Now;
            List<NicoVideo> newVideos = _subscFeedVideoRepository.RegisteringVideosIfNotExist(entity.Id, now, result.Videos).ToList();
            result.NewVideos = entity.LastUpdateAt != DateTime.MinValue ? newVideos.ToList() : new List<NicoVideo>();

            entity.LastUpdateAt = now;
            _ = _subscriptionRegistrationRepository.UpdateItem(entity);

            return result;

        }, cancellationToken);

        // 更新を通知する
        Debug.WriteLine("[FeedUpdate] complete: " + entity.Label);

        return true;
    }


    public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(SubscriptionSourceEntity source, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideos(source.Id, skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetAllSubscFeedVideos(int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideos(skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(SubscriptionGroup? group, int skip = 0, int limit = int.MaxValue)
    {
        return (group != null
            ? _subscriptionRegistrationRepository.Find(x => x.Group!.Id == group.Id)
            : _subscriptionRegistrationRepository.Find(x => x.Group == null)
            )
            .SelectMany(subsc => _subscFeedVideoRepository.GetVideos(subsc.Id))
            .Distinct()
            .OrderByDescending(x => x.PostAt)
            .Skip(skip)
            .Take(limit)
            ;

        //var subscSources = _subscriptionRegistrationRepository.Find(x => x.Group == group);
        //return _subscFeedVideoRepository.GetVideos(subscSources.Select(x => x.Id), skip, limit);
    }

    public void UpdateFeedVideos(IEnumerable<SubscFeedVideo> videos)
    {
        _subscFeedVideoRepository.UpdateVideos(videos);
    }

    private async Task<SubscriptionFeedUpdateResult> GetFeedResultAsync(SubscriptionSourceEntity entity)
    {
        try
        {
            List<NicoVideo> videos = entity.SourceType switch
            {
                SubscriptionSourceType.Mylist => await GetMylistFeedResult(entity.SourceParameter, _mylistProvider),
                SubscriptionSourceType.User => await GetUserVideosFeedResult(entity.SourceParameter, _userProvider),
                SubscriptionSourceType.Channel => await GetChannelVideosFeedResult(entity.SourceParameter, _channelProvider, _nicoVideoProvider),
                SubscriptionSourceType.Series => await GetSeriesVideosFeedResult(entity.SourceParameter, _seriesRepository),
                SubscriptionSourceType.SearchWithKeyword => await GetKeywordSearchFeedResult(entity.SourceParameter, _searchProvider),
                SubscriptionSourceType.SearchWithTag => await GetTagSearchFeedResult(entity.SourceParameter, _searchProvider),
                _ => throw new NotSupportedException(entity.SourceType.ToString())
            };

            _subscFeedVideoRepository.RegisteringVideosIfNotExist(entity.Id, DateTime.Now, videos);

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
        uint id = uint.Parse(userId);
        List<NicoVideo> items = new();
        int page = 0;

        UserVideoResponse res = await userProvider.GetUserVideosAsync(id, page, 50);

        UserVideoItem[] videoItems = res.Data.Items;
        int currentItemsCount = videoItems?.Length ?? 0;
        if (videoItems == null || currentItemsCount == 0)
        {

        }
        else
        {
            foreach (UserVideoItem item in videoItems)
            {
                NvapiVideoItem videoItem = item.Essential;
                NicoVideo video = _nicoVideoProvider.UpdateCache(videoItem.Id, videoItem);

                items.Add(video);                
            }
        }

        return items;
    }

    private async Task<List<NicoVideo>> GetChannelVideosFeedResult(string channelId, ChannelProvider channelProvider, NicoVideoProvider nicoVideoProvider)
    {
        int page = 0;
        NiconicoToolkit.Channels.ChannelVideoResponse res = await channelProvider.GetChannelVideo(channelId, page);

        NiconicoToolkit.Channels.ChannelVideoItem[] videoItems = res.Data.Videos;
        int currentItemsCount = videoItems?.Length ?? 0;

        if (videoItems == null || currentItemsCount == 0)
        {
            return new List<NicoVideo>();
        }

        List<NicoVideo> items = new();
        foreach (NiconicoToolkit.Channels.ChannelVideoItem item in videoItems)
        {
            NicoVideo video = nicoVideoProvider.UpdateCache(item.ItemId, video =>
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
        NiconicoToolkit.Series.NvapiSeriesVidoesResponseContainer result = await seriesRepository.GetSeriesVideosAsync(seriesId);

        return result.Data.Items.OrderByDescending(x => x.Video.RegisteredAt).Select(video =>
        {
            return _nicoVideoProvider.UpdateCache(video.Video.Id, v =>
            {
                v.Title = video.Video.Title;
                v.VideoAliasId = video.Video.Id;
                v.PostedAt = video.Video.RegisteredAt.DateTime;
                v.Length = TimeSpan.FromSeconds(video.Video.Duration);
                v.ThumbnailUrl = video.Video.Thumbnail.MiddleUrl.OriginalString;

                return (false, default);
            });
        }).ToList()
        ;
    }

    private static async Task<List<NicoVideo>> GetMylistFeedResult(MylistId mylistId, MylistProvider mylistProvider)
    {
        List<NicoVideo> items = new();
        int page = 0;
        const int itemGetCountPerPage = 50;
        MylistItemsGetResult result = await mylistProvider.GetMylistVideoItems(mylistId, page, itemGetCountPerPage, MylistSortKey.AddedAt, MylistSortOrder.Desc);

        IReadOnlyCollection<MylistItem> videoItems = result.Items;
        _ = videoItems?.Count ?? 0;
        if (result.IsSuccess)
        {
            items.AddRange(result.NicoVideoItems);
        }

        return items;
    }

    private async Task<List<NicoVideo>> GetKeywordSearchFeedResult(string keyword, SearchProvider searchProvider)
    {
        List<NicoVideo> items = new();
        int page = 0;
        const int itemGetCountPerPage = 50;

        int head = page * itemGetCountPerPage;
        VideoListingResponse res = await searchProvider.GetKeywordSearch(keyword, head, itemGetCountPerPage);

        VideoInfo[] videoItems = res.Videos;
        int currentItemsCount = videoItems?.Length ?? 0;
        if (videoItems == null || currentItemsCount == 0)
        {

        }
        else
        {
            foreach (VideoInfo item in videoItems)
            {
                NicoVideo video = _nicoVideoProvider.UpdateCache(item.Video.Id, video =>
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

    private async Task<List<NicoVideo>> GetTagSearchFeedResult(string tag, SearchProvider searchProvider)
    {
        List<NicoVideo> items = new();
        int page = 0;
        const int itemGetCountPerPage = 50;

        int head = page * itemGetCountPerPage;
        VideoListingResponse res = await searchProvider.GetTagSearch(tag, head, itemGetCountPerPage);

        VideoInfo[] videoItems = res.Videos;
        int currentItemsCount = videoItems?.Length ?? 0;
        if (videoItems == null || currentItemsCount == 0)
        {

        }
        else
        {
            foreach (VideoInfo item in videoItems)
            {
                NicoVideo video = _nicoVideoProvider.UpdateCache(item.Video.Id, video =>
                {
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

                    return (item.Video.IsDeleted, default);
                });

                items.Add(video);
            }
        }


        return items;
    }
}

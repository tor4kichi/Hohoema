#nullable enable
using AngleSharp.Dom;
using AngleSharp.Media;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Infra;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using LiteDB;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.Search;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Subscriptions;

public class SubscriptionFeedUpdateResult
{
    public bool IsSuccessed { get; set; }
    public Subscription Entity { get; set; }
    public List<NicoVideo> Videos { get; set; }
    public List<NicoVideo> NewVideos { get; set; }
}

public sealed class SubscriptionManager
{
    private readonly IMessenger _messenger;
    private readonly NiconicoSession _niconicoSession;
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
    private readonly SubscriptionUpdateRespository _subscriptionUpdateRespository;
    private readonly SubscriptionGroupCheckedRespository _subscriptionGroupCheckedRespository;

    public SubscriptionManager(
        IMessenger messenger,
        NiconicoSession niconicoSession,
        SubscriptionRegistrationRepository subscriptionRegistrationRepository,
        SubscFeedVideoRepository subscFeedVideoRepository,
        ChannelProvider channelProvider,
        SearchProvider searchProvider,
        UserProvider userProvider,
        MylistProvider mylistProvider,
        NicoVideoProvider nicoVideoProvider,
        SeriesProvider seriesRepository,
        NicoVideoOwnerCacheRepository nicoVideoOwnerRepository,
        SubscriptionGroupRepository subscriptionGroupRepository,
        SubscriptionUpdateRespository subscriptionUpdateRespository,
        SubscriptionGroupCheckedRespository subscriptionGroupCheckedRespository
        )
    {
        _messenger = messenger;
        _niconicoSession = niconicoSession;
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
        _subscriptionUpdateRespository = subscriptionUpdateRespository;
        _subscriptionGroupCheckedRespository = subscriptionGroupCheckedRespository;
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

        SetCheckedAt(group.GroupId, DateTime.Now);
        return group;
    }

    public bool DeleteSubscriptionGroup(SubscriptionGroup group)
    {
        var sources = _subscriptionRegistrationRepository.Find(x => x.Group!.GroupId == group.GroupId);
        foreach (var source in sources)
        {
            source.Group = null;
            UpdateSubscription(source);
        }

        bool result = _subscriptionGroupRepository.DeleteItem(group.GroupId);
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

    public Subscription AddSubscription(IVideoContentProvider video, SubscriptionGroup? group = null)
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

        return AddSubscription_Internal(new Subscription()
        {
            Label = owner.ScreenName,
            SourceParameter = video.ProviderId,
            SourceType = sourceType,
            Group = group
        });
    }

    public Subscription AddSubscription(IMylist mylist, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription()
        {
            Label = mylist.Name,
            SourceParameter = mylist.PlaylistId.Id,
            SourceType = SubscriptionSourceType.Mylist,
            Group = group
        });
    }

    public Subscription AddKeywordSearchSubscription(string keyword, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription()
        {
            Label = keyword,
            SourceParameter = keyword,
            SourceType = SubscriptionSourceType.SearchWithKeyword,
            Group = group,
        });
    }
    public Subscription AddTagSearchSubscription(string tag, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription()
        {
            Label = tag,
            SourceParameter = tag,
            SourceType = SubscriptionSourceType.SearchWithTag,
            Group = group,
        });
    }


    public Subscription AddSubscription(SubscriptionSourceType sourceType, string sourceParameter, string label, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription()
        {
            Label = label,
            SourceParameter = sourceParameter,
            SourceType = sourceType,
            Group = group,
        });
    }

    private Subscription AddSubscription_Internal(Subscription newEntity)
    {
        _subscriptionRegistrationRepository.CreateItem(newEntity);         
        _messenger.Send(new SubscriptionAddedMessage(newEntity));
        return newEntity;
    }

    public void UpdateSubscription(Subscription entity)
    {
        _ = _subscriptionRegistrationRepository.UpdateItem(entity);
    }

    public void UpdateSubscriptionGroup(SubscriptionGroup group)
    {
        _subscriptionGroupRepository.UpdateItem(group);
    }

    public void RemoveSubscription(Subscription entity)
    {
        bool registrationRemoved = _subscriptionRegistrationRepository.DeleteItem(entity.SubscriptionId);
        Debug.WriteLine("[SubscriptionSource Remove] registration removed: " + registrationRemoved);

        _messenger.Send(new SubscriptionDeletedMessage(entity.SubscriptionId));

        bool feedResultRemoved = _subscFeedVideoRepository.DeleteSubsc(entity);
        Debug.WriteLine("[SubscriptionSource Remove] feed result removed: " + feedResultRemoved);
    }

    public IList<Subscription> GetAllSubscriptions()
    {
        return _subscriptionRegistrationRepository.ReadAllItems();
    }

    public Subscription GetSubscription(SusbcriptionId id)
    {
        return _subscriptionRegistrationRepository.FindById(id);
    }

    public bool TryGetSubscriptionGroup(SubscriptionSourceType sourceType, string id, out SubscriptionGroup? outGroup)
    {
        return _subscriptionRegistrationRepository.TryGetSubscriptionGroup(sourceType, id, out outGroup);
    }

    public bool TryGetSubscriptionGroup(SubscriptionSourceType sourceType, string id, out Subscription outSourceEntity, out SubscriptionGroup? outGroup)
    {
        return _subscriptionRegistrationRepository.TryGetSubscriptionGroup(sourceType, id, out outSourceEntity, out outGroup);
    }

    public SubscriptionGroup? GetSubscriptionGroup(SubscriptionGroupId subscriptionGroupId)
    {
        return _subscriptionGroupRepository.FindById(subscriptionGroupId);
    }

    public int GetSubscriptionGroupVideosCount(SubscriptionGroup? group) 
    {
        return GetSubscriptionGroupSubscriptions(group)
            .Sum(_subscFeedVideoRepository.GetVideoCount);
    }

    public DateTime GetLastUpdatedAt(SusbcriptionId subscriptionId)
    {
        return _subscriptionUpdateRespository.GetOrAdd(subscriptionId).LastUpdatedAt;
    }

    public DateTime GetLastCheckedAt(SubscriptionGroupId subscriptionGroupId)
    {
        return _subscriptionGroupCheckedRespository.GetOrAdd(subscriptionGroupId).LastCheckedAt;
    }

    public void SetUpdatedAt(SusbcriptionId subscriptionId, DateTime? updatedAt = null)
    {
        updatedAt ??= DateTime.Now;
        var update = _subscriptionUpdateRespository.GetOrAdd(subscriptionId);
        update.LastUpdatedAt = updatedAt.Value;
        _subscriptionUpdateRespository.UpdateItem(update);
    }

    public void SetCheckedAt(SubscriptionGroupId subscriptionGroupId, DateTime? checkedAt = null)
    {
        checkedAt ??= DateTime.Now;
        var entity = _subscriptionGroupCheckedRespository.GetOrAdd(subscriptionGroupId);
        entity.LastCheckedAt = checkedAt.Value;
        _subscriptionGroupCheckedRespository.UpdateItem(entity);
    }


    private static readonly TimeSpan _FeedResultUpdateInterval = TimeSpan.FromMinutes(60);

    private static bool IsExpiredFeedResultUpdatedTime(DateTime lastUpdatedAt)
    {
        return lastUpdatedAt + _FeedResultUpdateInterval < DateTime.Now;
    }

    public async IAsyncEnumerable<SubscriptionFeedUpdateResult> RefreshAllFeedUpdateResultAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IList<Subscription> entities = _subscriptionRegistrationRepository.ReadAllItems();
        foreach (Subscription entity in entities.Where(x => x.IsEnabled).OrderBy(x => x.SortIndex))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return await RefreshFeedUpdateResultAsync(entity, cancellationToken);
        }
    }


    public async ValueTask<SubscriptionFeedUpdateResult> RefreshFeedUpdateResultAsync(Subscription entity, CancellationToken cancellationToken = default)
    {
        SubscriptionUpdate update = _subscriptionUpdateRespository.GetOrAdd(entity.SubscriptionId);
        if (!IsExpiredFeedResultUpdatedTime(update.LastUpdatedAt))
        {
            // 前回更新から時間経っていない場合はスキップする
            Debug.WriteLine("[FeedUpdate] update skip: " + entity.Label);
            return new SubscriptionFeedUpdateResult() { IsSuccessed = false };
        }

        DateTime now = DateTime.Now;
        SubscriptionFeedUpdateResult result = await Task.Run(async () =>
        {
            Debug.WriteLine("[FeedUpdate] start: " + entity.Label);

            // オンラインソースから情報を取得して
            SubscriptionFeedUpdateResult result = await GetFeedResultAsync(entity);

            if (result.IsSuccessed is false)
            {
                return result;
            }

            update.LastUpdatedAt = now;
            _subscriptionUpdateRespository.UpdateItem(update);

            cancellationToken.ThrowIfCancellationRequested();

            List<SubscFeedVideo> newVideos = _subscFeedVideoRepository.RegisteringVideosIfNotExist(entity.SubscriptionId, now, result.Videos).ToList();
            var newVideoIds = newVideos.Select(x => x.VideoId).ToHashSet();
            result.NewVideos = update.LastUpdatedAt != DateTime.MinValue 
                ? result.Videos.Where(x => newVideoIds.Contains(x.VideoId)).ToList()
                : new List<NicoVideo>()
                ;

            foreach (var newVideo in newVideos)
            {
                _ = _messenger.Send(new NewSubscFeedVideoMessage(newVideo));
            }

            return result;

        }, cancellationToken);

        // 更新を通知する
        Debug.WriteLine("[FeedUpdate] complete: " + entity.Label);

        return result;
    }


    public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(Subscription source, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideos(source.SubscriptionId, skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetAllSubscFeedVideos(int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideos(skip, limit);
    }

    private IEnumerable<Subscription> GetSubscriptionGroupSubscriptions(SubscriptionGroup? group = null)
    {
        return (group != null
            ? _subscriptionRegistrationRepository.Find(x => x.Group!.GroupId == group.GroupId)
            : _subscriptionRegistrationRepository.Find(x => x.Group == null)
            );
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(SubscriptionGroup? group, int skip = 0, int limit = int.MaxValue)
    {
        return GetSubscriptionGroupSubscriptions(group)
            .SelectMany(subsc => _subscFeedVideoRepository.GetVideos(subsc.SubscriptionId))
            .Distinct(SubscFeedVideoEqualityComparer.Default)
            .OrderByDescending(x => x.PostAt)
            .Skip(skip)
            .Take(limit)
            ;
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosRaw(SubscriptionGroup? group)
    {
        return GetSubscriptionGroupSubscriptions(group)
            .SelectMany(subsc => _subscFeedVideoRepository.GetVideos(subsc.SubscriptionId))
            .Distinct(SubscFeedVideoEqualityComparer.Default)
            ;
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosForMarkAsChecked(DateTime targetPostAt)
    {
        return _subscFeedVideoRepository.GetVideosForMarkAsChecked(targetPostAt);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosForMarkAsChecked(SubscriptionGroup? group, DateTime targetPostAt)
    {
        var subscIds = GetSubscriptionGroupSubscriptions(group).Select(x => x.SubscriptionId);
        return _subscFeedVideoRepository.GetVideosForMarkAsChecked(subscIds, targetPostAt);
    }

    public void UpdateFeedVideos(IEnumerable<SubscFeedVideo> videos)
    {
        _subscFeedVideoRepository.UpdateVideos(videos);
        foreach (SubscFeedVideo video in videos)
        {
            _ = _messenger.Send(new SubscFeedVideoValueChangedMessage(video));
        }
    }

    private async Task<SubscriptionFeedUpdateResult> GetFeedResultAsync(Subscription entity)
    {
        try
        {
            List<NicoVideo> videos = entity.SourceType switch
            {
                SubscriptionSourceType.Mylist => await GetMylistFeedResult(entity.SourceParameter, _mylistProvider),
                SubscriptionSourceType.User => await GetUserVideosFeedResult(entity.SourceParameter, _userProvider),
                SubscriptionSourceType.Channel => await GetChannelVideosFeedResult(entity.SourceParameter, _channelProvider, _nicoVideoProvider),
                SubscriptionSourceType.Series => await GetSeriesVideosFeedResult(entity.SourceParameter, _seriesRepository),
                SubscriptionSourceType.SearchWithKeyword => await GetKeywordSearchFeedResult(entity.SourceParameter, _niconicoSession.ToolkitContext.Search),
                SubscriptionSourceType.SearchWithTag => await GetTagSearchFeedResult(entity.SourceParameter, _niconicoSession.ToolkitContext.Search),
                _ => throw new NotSupportedException(entity.SourceType.ToString())
            };

            _subscFeedVideoRepository.RegisteringVideosIfNotExist(entity.SubscriptionId, DateTime.Now, videos);

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

    private async Task<List<NicoVideo>> GetKeywordSearchFeedResult(string keyword, SearchClient searchProvider)
    {
        List<NicoVideo> items = new();
        int page = 0;
        const int itemGetCountPerPage = 50;

        int head = page * itemGetCountPerPage;
        var res = await searchProvider.Video.VideoSearchAsync(keyword, isTagSearch: false, sortKey: NiconicoToolkit.Search.Video.SortKey.RegisteredAt, sortOrder: NiconicoToolkit.Search.Video.SortOrder.Desc);

        Guard.IsTrue(res.IsSuccess);

        if (res.Data.TotalCount == 0)
        {
            return items;
        }

        foreach (NvapiVideoItem item in res.Data.Items)
        {
            NicoVideo video = _nicoVideoProvider.UpdateCache(item.Id, item);
            items.Add(video);
        }

        return items;
    }

    private async Task<List<NicoVideo>> GetTagSearchFeedResult(string tag, SearchClient searchProvider)
    {
        List<NicoVideo> items = new();
        int page = 0;
        const int itemGetCountPerPage = 50;

        int head = page * itemGetCountPerPage;
        var res = await searchProvider.Video.VideoSearchAsync(tag, isTagSearch: true, sortKey: NiconicoToolkit.Search.Video.SortKey.RegisteredAt, sortOrder: NiconicoToolkit.Search.Video.SortOrder.Desc);

        Guard.IsTrue(res.IsSuccess);

        if (res.Data.TotalCount == 0)
        {
            return items;
        }

        foreach (NvapiVideoItem item in res.Data.Items)
        {
            NicoVideo video = _nicoVideoProvider.UpdateCache(item.Id, item);
            items.Add(video);
        }

        return items;
    }
}

public sealed class SubscriptionUpdate
{
    [BsonCtor]
    public SubscriptionUpdate(SusbcriptionId subscriptionSourceId, DateTime lastUpdatedAt)
    {
        SubscriptionSourceId = subscriptionSourceId;
        LastUpdatedAt = lastUpdatedAt;        
    }

    public SubscriptionUpdate(SusbcriptionId subscriptionSourceId)
    {
        SubscriptionSourceId = subscriptionSourceId;
    }

    [BsonId]
    public SusbcriptionId SubscriptionSourceId { get; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.MinValue;

}


public sealed class SubscriptionUpdateRespository : LiteDBServiceBase<SubscriptionUpdate>
{
    public SubscriptionUpdateRespository(LiteDatabase liteDatabase) : base(liteDatabase)
    {
    }

    new BsonValue CreateItem(SubscriptionUpdate item)
    {
        throw new NotSupportedException();
    }

    internal SubscriptionUpdate GetOrAdd(SusbcriptionId id)
    {
        if (_collection.FindById(id.AsPrimitive()) is not null and var update)
        {
            return update;
        }

        var newUpdate = new SubscriptionUpdate(id);
        _collection.Insert(newUpdate);
        return newUpdate;
    }
}



public sealed class SubscriptionGroupChecked
{
    [BsonCtor]
    public SubscriptionGroupChecked(SubscriptionGroupId subscriptionGroupId, DateTime lastCheckedAt)
    {
        SubscriptionGroupId = subscriptionGroupId;
        LastCheckedAt = lastCheckedAt;
    }

    public SubscriptionGroupChecked(SubscriptionGroupId subscriptionSourceId)
    {
        SubscriptionGroupId = subscriptionSourceId;
    }

    [BsonId]
    public SubscriptionGroupId SubscriptionGroupId { get; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.MinValue;

    public DateTime LastCheckedAt { get; set; } = DateTime.MinValue;

}


public sealed class SubscriptionGroupCheckedRespository : LiteDBServiceBase<SubscriptionGroupChecked>
{
    public SubscriptionGroupCheckedRespository(LiteDatabase liteDatabase) : base(liteDatabase)
    {
    }

    new BsonValue CreateItem(SubscriptionGroupChecked item)
    {
        throw new NotSupportedException();
    }

    internal SubscriptionGroupChecked GetOrAdd(SubscriptionGroupId groupId)
    {
        if (!(_collection.FindById(groupId.AsPrimitive()) is not null and var entity))
        {
            entity = new SubscriptionGroupChecked(groupId);
            _collection.Insert(entity);
        }

        return entity;
    }
}
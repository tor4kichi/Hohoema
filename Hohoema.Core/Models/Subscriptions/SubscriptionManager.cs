#nullable enable
using AngleSharp.Dom;
using AngleSharp.Media;
using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Infra;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Search;
using Hohoema.Models.Niconico.User;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Niconico.Video.Series;
using Hohoema.Models.Playlist;
using ImTools;
using LiteDB;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.Search;
using NiconicoToolkit.User;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Subscriptions;

public class SubscriptionFeedUpdateResult
{
    public SubscriptionFeedUpdateResult(Subscription subscription, List<NicoVideo> videos, List<NicoVideo> newVideos, DateTime updateAt)
    {
        IsSuccessed = true;
        Subscription = subscription;
        Videos = videos;
        NewVideos = newVideos;
        UpdateAt = updateAt;
    }

    public SubscriptionFeedUpdateResult(Subscription subscription, SubscriptionFeedUpdateFailedReason failedReason, DateTime updateAt)
    {
        IsSuccessed = false;
        Subscription = subscription;
        FailedReason = failedReason;
        UpdateAt = updateAt;
    }

    public bool IsSuccessed { get; init; }
    public SubscriptionFeedUpdateFailedReason? FailedReason { get; init; }
    public Subscription Subscription { get; init; }
    public List<NicoVideo>? Videos { get; init; }
    public List<NicoVideo>? NewVideos { get; init; }
    public DateTime UpdateAt { get; init; }
}

public enum SubscriptionFeedUpdateFailedReason
{
    Unknown,

    IsAutoUpdateDisabled,
    IsAutoUpdateDisabledWithGroup,
    SourceCanNotAccess,
    Interval,
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
        ILocalizeService localizeService,
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

        DefaultSubscriptionGroup = new SubscriptionGroup(SubscriptionGroupId.DefaultGroupId, localizeService.Translate("SubscGroup_DefaultGroupName")) { Order = -1 };
    }

    public List<SubscriptionGroup> GetSubscriptionGroups(bool withDefaultGroup = false)
    {
        var list = _subscriptionGroupRepository.ReadAllItems();
        if (withDefaultGroup)
        {
            list.Insert(0, DefaultSubscriptionGroup);
        }

        return list;
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
        if (group.GroupId == SubscriptionGroupId.DefaultGroupId)
        {
            return false;
        }

        var sources = _subscriptionRegistrationRepository.Find(x => x.Group!.GroupId == group.GroupId).ToArray();
        foreach (var source in sources)
        {
            MoveSubscriptionGroup(source, DefaultSubscriptionGroup);
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
        ClearEnabledSubscriptionsCount();
        return newEntity;
    }

    public void UpdateSubscription(Subscription entity)
    {
        _ = _subscriptionRegistrationRepository.UpdateItem(entity);
        _messenger.Send(new SubscriptionUpdatedMessage(entity));
        ClearEnabledSubscriptionsCount();
    }

    public void MoveSubscriptionGroup(Subscription entity, SubscriptionGroup moveDestinationGroup)
    {
        var lastGroupId = entity.Group?.GroupId ?? SubscriptionGroupId.DefaultGroupId;
        if (moveDestinationGroup.IsDefaultGroup)
        {
            entity.Group = null;
        }
        else
        {
            entity.Group = moveDestinationGroup;
        }

        _ = _subscriptionRegistrationRepository.UpdateItem(entity);
        _messenger.Send(new SubscriptionGroupMovedMessage(entity) {LastGroupId = lastGroupId, CurrentGroupId = moveDestinationGroup.GroupId });
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

        ClearEnabledSubscriptionsCount();
    }

    public IList<Subscription> GetSubscriptions()
    {
        return _subscriptionRegistrationRepository.ReadAllItems();
    }

    public IList<Subscription> GetSubscriptions(SubscriptionGroupId groupId)
    {
        if (groupId == SubscriptionGroupId.DefaultGroupId)
        {
            return _subscriptionRegistrationRepository.Find(x => x.Group == null).OrderBy(x => x.SortIndex).ToList();
        }
        else
        {
            return _subscriptionRegistrationRepository.Find(groupId).ToList();
        }
    }

    public Subscription GetSubscription(SubscriptionId id)
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

    public SubscriptionGroup DefaultSubscriptionGroup { get; }
    public string AllSubscriptouGroupId { get; } = string.Empty;

    public SubscriptionGroup? GetSubscriptionGroup(SubscriptionGroupId subscriptionGroupId)
    {
        return _subscriptionGroupRepository.FindById(subscriptionGroupId);
    }

    /// <summary>
    /// 購読グループを取得。全部を指す場合は null を返す。
    /// </summary>
    /// <param name="subscriptionGroupId"></param>
    /// <returns></returns>
    public SubscriptionGroup? GetSubscriptionGroup(string subscriptionGroupId)
    {
        return subscriptionGroupId != AllSubscriptouGroupId
            ? (subscriptionGroupId != SubscriptionGroupId.DefaultGroupId.ToString()
                ? _subscriptionGroupRepository.FindById(SubscriptionGroupId.Parse(subscriptionGroupId))
                : DefaultSubscriptionGroup
                )
            : null
            ;
    }

    public int GetSubscriptionGroupVideosCount(SubscriptionGroupId? groupId) 
    {
        return GetSubscriptionGroupSubscriptions(groupId)
            .Sum(_subscFeedVideoRepository.GetVideoCount);
    }
   
    public DateTime GetLastUpdatedAt(SubscriptionId subscriptionId)
    {
        return _subscriptionUpdateRespository.GetOrAdd(subscriptionId).LastUpdatedAt;
    }

    public void SetUpdatedAt(SubscriptionId subscriptionId, DateTime? updatedAt = null)
    {
        updatedAt ??= DateTime.Now;
        var update = _subscriptionUpdateRespository.GetOrAdd(subscriptionId);
        update.LastUpdatedAt = updatedAt.Value;
        _subscriptionUpdateRespository.UpdateItem(update);
    }

    public SubscriptionUpdate GetSubscriptionProps(SubscriptionId subscriptionId)
    {
        return _subscriptionUpdateRespository.GetOrAdd(subscriptionId);
    }

    public void SetUpdatedAt(SubscriptionUpdate update)
    {
        _subscriptionUpdateRespository.UpdateItem(update);
    }


    public DateTime GetLastCheckedAt(SubscriptionGroupId subscriptionGroupId)
    {
        return _subscriptionGroupCheckedRespository.GetOrAdd(subscriptionGroupId).LastCheckedAt;
    }

    public void SetCheckedAt(SubscriptionGroupId subscriptionGroupId, DateTime? checkedAt = null)
    {
        checkedAt ??= DateTime.Now;
        var entity = _subscriptionGroupCheckedRespository.GetOrAdd(subscriptionGroupId);

        entity.LastCheckedAt = checkedAt.Value;
        _subscriptionGroupCheckedRespository.UpdateItem(entity);
        _messenger.Send(new SubscriptionGroupCheckedAtChangedMessage(subscriptionGroupId, checkedAt.Value));
    }

    public SubscriptionGroupProps GetSubscriptionGroupProps(SubscriptionGroupId subscriptionGroupId)
    {
        return _subscriptionGroupCheckedRespository.GetOrAdd(subscriptionGroupId);
    }

    public void SetSubcriptionGroupProps(SubscriptionGroupProps subscriptionGroupProps)
    {
        _subscriptionGroupCheckedRespository.UpdateItem(subscriptionGroupProps);
    }


    public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(Subscription source, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideos(source.SubscriptionId, skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetAllSubscFeedVideos(int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideos(skip, limit);
    }

    private IEnumerable<Subscription> GetSubscriptionGroupSubscriptions(SubscriptionGroupId? groupId = null)
    {
        if (groupId == null)
        {
            // 全件取得
            return _subscriptionRegistrationRepository.Find(Query.All());
        }
        else if (groupId.Value == SubscriptionGroupId.DefaultGroupId)
        {
            // デフォルトはIdを null として扱っている
            return _subscriptionRegistrationRepository.Find(x => x.Group == null);
        }
        else
        {
            return _subscriptionRegistrationRepository.Find(x => x.Group!.GroupId == groupId.Value);
        }        
    }

    public DateTime GetLatestPostAt(SubscriptionGroupId? groupId)
    {
        return GetSubscriptionGroupSubscriptions(groupId)
            .Select(subsc => GetLatestPostAt(subsc.SubscriptionId))
            .Max();
            
    }

    public DateTime GetLatestPostAt(SubscriptionId subscriptionId)
    {
        return _subscFeedVideoRepository.GetLatestTimeOnSubscVideo(subscriptionId);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideos(SubscriptionGroupId? groupId, int skip = 0, int limit = int.MaxValue)
    {
        return GetSubscriptionGroupSubscriptions(groupId)
            .SelectMany(subsc => _subscFeedVideoRepository.GetVideos(subsc.SubscriptionId))
            .Distinct(SubscFeedVideoEqualityComparer.Default)
            .OrderByDescending(x => x.PostAt)
            .Skip(skip)
            .Take(limit)
            ;
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosRaw(SubscriptionGroupId? groupId)
    {
        return GetSubscriptionGroupSubscriptions(groupId)
            .SelectMany(subsc => _subscFeedVideoRepository.GetVideos(subsc.SubscriptionId))
            .Distinct(SubscFeedVideoEqualityComparer.Default)
            ;
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosOlderAt(DateTime targetPostAt, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideosOlderAt(targetPostAt, skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosOlderAt(SubscriptionId subscriptionId, DateTime? targetPostAt = null, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideosOlderAt(subscriptionId, targetPostAt ?? GetLastUpdatedAt(subscriptionId), skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosOlderAt(SubscriptionGroupId? groupId, DateTime targetPostAt, int skip = 0, int limit = int.MaxValue)
    {
        var subscIds = GetSubscriptionGroupSubscriptions(groupId).Select(x => x.SubscriptionId);
        return _subscFeedVideoRepository.GetVideosOlderAt(subscIds, targetPostAt, skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosNewerAt(DateTime targetPostAt, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideosNewerAt(targetPostAt, skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosNewerAt(SubscriptionId subscriptionId, DateTime? targetPostAt = null, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideosNewerAt(subscriptionId, targetPostAt ?? GetLastUpdatedAt(subscriptionId), skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosNewerAt(SubscriptionGroupId? groupId, DateTime targetPostAt, int skip = 0, int limit = int.MaxValue)
    {
        var subscIds = GetSubscriptionGroupSubscriptions(groupId).Select(x => x.SubscriptionId);
        return _subscFeedVideoRepository.GetVideosNewerAt(subscIds, targetPostAt, skip, limit);
    }

    public int GetFeedVideosCount(SubscriptionId subscriptionId)
    {
        return _subscFeedVideoRepository.GetVideoCount(subscriptionId);
    }

    public int GetFeedVideosCount(SubscriptionGroupId subscriptionGroupId)
    {
        return GetSubscriptions(subscriptionGroupId).Sum(x => GetFeedVideosCount(x.SubscriptionId));
    }

    public int GetFeedVideosCountWithNewer(SubscriptionId subscriptionId, DateTime? dateTime = null)
    {
        return _subscFeedVideoRepository.GetVideoCountWithDateTimeNewer(subscriptionId, dateTime ?? GetLastUpdatedAt(subscriptionId));
    }

    public int GetFeedVideosCountWithNewer(SubscriptionGroupId subscriptionGroupId)
    {
        return GetSubscriptions(subscriptionGroupId).Sum(x => GetFeedVideosCountWithNewer(x.SubscriptionId, GetLastUpdatedAt(x.SubscriptionId)));
    }

    public int GetFeedVideosCountWithNewer(DateTime dateTime)
    {
        return GetSubscriptions().Sum(x => GetFeedVideosCountWithNewer(x.SubscriptionId, dateTime));
    }


    private int? _EnabledSubscriptionsCountCached;

    private int GetEnabledSubscriptionsCount()
    {
        return _EnabledSubscriptionsCountCached ??= _subscriptionRegistrationRepository.CountSafe(x => x.IsAutoUpdateEnabled);
    }

    private void ClearEnabledSubscriptionsCount()
    {
        _EnabledSubscriptionsCountCached = null;
    }


    private static readonly TimeSpan _FeedUpdateIntervalPerSubscription = TimeSpan.FromMinutes(5);

    private bool IsExpiredFeedResultUpdatedTime(DateTime lastUpdatedAt)
    {
         return GetNextUpdateTime(lastUpdatedAt) < DateTime.Now;
    }

    public DateTime GetNextUpdateTime(DateTime lastUpdatedAt)
    {
        int updateEnabledSubscriptionsCount = GetEnabledSubscriptionsCount();
        var interval = _FeedUpdateIntervalPerSubscription * Math.Max(1, updateEnabledSubscriptionsCount);
        if (interval < TimeSpan.FromHours(1))
        {
            interval = TimeSpan.FromHours(1);
        }
        return lastUpdatedAt + interval;
    }

    public IEnumerable<Subscription> GetSortedSubscriptions()
    {
        foreach (var group in GetSubscriptionGroups(withDefaultGroup: true))
        {
            foreach (Subscription subscription in GetSubscriptions(group.GroupId))
            {
                yield return subscription;
            }
        }
    }

    public SubscriptionFeedUpdateFailedReason? CheckCanUpdate(bool isManualUpdate, Subscription subscription, SubscriptionUpdate? update = null)
    {
        if (!isManualUpdate)
        {
            if (subscription.IsAutoUpdateEnabled is false)
            {
                Debug.WriteLine("[FeedUpdate] update disabled: " + subscription.Label);
                return SubscriptionFeedUpdateFailedReason.IsAutoUpdateDisabled;
            }
            else if (subscription.Group?.GroupId is not null and SubscriptionGroupId groupId
                && GetSubscriptionGroupProps(groupId).IsAutoUpdateEnabled is false
                )
            {
                Debug.WriteLine("[FeedUpdate] update disabled (with Group): " + subscription.Label);
                return SubscriptionFeedUpdateFailedReason.IsAutoUpdateDisabledWithGroup;
            }
        }

        update ??= _subscriptionUpdateRespository.GetOrAdd(subscription.SubscriptionId);
        if (!IsExpiredFeedResultUpdatedTime(update.LastUpdatedAt))
        {
            // 前回更新から時間経っていない場合はスキップする
            Debug.WriteLine("[FeedUpdate] update skip: " + subscription.Label);
            return SubscriptionFeedUpdateFailedReason.Interval;
        }

        return default;
    }

    public async ValueTask<SubscriptionFeedUpdateResult> UpdateSubscriptionFeedVideosAsync(Subscription subscription, SubscriptionUpdate? update = null, DateTime? updateDateTime = null, CancellationToken cancellationToken = default)
    {
        update ??= _subscriptionUpdateRespository.GetOrAdd(subscription.SubscriptionId);

        Debug.WriteLine("[FeedUpdate] start: " + subscription.Label);
        update.LastUpdatedAt = updateDateTime ?? DateTime.Now;
        _subscriptionUpdateRespository.UpdateItem(update);
        try
        {
            var videos = await Task.Run(
                () => GetFeedResultAsync(subscription)
                , cancellationToken
                );

            cancellationToken.ThrowIfCancellationRequested();

            List<SubscFeedVideo> newSubscVideos = _subscFeedVideoRepository.RegisteringVideosIfNotExist(
                subscription.SubscriptionId,
                update.LastUpdatedAt,
                videos
                ).ToList();
            
            foreach (var newVideo in newSubscVideos)
            {
                _messenger.Send(new NewSubscFeedVideoMessage(newVideo));
            }

            var newVideoIds = newSubscVideos.Select(x => x.VideoId).ToHashSet();
            var newVideos = update.LastUpdatedAt != DateTime.MinValue
                ? videos.Where(x => newVideoIds.Contains(x.VideoId)).ToList()
                : new List<NicoVideo>()
                ;

            var result = new SubscriptionFeedUpdateResult(subscription, videos, newVideos, update.LastUpdatedAt);
            _messenger.Send(new SubscriptionFeedUpdatedMessage(result));
            return result;
        }
        catch (Exception ex)
        {
            var result = new SubscriptionFeedUpdateResult(subscription, SubscriptionFeedUpdateFailedReason.SourceCanNotAccess, update.LastUpdatedAt);
            _messenger.Send(new SubscriptionFeedUpdatedMessage(result));
            return result;
        }
    }


    public void UpdateFeedVideos(IEnumerable<SubscFeedVideo> videos)
    {
        _subscFeedVideoRepository.UpdateVideos(videos);
        foreach (SubscFeedVideo video in videos)
        {
            _ = _messenger.Send(new SubscFeedVideoValueChangedMessage(video));
        }
    }

    private async Task<List<NicoVideo>> GetFeedResultAsync(Subscription entity)
    {
        var videos = entity.SourceType switch
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
        
        return videos;
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

    public bool IsContainSubscriptionGroup(SubscriptionId sourceSubscId, SubscriptionGroupId groupId)
    {
        return _subscriptionRegistrationRepository.FindById(sourceSubscId).Group?.GroupId == groupId;
    }
}

public sealed class SubscriptionUpdate
{
    [BsonCtor]
    public SubscriptionUpdate(SubscriptionId subscriptionSourceId, DateTime lastUpdatedAt)
    {
        SubscriptionSourceId = subscriptionSourceId;
        LastUpdatedAt = lastUpdatedAt;        
    }

    public SubscriptionUpdate(SubscriptionId subscriptionSourceId)
    {
        SubscriptionSourceId = subscriptionSourceId;
    }

    [BsonId]
    public SubscriptionId SubscriptionSourceId { get; }

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

    internal SubscriptionUpdate GetOrAdd(SubscriptionId id)
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



public sealed class SubscriptionGroupProps
{
    [BsonCtor]
    public SubscriptionGroupProps(SubscriptionGroupId subscriptionGroupId, DateTime lastCheckedAt, bool isAutoUpdateEnabled, bool isAddToQueueWhenUpdated)
    {
        SubscriptionGroupId = subscriptionGroupId;
        LastCheckedAt = lastCheckedAt;
        IsAutoUpdateEnabled = isAutoUpdateEnabled;
        IsAddToQueueWhenUpdated = isAddToQueueWhenUpdated;
    }

    public SubscriptionGroupProps(SubscriptionGroupId subscriptionSourceId)
    {
        SubscriptionGroupId = subscriptionSourceId;
    }

    [BsonId]
    public SubscriptionGroupId SubscriptionGroupId { get; }

    public DateTime LastCheckedAt { get; set; } = DateTime.MinValue;

    public bool IsAutoUpdateEnabled { get; set; } = true;

    public bool IsAddToQueueWhenUpdated { get; set; } = false;
}


public sealed class SubscriptionGroupCheckedRespository : LiteDBServiceBase<SubscriptionGroupProps>
{
    private class SubscriptionGroupCheckedDefaultSettings : FlagsRepositoryBase
    {
        public DateTime LastChecked
        {
            get => Read(DateTime.MinValue);
            set => Save(value);
        }

        public bool IsAutoUpdateEnabled
        {
            get => Read(true);
            set => Save(value);
        }

        public bool IsAddToQueueWhenUpdated
        {
            get => Read(true);
            set => Save(value);
        }        
    }

    private SubscriptionGroupCheckedDefaultSettings _defaultSettings;
    public SubscriptionGroupCheckedRespository(LiteDatabase liteDatabase) 
        : base(liteDatabase)
    {
        //liteDatabase.DropCollection("SubscriptionGroupChecked");
        _defaultSettings = new SubscriptionGroupCheckedDefaultSettings();
    }

    new BsonValue CreateItem(SubscriptionGroupProps item)
    {
        throw new NotSupportedException();
    }

    private SubscriptionGroupProps GetDefaultGroup()
    {
        return new SubscriptionGroupProps(
            SubscriptionGroupId.DefaultGroupId, 
            _defaultSettings.LastChecked, 
            _defaultSettings.IsAutoUpdateEnabled, 
            _defaultSettings.IsAddToQueueWhenUpdated
            );
    }
    
    private void SetDefaultGroupLastCheckedAt(DateTime lastChecked)
    {
        _defaultSettings.LastChecked = lastChecked;        
    }

    private void SetDefaultGroupIsAutoUpdateEnabled(bool isAutoUpdateEnabled)
    {
        _defaultSettings.IsAutoUpdateEnabled = isAutoUpdateEnabled;
    }

    internal SubscriptionGroupProps GetOrAdd(SubscriptionGroupId groupId)
    {
        if (!(_collection.FindOne(x => x.SubscriptionGroupId == groupId) is not null and var entity))
        {            
            if (groupId == SubscriptionGroupId.DefaultGroupId)
            {
                entity = GetDefaultGroup();
            }
            else
            {
                entity = new SubscriptionGroupProps(groupId);
                _collection.Insert(entity);
            }
        }

        return entity;
    }

    public override bool UpdateItem(SubscriptionGroupProps item)
    {
        if (item.SubscriptionGroupId == SubscriptionGroupId.DefaultGroupId)
        {
            _defaultSettings.LastChecked = item.LastCheckedAt;
            _defaultSettings.IsAutoUpdateEnabled = item.IsAutoUpdateEnabled;
            _defaultSettings.IsAddToQueueWhenUpdated= item.IsAddToQueueWhenUpdated;
            return true;
        }
        else
        {
            return base.UpdateItem(item);
        }
    }
}
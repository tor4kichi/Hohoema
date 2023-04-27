#nullable enable
using AngleSharp.Dom;
using AngleSharp.Html;
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
    public SubscriptionFeedUpdateResult(Subscription subscription, List<NicoVideo> newVideos, DateTime updateAt)
    {
        IsSuccessed = true;
        Subscription = subscription;        
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
    private readonly SubscriptionGroupPropsRespository _subscriptionGroupCheckedRespository;

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
        SubscriptionGroupPropsRespository subscriptionGroupCheckedRespository
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

    public IEnumerable<SubscriptionGroup> GetSubscriptionGroups(bool withDefaultGroup = false)
    {        
        if (withDefaultGroup)
        {
            return Enumerable.Concat(
                new[] { DefaultSubscriptionGroup },
                _subscriptionGroupRepository.Find(Query.All(nameof(SubscriptionGroup.Order), Query.Ascending))
                );
        }
        else
        {
            return _subscriptionGroupRepository.Find(Query.All(nameof(SubscriptionGroup.Order), Query.Ascending));
        }
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
        if (group.GroupId == SubscriptionGroupId.DefaultGroupId)
        {
            return false;
        }

        var sources = _subscriptionRegistrationRepository.Find(x => x.Group!.GroupId == group.GroupId).ToArray();
        foreach (var source in sources)
        {
            MoveSubscriptionGroupAndInsertToLast(source, DefaultSubscriptionGroup);
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
        _subscriptionGroupRepository.UpdateItem(groups.Where(x => x.IsDefaultGroup is false));

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

        return AddSubscription_Internal(new Subscription(SubscriptionId.NewObjectId())
        {
            Label = owner.ScreenName,
            SourceParameter = video.ProviderId,
            SourceType = sourceType,
            Group = group
        });
    }

    public Subscription AddSubscription(IMylist mylist, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription(SubscriptionId.NewObjectId())
        {
            Label = mylist.Name,
            SourceParameter = mylist.PlaylistId.Id,
            SourceType = SubscriptionSourceType.Mylist,
            Group = group
        });
    }

    public Subscription AddKeywordSearchSubscription(string keyword, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription(SubscriptionId.NewObjectId())
        {
            Label = keyword,
            SourceParameter = keyword,
            SourceType = SubscriptionSourceType.SearchWithKeyword,
            Group = group,
        });
    }
    public Subscription AddTagSearchSubscription(string tag, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription(SubscriptionId.NewObjectId())
        {
            Label = tag,
            SourceParameter = tag,
            SourceType = SubscriptionSourceType.SearchWithTag,
            Group = group,
        });
    }


    public Subscription AddSubscription(SubscriptionSourceType sourceType, string sourceParameter, string label, SubscriptionGroup? group = null)
    {
        return AddSubscription_Internal(new Subscription(SubscriptionId.NewObjectId())
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

    public void MoveSubscriptionGroupAndInsertToLast(Subscription entity, SubscriptionGroup moveDestinationGroup)
    {
        var lastGroupId = entity.Group?.GroupId ?? SubscriptionGroupId.DefaultGroupId;
        int count;
        if (moveDestinationGroup.IsDefaultGroup)
        {
            entity.Group = null;
            count = _subscriptionRegistrationRepository.CountSafe(x => x.Group!.GroupId == null);
        }
        else
        {
            entity.Group = moveDestinationGroup;
            count = _subscriptionRegistrationRepository.CountSafe(x => x.Group!.GroupId == lastGroupId);
        }

        entity.SortIndex = count;
        _ = _subscriptionRegistrationRepository.UpdateItem(entity);
        _messenger.Send(new SubscriptionGroupMovedMessage(entity) {LastGroupId = lastGroupId, CurrentGroupId = moveDestinationGroup.GroupId });
    }

    public void MoveSubscriptionGroupAndInsert(Subscription entity, int sortIndex, SubscriptionGroup moveDestinationGroup)
    {
        // 移動先のグループに属する購読のsortIndex以降のアイテムを後ろにずらすように更新する
        if (moveDestinationGroup.IsDefaultGroup)
        {
            entity.Group = null;
            _subscriptionRegistrationRepository.UpdateMany(
                x => new Subscription(x.SubscriptionId, x.SortIndex + 1, x.Label, x.SourceType, x.SourceParameter, x.IsAutoUpdateEnabled, x.IsAddToQueueWhenUpdated, x.Group, x.IsToastNotificationEnabled),
                x => x.Group == null && sortIndex <= x.SortIndex
                ); 
        }
        else
        {
            entity.Group = moveDestinationGroup;
            SubscriptionGroupId destinationGroupId = moveDestinationGroup.GroupId;
            _subscriptionRegistrationRepository.UpdateMany(
                x => new Subscription(x.SubscriptionId, x.SortIndex + 1, x.Label, x.SourceType, x.SourceParameter, x.IsAutoUpdateEnabled, x.IsAddToQueueWhenUpdated, x.Group, x.IsToastNotificationEnabled),
                x => x.Group!.GroupId == destinationGroupId && sortIndex <= x.SortIndex
                );
        }

        entity.SortIndex = sortIndex;
        _ = _subscriptionRegistrationRepository.UpdateItem(entity);
        _messenger.Send(new SubscriptionGroupMovedMessage(entity) 
        {
            LastGroupId = entity.Group?.GroupId ?? SubscriptionGroupId.DefaultGroupId,
            CurrentGroupId = moveDestinationGroup.GroupId 
        });
    }

    public void UpdateSubscriptionGroup(SubscriptionGroup group)
    {
        if (group.IsDefaultGroup) { return; }
        _subscriptionGroupRepository.UpdateItem(group);

        _messenger.Send<SubscriptionGroupUpdatedMessage>(new(group));
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

    public IEnumerable<Subscription> GetSubscriptionsWithoutSort()
    {
        return _subscriptionRegistrationRepository.ReadAllItems();
    }

    public IEnumerable<Subscription> GetSubscriptions(SubscriptionGroupId? groupId)
    {
        if (groupId == null)
        {
            return GetSubscriptionGroups(withDefaultGroup: true)
                .SelectMany(x => GetSubscriptionGroupSubscriptions(x.GroupId).OrderBy(x => x.SortIndex))
                ;
        }
        else if (groupId == SubscriptionGroupId.DefaultGroupId)
        {
            return _subscriptionRegistrationRepository.Find(x => x.Group == null).OrderBy(x => x.SortIndex);
        }
        else
        {
            return _subscriptionRegistrationRepository.Find(groupId.Value);
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
   
    public SubscriptionUpdate GetSubscriptionProps(SubscriptionId subscriptionId)
    {
        return _subscriptionUpdateRespository.GetOrAdd(subscriptionId);
    }        

    public void SetSubscriptionProps(SubscriptionUpdate update)
    {
        _subscriptionUpdateRespository.UpdateItem(update);
    }

    public void UpdateAllSubscriptionCheckedAt(DateTime? checkedAt = null)
    {
        foreach (var group in GetSubscriptionGroups(withDefaultGroup: true))
        {
            UpdateSubscriptionCheckedAt(group.GroupId, checkedAt);
        }
    }

    public void UpdateSubscriptionCheckedAt(SubscriptionGroupId subscriptionGroupId, DateTime? checkedAt = null)
    {
        foreach (var subsc in GetSubscriptions(subscriptionGroupId))
        {
            UpdateSubscriptionCheckedAt(subsc, checkedAt);
        }
    }

    public void UpdateSubscriptionCheckedAt(Subscription subscription, DateTime? checkedAt = null)
    {
        UpdateSubscriptionCheckedAt(subscription.SubscriptionId, subscription.Group?.GroupId ?? SubscriptionGroupId.DefaultGroupId, checkedAt);
    }

    public void UpdateSubscriptionCheckedAt(SubscriptionId subscriptionId, DateTime? checkedAt = null)
    {
        var subscription = GetSubscription(subscriptionId);
        UpdateSubscriptionCheckedAt(subscription.SubscriptionId, subscription.Group?.GroupId ?? SubscriptionGroupId.DefaultGroupId, checkedAt);
    }

    private void UpdateSubscriptionCheckedAt(SubscriptionId subscriptionId, SubscriptionGroupId subscriptionGroupId, DateTime? checkedAt)
    {
        var props = GetSubscriptionProps(subscriptionId);
        props.LastCheckedAt = (checkedAt ?? GetLatestPostAt(subscriptionId)) + TimeSpan.FromSeconds(1);
        SetSubscriptionProps(props);
        _messenger.Send(new SubscriptionCheckedAtChangedMessage(props, subscriptionGroupId));
    }

    public DateTime GetSubscriptionCheckedAt(SubscriptionId subscriptionId)
    {
        return GetSubscriptionProps(subscriptionId).LastCheckedAt;
    }

    public SubscriptionGroupProps GetSubscriptionGroupProps(SubscriptionGroupId subscriptionGroupId)
    {
        return _subscriptionGroupCheckedRespository.GetOrAdd(subscriptionGroupId);
    }

    public void SetSubcriptionGroupProps(SubscriptionGroupProps subscriptionGroupProps)
    {
        _subscriptionGroupCheckedRespository.UpdateItem(subscriptionGroupProps);
        _messenger.Send(new SubscriptionGroupPropsChangedMessage(subscriptionGroupProps));
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

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosOlderAt(Subscription subscription, DateTime? targetPostAt = null, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideosOlderAt(subscription.SubscriptionId, targetPostAt ?? GetSubscriptionCheckedAt(subscription.SubscriptionId), skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosOlderAt(SubscriptionGroupId? groupId, DateTime? targetPostAt = null, int skip = 0, int limit = int.MaxValue)
    {
        var subscIds = GetSubscriptionGroupSubscriptions(groupId).Select(x => x.SubscriptionId);
        return subscIds.SelectMany(x => _subscFeedVideoRepository.GetVideosOlderAt(x, targetPostAt ?? GetSubscriptionCheckedAt(x), skip, limit));
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosNewerAt(DateTime targetPostAt, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideosNewerAt(targetPostAt, skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosNewerAt(Subscription subscription, DateTime? targetPostAt = null, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.GetVideosNewerAt(subscription.SubscriptionId, targetPostAt ?? GetSubscriptionCheckedAt(subscription.SubscriptionId), skip, limit);
    }

    public IEnumerable<SubscFeedVideo> GetSubscFeedVideosNewerAt(SubscriptionGroupId? groupId, DateTime? targetPostAt = null, int skip = 0, int limit = int.MaxValue)
    {
        var subscIds = GetSubscriptionGroupSubscriptions(groupId).OrderBy(x => x.SortIndex).Select(x => x.SubscriptionId);
        return subscIds.SelectMany(x => _subscFeedVideoRepository.GetVideosNewerAt(x, targetPostAt ?? GetSubscriptionCheckedAt(x), skip, limit));
    }

    public int GetFeedVideosCount(SubscriptionId subscriptionId)
    {
        return _subscFeedVideoRepository.GetVideoCount(subscriptionId);
    }

    public int GetFeedVideosCount(SubscriptionGroupId subscriptionGroupId)
    {
        return GetSubscriptions(subscriptionGroupId).Sum(x => GetFeedVideosCount(x.SubscriptionId));
    }

    public int GetFeedVideosCountWithNewer(Subscription subscription)
    {
        return _subscFeedVideoRepository.GetVideoCountWithDateTimeNewer(subscription.SubscriptionId, GetSubscriptionCheckedAt(subscription.SubscriptionId));
    }

    public int GetFeedVideosCountWithNewer(SubscriptionGroupId subscriptionGroupId)
    {        
        return GetSubscriptions(subscriptionGroupId).Sum(GetFeedVideosCountWithNewer);
    }

    public int GetFeedVideosCountWithNewer()
    {
        return GetSubscriptionsWithoutSort().Sum(GetFeedVideosCountWithNewer);
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

    private bool IsReachedNextUpdateTime(DateTime lastUpdatedAt)
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

    public SubscriptionFeedUpdateFailedReason? CheckCanUpdate(bool isManualUpdate, Subscription subscription, ref SubscriptionUpdate? update)
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

        return default;
    }

    public async ValueTask<SubscriptionFeedUpdateResult> UpdateSubscriptionFeedVideosAsync(Subscription subscription, SubscriptionUpdate? update = null, DateTime? updateDateTime = null, CancellationToken cancellationToken = default)
    {
        update ??= GetSubscriptionProps(subscription.SubscriptionId);

        Debug.WriteLine("[FeedUpdate] start: " + subscription.Label);
        DateTime currentUpdatedAt = updateDateTime ?? DateTime.Now;
        
        try
        {
            List<NicoVideo> latestVideos = await Task.Run(
                () => GetFeedResultAsync(subscription)
                , cancellationToken
                );

            cancellationToken.ThrowIfCancellationRequested();

            var latestPostAt = GetLatestPostAt(subscription.SubscriptionId);

            var (newSubscVideos, newVideos) = _subscFeedVideoRepository.RegisteringVideosIfNotExist(
                        subscId: subscription.SubscriptionId,
                        updateAt: DateTime.Now,
                        lastCheckedAt: latestPostAt,
                        videos: latestVideos
                        );

            if (update.UpdateCount == 0)
            {
                UpdateSubscriptionCheckedAt(subscription, newVideos.Any() ? newVideos.Max(x => x.PostedAt) : null);

                newVideos.Clear();
                newSubscVideos.Clear();                
            }

            update.UpdateCount++;
            SetSubscriptionProps(update);

            foreach (var newVideo in newSubscVideos)
            {
                _messenger.Send(new NewSubscFeedVideoMessage(newVideo));
            }

            var result = new SubscriptionFeedUpdateResult(subscription, newVideos, currentUpdatedAt);
            _messenger.Send(new SubscriptionFeedUpdatedMessage(result));
            return result;
        }
        catch (Exception ex)
        {
            var result = new SubscriptionFeedUpdateResult(subscription, SubscriptionFeedUpdateFailedReason.SourceCanNotAccess, currentUpdatedAt);
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
        return entity.SourceType switch
        {
            SubscriptionSourceType.Mylist => await GetMylistFeedResult(entity.SourceParameter, _mylistProvider),
            SubscriptionSourceType.User => await GetUserVideosFeedResult(entity.SourceParameter, _userProvider),
            SubscriptionSourceType.Channel => await GetChannelVideosFeedResult(entity.SourceParameter, _channelProvider, _nicoVideoProvider),
            SubscriptionSourceType.Series => await GetSeriesVideosFeedResult(entity.SourceParameter, _seriesRepository),
            SubscriptionSourceType.SearchWithKeyword => await GetKeywordSearchFeedResult(entity.SourceParameter, _niconicoSession.ToolkitContext.Search),
            SubscriptionSourceType.SearchWithTag => await GetTagSearchFeedResult(entity.SourceParameter, _niconicoSession.ToolkitContext.Search),
            _ => throw new NotSupportedException(entity.SourceType.ToString())
        };        
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

    internal void SetSubscriptionCheckedAt(SubscriptionGroupId groupId, IVideoContent video)
    {
        if (video is SubscFeedVideo feedVideo)
        {
            UpdateSubscriptionCheckedAt(feedVideo.SourceSubscId, groupId, video.PostedAt);
        }
        else
        {
            // グループ内購読全てに対してVideoIdを全検索 
            foreach (var subsc in GetSubscriptions(groupId))
            {
                if (_subscFeedVideoRepository.IsVideoExists(subsc.SubscriptionId, video.VideoId))
                {
                    UpdateSubscriptionCheckedAt(subsc.SubscriptionId, groupId, video.PostedAt);
                    break;
                }
            }
        }
    }
}

public sealed class SubscriptionUpdate
{
    [BsonCtor]
    public SubscriptionUpdate(SubscriptionId subscriptionSourceId, DateTime lastCheckedAt, int updateCount)
    {
        SubscriptionSourceId = subscriptionSourceId;
        LastCheckedAt = lastCheckedAt;
        UpdateCount = updateCount;
    }

    public SubscriptionUpdate(SubscriptionId subscriptionSourceId)
    {
        SubscriptionSourceId = subscriptionSourceId;
    }

    [BsonId]
    public SubscriptionId SubscriptionSourceId { get; }

    public DateTime LastCheckedAt { get; set; } = DateTime.MinValue;

    public int UpdateCount { get; set; } = 0;
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
        try
        {
            if (_collection.FindById(id.AsPrimitive()) is not null and var update)
            {
                return update;
            }

            var newUpdate = new SubscriptionUpdate(id) { LastCheckedAt = DateTime.Now };
            _collection.Insert(newUpdate);
            return newUpdate;
        }
        catch
        {
            var newUpdate = new SubscriptionUpdate(id) { LastCheckedAt = DateTime.Now };
            _collection.Upsert(newUpdate);
            return newUpdate;
        }
    }
}



public sealed class SubscriptionGroupProps
{
    [BsonCtor]
    public SubscriptionGroupProps(
        SubscriptionGroupId subscriptionGroupId, 
        bool isAutoUpdateEnabled, 
        bool isAddToQueueWhenUpdated, 
        bool isToastNotificationEnabled, 
        bool isInAppLiteNotificationEnabled,
        bool isShowInAppMenu
        )
    {
        SubscriptionGroupId = subscriptionGroupId;
        IsAutoUpdateEnabled = isAutoUpdateEnabled;
        IsAddToQueueWhenUpdated = isAddToQueueWhenUpdated;
        IsToastNotificationEnabled = isToastNotificationEnabled;
        IsInAppLiteNotificationEnabled = isInAppLiteNotificationEnabled;
        IsShowInAppMenu = isShowInAppMenu;
    }

    public SubscriptionGroupProps(SubscriptionGroupId subscriptionSourceId)
    {
        SubscriptionGroupId = subscriptionSourceId;
    }

    [BsonId]
    public SubscriptionGroupId SubscriptionGroupId { get; }

    public bool IsAutoUpdateEnabled { get; set; } = true;

    public bool IsAddToQueueWhenUpdated { get; set; } = false;

    public bool IsToastNotificationEnabled { get; set; } = true;

    public bool IsInAppLiteNotificationEnabled { get; set; } = true;

    public bool IsShowInAppMenu { get; set; } = true;
}


public sealed class SubscriptionGroupPropsRespository : LiteDBServiceBase<SubscriptionGroupProps>
{
    private class SubscriptionGroupPropsForDefault : FlagsRepositoryBase
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

        public bool IsToastNotificationEnabled
        {
            get => Read(true);
            set => Save(value);
        }

        public bool IsInAppLiteNotificationEnabled
        {
            get => Read(true);
            set => Save(value);
        }

        public bool IsShowInAppMenu
        {
            get => Read(true);
            set => Save(value);
        }
    }

    private SubscriptionGroupPropsForDefault _defaultSettings;
    public SubscriptionGroupPropsRespository(LiteDatabase liteDatabase) 
        : base(liteDatabase)
    {
        //liteDatabase.DropCollection("SubscriptionGroupChecked");
        _defaultSettings = new SubscriptionGroupPropsForDefault();
    }

    new BsonValue CreateItem(SubscriptionGroupProps item)
    {
        throw new NotSupportedException();
    }

    private SubscriptionGroupProps GetDefaultGroup()
    {
        return new SubscriptionGroupProps(
            subscriptionGroupId: SubscriptionGroupId.DefaultGroupId,
            isAutoUpdateEnabled: _defaultSettings.IsAutoUpdateEnabled,
            isAddToQueueWhenUpdated: _defaultSettings.IsAddToQueueWhenUpdated,
            isToastNotificationEnabled: _defaultSettings.IsToastNotificationEnabled,
            isInAppLiteNotificationEnabled: _defaultSettings.IsInAppLiteNotificationEnabled,
            isShowInAppMenu: _defaultSettings.IsShowInAppMenu
            );
    }
    
    internal SubscriptionGroupProps GetOrAdd(SubscriptionGroupId groupId)
    {
        if (groupId == SubscriptionGroupId.DefaultGroupId)
        {
            return GetDefaultGroup();
        }

        try
        {
            if (!(_collection.FindOne(x => x.SubscriptionGroupId == groupId) is not null and var entity))
            {
                entity = new SubscriptionGroupProps(groupId);
                _collection.Insert(entity);
            }

            return entity;
        }
        catch
        {
            var entity = new SubscriptionGroupProps(groupId);
            _collection.Upsert(entity);
            return entity;
        }
    }

    public override bool UpdateItem(SubscriptionGroupProps item)
    {
        if (item.SubscriptionGroupId == SubscriptionGroupId.DefaultGroupId)
        {
            _defaultSettings.IsAutoUpdateEnabled = item.IsAutoUpdateEnabled;
            _defaultSettings.IsAddToQueueWhenUpdated= item.IsAddToQueueWhenUpdated;
            _defaultSettings.IsToastNotificationEnabled = item.IsToastNotificationEnabled;
            _defaultSettings.IsInAppLiteNotificationEnabled = item.IsInAppLiteNotificationEnabled;
            _defaultSettings.IsShowInAppMenu = item.IsShowInAppMenu;
            return true;
        }
        else
        {
            return base.UpdateItem(item);
        }
    }
}
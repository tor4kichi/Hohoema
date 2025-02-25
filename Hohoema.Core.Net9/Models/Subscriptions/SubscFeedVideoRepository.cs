﻿#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using LiteDB;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Subscriptions;

public sealed class SubscFeedVideo : IVideoContent
{
    [BsonId(autoId: true)]
    public ObjectId Id { get; init; }
    public DateTime FeedUpdateAt { get; set; }

    public SubscriptionId SourceSubscId { get; init; }
    public string VideoId { get; init; }
    public DateTime PostAt { get; init; }
    public string Title { get; init; }
    public string? ThumbnailUrl { get; init; }
    public TimeSpan Legnth { get; init; }

    [BsonIgnore]
    VideoId IVideoContent.VideoId => VideoId;

    [BsonIgnore]
    TimeSpan IVideoContent.Length => Legnth;

    [BsonIgnore]
    string IVideoContent.ThumbnailUrl => ThumbnailUrl;

    [BsonIgnore]
    DateTime IVideoContent.PostedAt => PostAt;

    bool IEquatable<IVideoContent>.Equals(IVideoContent other)
    {
        return this.VideoId == other.VideoId;
    }
}

public sealed class SubscFeedVideoEqualityComparer : IEqualityComparer<SubscFeedVideo>
{
    public static readonly SubscFeedVideoEqualityComparer Default = new SubscFeedVideoEqualityComparer();
    public bool Equals(SubscFeedVideo x, SubscFeedVideo y)
    {
        return string.Equals(x.VideoId, y.VideoId, StringComparison.Ordinal);
    }

    public int GetHashCode(SubscFeedVideo obj)
    {
        return obj.VideoId.GetHashCode();
    }
}


public sealed class SubscFeedVideoRepository
{
    static SubscFeedVideoRepository()
    {        
        BsonMapper.Global.RegisterType(x => x.AsPrimitive(), x => new SubscriptionId(x.AsObjectId));
    }

    private class SubscFeedVideoRepository_Internal : LiteDBServiceBase<SubscFeedVideo>
    {
        public SubscFeedVideoRepository_Internal(LiteDatabase liteDatabase) : base(liteDatabase)
        {
            _collection.EnsureIndex(x => x.SourceSubscId);
            _collection.EnsureIndex(x => x.PostAt);
            _collection.EnsureIndex(x => x.VideoId);
            _collection.EnsureIndex(x => x.FeedUpdateAt);
        }

        public DateTime GetLatestPostAt(SubscriptionId subscId)
        {
            try
            {
                return _collection.Find(x => x.SourceSubscId == subscId).Max(x => x.PostAt);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        public int GetVideoCount(SubscriptionId subscId)
        {
            try
            {
                return CountSafe(x => x.SourceSubscId == subscId);
            }
            catch 
            {
                return 0;
            }
        }

        internal bool DeleteItem(SubscriptionId subscriptionId)
        {
            return base.DeleteItem(subscriptionId.AsPrimitive());
        }
    }

    private readonly SubscFeedVideoRepository_Internal _subscFeedVideoRepository;

    public SubscFeedVideoRepository(LiteDatabase liteDatabase)
    {
        _subscFeedVideoRepository = new SubscFeedVideoRepository_Internal(liteDatabase);
    }


    public bool DeleteSubsc(Subscription source)
    {
        return _subscFeedVideoRepository.DeleteItem(source.SubscriptionId);
    }

    public IEnumerable<SubscFeedVideo> GetVideos(SubscriptionId subscId, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(Query.All()).OrderByDescending(x => x.PostAt).Where(x => x.SourceSubscId == subscId).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideos(IEnumerable<SubscriptionId> subscIds, int skip = 0, int limit = int.MaxValue)
    {
        HashSet<SubscriptionId> idHashSet = subscIds.ToHashSet();
        return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending)).Where(x => idHashSet.Contains(x.SourceSubscId)).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideos(int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending)).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosOlderAt(DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(x => x.PostAt <= targetTime).Where(x => true).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosOlderAt(SubscriptionId subscriptionId, DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {        
        return _subscFeedVideoRepository.Find(x => x.PostAt <= targetTime && x.SourceSubscId == subscriptionId).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosOlderAt(IEnumerable<SubscriptionId> subscIds, DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        HashSet<SubscriptionId> idHashSet = subscIds.ToHashSet();
        return _subscFeedVideoRepository.Find(x => x.PostAt <= targetTime).Where(x => idHashSet.Contains(x.SourceSubscId)).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosNewerAt(DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(x => targetTime < x.PostAt).Where(x => true).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosNewerAt(SubscriptionId subscriptionId, DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(x => targetTime < x.PostAt && x.SourceSubscId == subscriptionId).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosNewerAt(IEnumerable<SubscriptionId> subscIds, DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        HashSet<SubscriptionId> idHashSet = subscIds.ToHashSet();
        return _subscFeedVideoRepository.Find(x => targetTime < x.PostAt).Where(x => idHashSet.Contains(x.SourceSubscId)).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public DateTime GetLatestTimeOnSubscVideo(SubscriptionId subscId)
    {
        return _subscFeedVideoRepository.GetLatestPostAt(subscId);
    }

    public void UpdateVideos(IEnumerable<SubscFeedVideo> videos)
    {
        _ = _subscFeedVideoRepository.UpdateItem(videos);            
    }

    public (List<SubscFeedVideo> newSubscVideos, List<NicoVideo> newVideos) RegisteringVideosIfNotExist(SubscriptionId subscId, DateTime updateAt, DateTime lastCheckedAt, IEnumerable<NicoVideo> videos)
    {
        var newVideos = videos.Where(x => x.PostedAt > lastCheckedAt).ToList();
        var newSubscItems = newVideos
            .Select(x => new SubscFeedVideo()
            {
                SourceSubscId = subscId,
                Id = ObjectId.NewObjectId(),
                VideoId = x.VideoId,
                PostAt = x.PostedAt,
                Title = x.Title,
                FeedUpdateAt = updateAt,
                ThumbnailUrl = x.ThumbnailUrl,
                Legnth = x.Length
            }
            ).ToList();
        _subscFeedVideoRepository.InsertBulk(newSubscItems);
        return (newSubscItems, newVideos);
    }

    internal int GetVideoCount(SubscriptionId subscriptionId)
    {
        return _subscFeedVideoRepository.GetVideoCount(subscriptionId);
    }


    internal int GetVideoCount(Subscription subsc)
    {
        return _subscFeedVideoRepository.GetVideoCount(subsc.SubscriptionId);
    }

    internal int GetVideoCountWithDateTimeNewer(SubscriptionId subscriptionId, DateTime targetDateTime)
    {
        return _subscFeedVideoRepository.CountSafe(x => x.SourceSubscId == subscriptionId && x.PostAt > targetDateTime);
    }

    internal bool IsVideoExists(SubscriptionId subscriptionId, string videoId)
    {
        return _subscFeedVideoRepository.Exists(x => x.SourceSubscId == subscriptionId && x.VideoId == videoId);
    }
}

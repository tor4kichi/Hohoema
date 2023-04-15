#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Contracts.Subscriptions;
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Subscriptions;

public sealed class SubscFeedVideo
{
    [BsonId(autoId: true)]
    public ObjectId Id { get; init; }

    public SusbcriptionId SourceSubscId { get; init; }
    public string VideoId { get; init; }
    public DateTime PostAt { get; init; }
    public string Title { get; init; }

    public DateTime FeedUpdateAt { get; set; }
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
        BsonMapper.Global.RegisterType(x => x.AsPrimitive(), x => new SusbcriptionId(x.AsObjectId));
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

        public DateTime GetLatestPostAt(SusbcriptionId subscId)
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

        public int GetVideoCount(SusbcriptionId subscId)
        {
            try
            {
                return _collection.Count(x => x.SourceSubscId == subscId);
            }
            catch 
            {
                return 0;
            }
        }

        internal bool DeleteItem(SusbcriptionId subscriptionId)
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

    public IEnumerable<SubscFeedVideo> GetVideos(SusbcriptionId subscId, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending)).Where(x => x.SourceSubscId == subscId).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideos(IEnumerable<SusbcriptionId> subscIds, int skip = 0, int limit = int.MaxValue)
    {
        HashSet<SusbcriptionId> idHashSet = subscIds.ToHashSet();
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

    public IEnumerable<SubscFeedVideo> GetVideosOlderAt(IEnumerable<SusbcriptionId> subscIds, DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        HashSet<SusbcriptionId> idHashSet = subscIds.ToHashSet();
        return _subscFeedVideoRepository.Find(x => x.PostAt <= targetTime).Where(x => idHashSet.Contains(x.SourceSubscId)).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosNewerAt(DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(x => targetTime < x.PostAt).Where(x => true).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosNewerAt(IEnumerable<SusbcriptionId> subscIds, DateTime targetTime, int skip = 0, int limit = int.MaxValue)
    {
        HashSet<SusbcriptionId> idHashSet = subscIds.ToHashSet();
        return _subscFeedVideoRepository.Find(x => targetTime < x.PostAt).Where(x => idHashSet.Contains(x.SourceSubscId)).OrderByDescending(x => x.PostAt).Skip(skip).Take(limit);
    }

    public DateTime GetLatestTimeOnSubscVideo(SusbcriptionId subscId)
    {
        return _subscFeedVideoRepository.GetLatestPostAt(subscId);
    }

    public void UpdateVideos(IEnumerable<SubscFeedVideo> videos)
    {
        _ = _subscFeedVideoRepository.UpdateItem(videos);
        
    }

    public IEnumerable<SubscFeedVideo> RegisteringVideosIfNotExist(SusbcriptionId subscId, DateTime updateAt, IEnumerable<NicoVideo> videos)
    {
        foreach (NicoVideo video in videos)
        {
            string videoId = video.VideoId;
            if (_subscFeedVideoRepository.Exists(x => x.SourceSubscId == subscId && x.VideoId == videoId) is false)
            {
                SubscFeedVideo feed = new()
                {
                    SourceSubscId = subscId,
                    Id = ObjectId.NewObjectId(),
                    VideoId = video.VideoId,
                    PostAt = video.PostedAt,
                    Title = video.Title,
                    FeedUpdateAt = updateAt,
                };

                _ = _subscFeedVideoRepository.CreateItem(feed);

                yield return feed;
            }
        }
    }

    internal int GetVideoCount(Subscription subsc)
    {
        return _subscFeedVideoRepository.GetVideoCount(subsc.SubscriptionId);
    }
}

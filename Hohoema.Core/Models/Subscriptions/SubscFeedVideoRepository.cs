#nullable enable
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
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

    public ObjectId SourceSubscId { get; init; }
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
    private class SubscFeedVideoRepository_Internal : LiteDBServiceBase<SubscFeedVideo>
    {
        public SubscFeedVideoRepository_Internal(LiteDatabase liteDatabase) : base(liteDatabase)
        {
            _ = _collection.EnsureIndex(x => x.SourceSubscId);
            _ = _collection.EnsureIndex(x => x.PostAt);
            _ = _collection.EnsureIndex(x => x.VideoId);
            _ = _collection.EnsureIndex(x => x.FeedUpdateAt);
        }

        public DateTime GetLatestPostAt(ObjectId subscId)
        {
            return _collection.Find(x => x.SourceSubscId == subscId).Max(x => x.PostAt);
        }

        public int GetVideoCount(ObjectId subscId)
        {
            return _collection.Count(x => x.SourceSubscId == subscId);
        }
    }

    private readonly SubscFeedVideoRepository_Internal _subscFeedVideoRepository;

    public SubscFeedVideoRepository(LiteDatabase liteDatabase)
    {
        _subscFeedVideoRepository = new SubscFeedVideoRepository_Internal(liteDatabase);
    }


    public bool DeleteSubsc(Subscription source)
    {
        return _subscFeedVideoRepository.DeleteItem(source.Id);
    }

    public IEnumerable<SubscFeedVideo> GetVideos(ObjectId subscId, int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending)).Where(x => x.SourceSubscId == subscId).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideos(IEnumerable<ObjectId> subscIds, int skip = 0, int limit = int.MaxValue)
    {
        HashSet<ObjectId> idHashSet = subscIds.ToHashSet();
        return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending)).Where(x => idHashSet.Contains(x.SourceSubscId)).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideos(int skip = 0, int limit = int.MaxValue)
    {
        return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending)).Skip(skip).Take(limit);
    }

    public IEnumerable<SubscFeedVideo> GetVideosForMarkAsChecked(DateTime targetDateTime)
    {
        return _subscFeedVideoRepository.Find(x => x.PostAt < targetDateTime).OrderByDescending(x => x.PostAt);
    }

    public IEnumerable<SubscFeedVideo> GetVideosForMarkAsChecked(IEnumerable<ObjectId> subscIds, DateTime targetDateTime)
    {
        HashSet<ObjectId> idHashSet = subscIds.ToHashSet();
        return _subscFeedVideoRepository.Find(x => x.PostAt < targetDateTime).Where(x => idHashSet.Contains(x.SourceSubscId)).OrderByDescending(x => x.PostAt);
    }

    public DateTime GetLatestTimeOnSubscVideo(ObjectId subscId)
    {
        return _subscFeedVideoRepository.GetLatestPostAt(subscId);
    }

    public void UpdateVideos(IEnumerable<SubscFeedVideo> videos)
    {
        _ = _subscFeedVideoRepository.UpdateItem(videos);
        
    }

    public IEnumerable<SubscFeedVideo> RegisteringVideosIfNotExist(ObjectId subscId, DateTime updateAt, IEnumerable<NicoVideo> videos)
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
        return _subscFeedVideoRepository.GetVideoCount(subsc.Id);
    }
}

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


public sealed class SubscFeedVideoValueChangedMessage : ValueChangedMessage<SubscFeedVideo>
{
    public SubscFeedVideoValueChangedMessage(SubscFeedVideo value) : base(value)
    {

    }
}

public sealed class NewSubscFeedVideoMessage : ValueChangedMessage<SubscFeedVideo>
{
    public NewSubscFeedVideoMessage(SubscFeedVideo value) : base(value)
    {

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
    }

    private readonly SubscFeedVideoRepository_Internal _subscFeedVideoRepository;
    private readonly IMessenger _messenger;

    public SubscFeedVideoRepository(LiteDatabase liteDatabase,
        IMessenger messenger
        )
    {
        _subscFeedVideoRepository = new SubscFeedVideoRepository_Internal(liteDatabase);
        _messenger = messenger;
    }


    public bool DeleteSubsc(SubscriptionSourceEntity source)
    {
        return _subscFeedVideoRepository.DeleteMany(x => x.SourceSubscId == source.Id);
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
        return _subscFeedVideoRepository.Find(Query.All(nameof(SubscFeedVideo.PostAt), Query.Descending), skip, limit);
    }

    public DateTime GetLatestTimeOnSubscVideo(ObjectId subscId)
    {
        return _subscFeedVideoRepository.GetLatestPostAt(subscId);
    }

    public void UpdateVideos(IEnumerable<SubscFeedVideo> videos)
    {
        _ = _subscFeedVideoRepository.UpdateItem(videos);
        foreach (SubscFeedVideo video in videos)
        {
            _ = _messenger.Send(new SubscFeedVideoValueChangedMessage(video));
        }
    }

    public IEnumerable<NicoVideo> RegisteringVideosIfNotExist(ObjectId subscId, DateTime updateAt, IEnumerable<NicoVideo> videos)
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
                _ = _messenger.Send(new NewSubscFeedVideoMessage(feed));

                yield return video;
            }
        }
    }

    public IEnumerable<SubscFeedVideo> RegisteringVideosIfNotExist(ObjectId subscId, DateTime updateAt, IEnumerable<SubscFeedVideo> videos)
    {
        foreach (SubscFeedVideo video in videos)
        {
            string videoId = video.VideoId;
            if (_subscFeedVideoRepository.Exists(x => x.SourceSubscId == subscId && x.VideoId == videoId) is false)
            {
                video.FeedUpdateAt = updateAt;
                _ = _subscFeedVideoRepository.CreateItem(video);

                _ = _messenger.Send(new NewSubscFeedVideoMessage(video));
                yield return video;
            }
        }
    }
}

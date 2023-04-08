#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Subscriptions;

public sealed class SubscriptionFeedResultRepository : LiteDBServiceBase<SubscriptionFeedResult>
{
    public static int FeedResultVideosCapacity = 100;

    public SubscriptionFeedResultRepository(LiteDatabase database)
        : base(database)
    {
        _ = _collection.EnsureIndex(x => x.SourceType);
        _ = _collection.EnsureIndex(x => x.SourceParamater);
    }


    public void ClearAll()
    {
        _ = _collection.DeleteAll();
    }

    public SubscriptionFeedResult GetFeedResult(SubscriptionSourceEntity source)
    {
        return _collection.FindOne(x => x.SourceType == source.SourceType && x.SourceParamater == source.SourceParameter);
    }


    public SubscriptionFeedResult MargeFeedResult(SubscriptionFeedResult target, SubscriptionSourceEntity source, IList<NicoVideo> videos)
    {
        SubscriptionFeedResult result = target ?? _collection.FindOne(x => x.SourceType == source.SourceType && x.SourceParamater == source.SourceParameter);
        if (result == null)
        {
            result = new SubscriptionFeedResult()
            {
                Id = ObjectId.NewObjectId(),
                SourceType = source.SourceType,
                SourceParamater = source.SourceParameter,
                Videos = videos.Select(ToFeedResultVideoItem).ToList(),
                LastUpdatedAt = DateTime.Now
            };

            _ = _collection.Insert(result);

            return result;
        }
        else
        {
            // 前回更新分までのIdsと新規分のIdの差集合を取る
            HashSet<string> ids = result.Videos.Select(x => x.VideoId).ToHashSet();
            IEnumerable<NicoVideo> exceptVideos = videos.Where(x => false == ids.Contains(x.Id));
            result.Videos = Enumerable.Concat(exceptVideos.Select(ToFeedResultVideoItem), result.Videos).Take(FeedResultVideosCapacity).ToList();
            result.LastUpdatedAt = DateTime.Now;

            _ = _collection.Update(result);

            return result;
        }
    }

    private static FeedResultVideoItem ToFeedResultVideoItem(IVideoContent x)
    {
        return new FeedResultVideoItem() { VideoId = x.VideoId, Title = x.Title, PostAt = x.PostedAt };
    }

    public bool DeleteItem(SubscriptionSourceEntity source)
    {
        SubscriptionFeedResult result = GetFeedResult(source);
        return result != null && base.DeleteItem(result.Id);
    }
}

using LiteDB;

using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Subscriptions;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Subscriptions
{
    public sealed class SubscriptionFeedResultRepository : LiteDBServiceBase<SubscriptionFeedResult>
    {
        public static int FeedResultVideosCapacity = 100;

        public SubscriptionFeedResultRepository(LiteDatabase database)
            : base(database)
        {
            _collection.EnsureIndex(x => x.SourceType);
            _collection.EnsureIndex(x => x.SourceParamater);
        }


        public void ClearAll()
        {
            _collection.DeleteAll();
        }

        public SubscriptionFeedResult GetFeedResult(SubscriptionSourceEntity source)
        {
            return _collection.FindOne(x => x.SourceType == source.SourceType && x.SourceParamater == source.SourceParameter);
        }


        public SubscriptionFeedResult MargeFeedResult(SubscriptionFeedResult target, SubscriptionSourceEntity source, IList<NicoVideo> videos)
        {
            var result = target ?? _collection.FindOne(x => x.SourceType == source.SourceType && x.SourceParamater == source.SourceParameter);
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

                _collection.Insert(result);

                return result;
            }
            else
            {
                // 前回更新分までのIdsと新規分のIdの差集合を取る
                var ids = result.Videos.Select(x => x.VideoId).ToHashSet();
                var exceptVideos = videos.Where(x => false == ids.Contains(x.Id));
                result.Videos = Enumerable.Concat(exceptVideos.Select(ToFeedResultVideoItem), result.Videos).Take(FeedResultVideosCapacity).ToList();
                result.LastUpdatedAt = DateTime.Now;

                _collection.Update(result);

                return result;
            }
        }


        static FeedResultVideoItem ToFeedResultVideoItem(IVideoContent x) => new FeedResultVideoItem() { VideoId = x.VideoId, Title = x.Title, PostAt = x.PostedAt };



        public bool DeleteItem(SubscriptionSourceEntity source)
        {
            var result = GetFeedResult(source);
            if (result != null)
            {
                return DeleteItem(result.Id);
            }
            else
            {
                return false;
            }
        }
    }
}

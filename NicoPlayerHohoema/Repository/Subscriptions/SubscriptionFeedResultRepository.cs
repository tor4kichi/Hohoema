using LiteDB;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Repository.Subscriptions
{
    public sealed class SubscriptionFeedResultRepository : LocalLiteDBService<SubscriptionFeedResult>
    {
        public static int FeedResultVideosCapacity = 100;

        public SubscriptionFeedResultRepository()
        {
            _collection.EnsureIndex(x => x.SourceType);
            _collection.EnsureIndex(x => x.SourceParamater);
        }


        public void ClearAll()
        {
            _collection.Delete(Query.All());
        }


        public SubscriptionFeedResult GetFeedResult(SubscriptionSourceEntity source)
        {
            return _collection.FindOne(x => x.SourceType == source.SourceType && x.SourceParamater == source.SourceParameter);
        }


        public SubscriptionFeedResult MargeFeedResult(SubscriptionFeedResult target, SubscriptionSourceEntity source, IList<Database.NicoVideo> videos)
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


        static FeedResultVideoItem ToFeedResultVideoItem(IVideoContent x) => new FeedResultVideoItem() { VideoId = x.Id, Title = x.Label, PostAt = x.PostedAt };



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

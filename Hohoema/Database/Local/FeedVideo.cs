using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Database
{
    public sealed class FeedVideo
    {
        [BsonId(autoId:true)]
        public int Id { get; set; }

        [BsonField]
        public int FeedId { get; set; }
        
        [BsonField]
        public string VideoId { get; set; }
    }


    public sealed class FeedVideoDb
    {
        public static IList<FeedVideo> Get(int feedId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            return db.Query<FeedVideo>()
                .Where(x => x.FeedId == feedId)
                .ToList();
        }

        public static void Upsert(int feedId, IEnumerable<string> videoIdList)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            var alreadyVidoes = db.Query<FeedVideo>()
                .Where(x => x.FeedId == feedId)
                .ToList();

            foreach (var videoid in videoIdList)
            {
                if (null != alreadyVidoes.SingleOrDefault(x => x.VideoId == videoid))
                {
                    db.Upsert<FeedVideo>(new FeedVideo() { FeedId = feedId, VideoId = videoid });
                }
            }
        }

        public static bool Remove(int feedId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            var count = db.Delete<FeedVideo>(x => x.FeedId == feedId);
            return count != 0;
        }
    }
}

using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Database
{
    public static class VideoPlayedHistoryDb
    {
        public static VideoPlayHistory VideoPlayed(string videoId)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                var history = db.SingleOrDefault<VideoPlayHistory>(x => x.VideoId == videoId);
                if (history != null)
                {
                    history.PlayCount++;
                }
                else
                {
                    history = new VideoPlayHistory
                    {
                        VideoId = videoId,
                        PlayCount = 1,
                        LastPlayed = DateTime.Now
                    };
                }

                db.Upsert(history);

                return history;
            }
        }

        public static VideoPlayHistory Get(string videoId)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.SingleOrDefault<VideoPlayHistory>(x => x.VideoId == videoId);
            }            
        }


        public static bool IsVideoPlayed(string videoId)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.SingleOrDefault<VideoPlayHistory>(x => x.VideoId == videoId)?.PlayCount > 0;
            }
        }

        public static void ClearAllHistories()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                db.DeleteMany<VideoPlayHistory>(x => true);
            }
        }
    }

    public class VideoPlayHistory
    {
        [BsonId]
        public string VideoId { get; set; }

        public uint PlayCount { get; set; } = 0;

        public DateTime LastPlayed { get; set; }
    }
}

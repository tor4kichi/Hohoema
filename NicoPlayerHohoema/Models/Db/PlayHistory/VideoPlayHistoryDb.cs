using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Db
{
	public static class VideoPlayHistoryDb
	{
        public static void VideoPlayed(string rawVideoId)
        {
            using (var db = new PlayHistoryDbContext())
            {
                VideoPlayHistory videoHistory = null;
                try
                {
                    videoHistory = db.VideoPlayHistory.SingleOrDefault(x => x.RawVideoId == rawVideoId);
                }
                catch { }

                if (videoHistory == null)
                {
                    videoHistory = new VideoPlayHistory()
                    {
                        RawVideoId = rawVideoId,
                        PlayCount = 1,
                        LastPlayed = DateTime.Now
                    };

                    db.VideoPlayHistory.Add(videoHistory);
                }
                else
                {
                    videoHistory.PlayCount++;
                    videoHistory.LastPlayed = DateTime.Now;
                    db.VideoPlayHistory.Update(videoHistory);
                }

                db.SaveChanges();
            }
        }

        public static VideoPlayHistory Get(string rawVideoId)
        {
            using (var db = new PlayHistoryDbContext())
            {
                VideoPlayHistory history = null;
                try
                {
                    history = db.VideoPlayHistory.SingleOrDefault(x => x.RawVideoId == rawVideoId);
                } catch { }
                
                return new VideoPlayHistory()
                {
                    LastPlayed = history?.LastPlayed ?? DateTime.MinValue,
                    PlayCount = history?.PlayCount ?? 0,
                    RawVideoId = rawVideoId
                };
            }
        }

        public static bool RemoveHistory(string rawVideoId)
        {
            bool removeSuccess = false;

            using (var db = new PlayHistoryDbContext())
            {
                VideoPlayHistory videoHistory = null;
                try
                {
                    videoHistory = db.VideoPlayHistory.SingleOrDefault(x => x.RawVideoId == rawVideoId);
                }
                catch { }

                if (videoHistory != null)
                {
                    db.VideoPlayHistory.Remove(videoHistory);
                    db.SaveChanges();
                    removeSuccess = true;
                }
            }

            return removeSuccess;
        }
    }
}

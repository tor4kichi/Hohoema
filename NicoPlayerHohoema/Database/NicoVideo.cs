using LiteDB;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database
{
    public class NicoVideo
    {
        [BsonId]
        public string RawVideoId { get; set; }
        public string VideoId { get; set; }

        public string ThreadId { get; set; }

        public string Title { get; set; }
        public string ThumbnailUrl { get; set; }

        public TimeSpan Length { get; set; }
        public DateTime PostedAt { get; set; }

        public int ViewCount { get; set; }
        public int MylistCount { get; set; }
        public int CommentCount { get; set; }

        [BsonRef]
        public NicoVideoOwner Owner { get; set; }

        /// <summary>
        /// [[deprecated]] ce.api経由では動画フォーマットが取得できない
        /// </summary>
        public MovieType MovieType { get; set; }
        public List<NicoVideoTag> Tags { get; set; }

        public string DescriptionWithHtml { get; set; }

        public DateTime LastUpdated { get; set; }

        public bool IsDeleted { get; set; }
        public PrivateReasonType PrivateReasonType { get; set; }
    }



    public static class NicoVideoDb
    {
        public static NicoVideo Get(string videoId)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            {
                return db
                    .Query<NicoVideo>()
                    .Include(x => x.Owner)
                    .Where(x => x.RawVideoId == videoId)
                    .SingleOrDefault()
                    ?? new NicoVideo() { RawVideoId = videoId };
                
            }
        }

        public static bool AddOrUpdate(NicoVideo video)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            {
                video.LastUpdated = DateTime.Now;
                return db.Upsert(video);
            }
        }

        public static IEnumerable<NicoVideo> SearchFromTitle(string keyword)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            {
                return db
                    .Query<NicoVideo>()
                    .Where(Query.Contains(nameof(NicoVideo.Title), keyword))
                    .ToList();
            }
        }

        public static int DeleteAll()
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            {
                return db.Delete<NicoVideo>(Query.All());
            }
        }

        public static bool Delete(NicoVideo video)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            {
                return db.Delete<NicoVideo>(new BsonValue(video.RawVideoId));
            }
            
        }

        public static int Delete(Expression<Func<NicoVideo, bool>> expression)
        {
            var db = HohoemaLiteDb.GetLiteRepository();
            {
                return db.Delete<NicoVideo>(expression);
            }
        }
    }


    public class NicoVideoTag
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsCategory { get; set; }
        public bool IsLocked { get; set; }
        public bool IsDictionaryExists { get; set; }
    }
}

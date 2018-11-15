using LiteDB;
using Mntone.Nico2.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Database
{
    public class NicoLive 
    {
        [BsonId]
        public string LiveId { get; set; }

        public DateTime LastUpdated { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        
        public int TsReservedCount { get; set; }
        public int CommentCount { get; set; }
        public int ViewCount { get; set; }
        public string PictureUrl { get; set; }
        public string ThumbnailUrl { get; set; }

        public CommunityType ProviderType { get; set; }
        public string BroadcasterId { get; set; }
        public bool IsMemberOnly { get; set; }

        public string BroadcasterName { get; set; }
        public string BroadcasterIconUrl { get; set; }

        public bool TimeshiftEnabled { get; set; }

        public int TsViewLimitNum { get; set; }
        public int TimeshiftLimit { get; set; }

        public DateTimeOffset EndTime { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset OpenTime { get; set; }

        public bool TsIsEndless { get; set; }
        public string UseTsarchive { get; set; }
        public DateTimeOffset? TsArchiveEndTime { get; set; }
        public DateTimeOffset? TsArchiveStartTime { get; set; }
        public DateTimeOffset? TsArchiveReleasedTime { get; set; }

        public IList<string> CategoryTags { get; set; }
        public IList<string> LockedTags { get; set; }
        public IList<string> FreeTags { get; set; }
    }


    public static class NicoLiveDb
    {
        public static NicoLive Get(string liveId)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db
                    .Query<NicoLive>()
                    .Where(x => x.LiveId == liveId)
                    .SingleOrDefault()
                    ?? null;

            }
        }

        public static bool AddOrUpdate(NicoLive liveData)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                liveData.LastUpdated = DateTime.Now;
                return db.Upsert(liveData);
            }
        }

        public static IEnumerable<NicoLive> SearchFromTitle(string keyword)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db
                    .Query<NicoLive>()
                    .Where(Query.Contains(nameof(NicoLive.Title), keyword))
                    .ToList();
            }
        }

        public static int DeleteAll()
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.Delete<NicoLive>(Query.All());
            }
        }

        public static bool Delete(NicoLive liveData)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.Delete<NicoLive>(new BsonValue(liveData.LiveId));
            }

        }

        public static int Delete(Expression<Func<NicoLive, bool>> expression)
        {
            var db = HohoemaLiteDb.GetTempLiteRepository();
            {
                return db.Delete<NicoLive>(expression);
            }
        }
    }
}

using Hohoema.Models.Infrastructure;
using LiteDB;
using NiconicoToolkit.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Live
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

        public ProviderType ProviderType { get; set; }
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

    public class NicoLiveCacheRepository : LiteDBServiceBase<NicoLive>
    {
        public NicoLiveCacheRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
            _collection.EnsureIndex(x => x.Title);
        }

        public NicoLive Get(string liveId)
        {
            return _collection.FindById(liveId); ;
        }

        public bool AddOrUpdate(NicoLive liveData)
        {
            liveData.LastUpdated = DateTime.Now;
            return _collection.Upsert(liveData);
        }

        public List<NicoLive> SearchFromTitle(string keyword)
        {
            return _collection.Find(x => x.Title.Contains(keyword)).ToList();
        }

        public int Delete(Expression<Func<NicoLive, bool>> expression)
        {
            return _collection.DeleteMany(expression);
        }
    }
}

using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public sealed class NicoVideoCacheRepository : LiteDBServiceBase<NicoVideo>
    {
        public NicoVideoCacheRepository(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public NicoVideo Get(string videoId)
        {
            return _collection
                .Include(x => x.Owner)
                .Find(x => x.RawVideoId == videoId)
                .SingleOrDefault()
                ?? new NicoVideo() { RawVideoId = videoId };

        }

        public List<NicoVideo> Get(IEnumerable<string> videoIds)
        {
            return videoIds.Select(id => _collection.FindById(id) ?? new NicoVideo() { RawVideoId = id }).ToList();
        }

        public bool AddOrUpdate(NicoVideo video)
        {
            video.LastUpdated = DateTime.Now;
            return _collection.Upsert(video);
        }

        public List<NicoVideo> SearchFromTitle(string keyword)
        {
            return _collection.Find(x => x.Title.Contains(keyword))
                    .ToList();
        }


        public int Delete(Expression<Func<NicoVideo, bool>> expression)
        {
            return _collection.DeleteMany(expression);
        }
    }
}

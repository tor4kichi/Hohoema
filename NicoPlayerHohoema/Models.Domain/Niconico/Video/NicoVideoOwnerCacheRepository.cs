using Hohoema.Models.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public sealed class NicoVideoOwnerCacheRepository : LiteDBServiceBase<NicoVideoOwner>
    {
        public NicoVideoOwnerCacheRepository(LiteDB.ILiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public NicoVideoOwner Get(string ownerId)
        {
            return _collection.FindById(ownerId);
        }

        public List<NicoVideoOwner> SearchFromTitle(string name)
        {
            return _collection.Find(x => x.ScreenName.Contains(name)).ToList();
        }
    }
}

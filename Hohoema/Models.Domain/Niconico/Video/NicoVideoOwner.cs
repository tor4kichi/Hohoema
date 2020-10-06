using LiteDB;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public enum NicoVideoUserType
    {
        User,
        Channel,
        Hidden,
    }

    public class NicoVideoOwner
    {
        [BsonId]
        public string OwnerId { get; set; }

        public string IconUrl { get; set; }

        public NicoVideoUserType UserType { get; set; }

        public string ScreenName { get; set; }
    }


    public sealed class NicoVideoOwnerRepository : LiteDBServiceBase<NicoVideoOwner>
    {
        public NicoVideoOwnerRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public NicoVideoOwner Get(string id)
        {
            return _collection.FindById(id);
        }

        public bool AddOrUpdate(NicoVideoOwner owner)
        {
            return _collection.Upsert(owner);
        }

        public List<NicoVideoOwner> SearchFromScreenName(string keyword)
        {
            return _collection.Find(x => x.ScreenName.Contains(keyword)).ToList();
        }

    }
}

using LiteDB;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.Channel;
using NiconicoToolkit.Video;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public class NicoVideoOwner : IUser, IChannel
    {
        [BsonId]
        public string OwnerId { get; set; }

        public string IconUrl { get; set; }

        public OwnerType UserType { get; set; }

        public string ScreenName { get; set; }

        string IUser.IconUrl => this.IconUrl;

        string INiconicoObject.Id => this.OwnerId;

        string INiconicoObject.Label => this.ScreenName;
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

using Hohoema.Models.Infrastructure;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Channel
{
    public sealed class NicoChannelInfo
    {
        /// <summary>
        /// ex) ch1234568 
        /// </summary>
        [BsonId]
        public string RawId { get; set; }

        public string Id { get; set; }

        public string Name{ get; set; }

        public string ThumbnailUrl { get; set; }

        public List<Mntone.Nico2.Channels.Video.ChannelVideoInfo> Videos { get; set; } = new List<Mntone.Nico2.Channels.Video.ChannelVideoInfo>();

        public DateTime LastUpdate { get; set; }
    }

    public class NicoChannelCacheRepository : LiteDBServiceBase<NicoChannelInfo>
    {
        public NicoChannelCacheRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
            _collection.EnsureIndex(x => x.Id);
        }

        public NicoChannelInfo GetFromRawId(string rawId)
        {
            return _collection
                .Include(x => x.Videos)
                .FindById(rawId);
        }

        public NicoChannelInfo GetFromId(string id)
        {
            return _collection
                .Include(x => x.Videos)
                .FindOne(x => x.Id == id);
        }


        public bool AddOrUpdate(NicoChannelInfo info)
        {
            info.LastUpdate = DateTime.Now;

            return _collection.Upsert(info);
        }
    }
}

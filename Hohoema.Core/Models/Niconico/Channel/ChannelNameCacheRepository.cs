using Hohoema.Infra;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Channel
{
    public sealed class ChannelNameCacheRepository : LiteDBServiceBase<ChannelEntity>
    {
        public ChannelNameCacheRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }
    }

    public class ChannelEntity
    {
        [BsonId]
        public string ChannelId { get; set; }

        public string ScreenName { get; set; }


    }
}

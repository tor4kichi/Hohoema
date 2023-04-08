using Hohoema.Infra;
using LiteDB;

namespace Hohoema.Models.Niconico.Channel;

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

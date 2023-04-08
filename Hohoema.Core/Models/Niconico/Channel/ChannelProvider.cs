using Hohoema.Infra;
using NiconicoToolkit.Channels;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Channel;

public sealed class ChannelProvider : ProviderBase
{
    private readonly ChannelNameCacheRepository _channelNameCacheRepository;

    public ChannelProvider(
        NiconicoSession niconicoSession,
        ChannelNameCacheRepository channelNameCacheRepository
        ) : base(niconicoSession)
    {
        _channelNameCacheRepository = channelNameCacheRepository;
    }

    public async ValueTask<string> GetChannelNameWithCacheAsync(ChannelId channelId)
    {
        string strChannelId = channelId.ToString();
        ChannelEntity cached = _channelNameCacheRepository.FindById(strChannelId);
        if (cached == null)
        {
            ChannelInfo info = await GetChannelInfo(channelId);
            cached = new ChannelEntity() { ChannelId = strChannelId, ScreenName = info.ScreenName };
            _ = _channelNameCacheRepository.UpdateItem(cached);
        }

        return cached.ScreenName;
    }


    public Task<ChannelVideoResponse> GetChannelVideo(string channelIdOrScreenName, int page, ChannelVideoSortKey? sortKey = null, ChannelVideoSortOrder? sortOrder = null)
    {
        return _niconicoSession.ToolkitContext.Channel.GetChannelVideoAsync(channelIdOrScreenName, page, sortKey, sortOrder);
    }

    public Task<ChannelInfo> GetChannelInfo(ChannelId channelId)
    {
        return _niconicoSession.ToolkitContext.Channel.GetChannelInfoAsync(channelId);
    }
}

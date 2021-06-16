using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Channels;

namespace Hohoema.Models.Domain.Niconico.Channel
{
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
            var strChannelId = channelId.ToString();
            var cached = _channelNameCacheRepository.FindById(strChannelId);
            if (cached == null)
            {
                var info = await GetChannelInfo(channelId);
                cached = new ChannelEntity() { ChannelId = strChannelId, ScreenName = info.ScreenName };
                _channelNameCacheRepository.UpdateItem(cached);
            }

            return cached.ScreenName;
        }


        public Task<ChannelVideoResponse> GetChannelVideo(string channelIdOrScreenName, int page)
        {
            return _niconicoSession.ToolkitContext.Channel.GetChannelVideoAsync(channelIdOrScreenName, page);
        }

        public Task<ChannelInfo> GetChannelInfo(ChannelId channelId)
        {
            return _niconicoSession.ToolkitContext.Channel.GetChannelInfoAsync(channelId);
        }
    }
}

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

        public async ValueTask<string> GetChannelNameWithCacheAsync(string channelId)
        {
            var cached = _channelNameCacheRepository.FindById(channelId);
            if (cached == null)
            {
                var info = await GetChannelInfo(channelId);
                cached = new ChannelEntity() { ChannelId = channelId, ScreenName = info.ScreenName };
                _channelNameCacheRepository.UpdateItem(cached);
            }

            return cached.ScreenName;
        }


        public Task<ChannelVideoResponse> GetChannelVideo(string channelId, int page)
        {
            return _niconicoSession.ToolkitContext.Channel.GetChannelVideoAsync(channelId, page);
        }

        public Task<ChannelInfo> GetChannelInfo(string channelId)
        {
            return _niconicoSession.ToolkitContext.Channel.GetChannelInfoAsync(channelId);
        }
    }
}

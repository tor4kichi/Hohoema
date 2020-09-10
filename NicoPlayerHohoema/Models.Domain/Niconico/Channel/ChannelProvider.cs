using Mntone.Nico2.Channels.Video;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Channel
{
    public sealed class ChannelProvider : ProviderBase
    {
        public ChannelProvider(NiconicoSession niconicoSession) : base(niconicoSession)
        {
        }




        public async Task<ChannelVideoResponse> GetChannelVideo(string channelId, int page)
        {
            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Channel.GetChannelVideosAsync(channelId, page);
            });            
        }

        public async Task<Mntone.Nico2.Channels.Info.ChannelInfo> GetChannelInfo(string channelId)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Channel.GetChannelInfo(channelId);
            });
        }



    }
}

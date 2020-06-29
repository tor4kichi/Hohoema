using Hohoema.Models.Niconico;
using Mntone.Nico2.Channels.Video;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Channel
{
    public sealed class ChannelProvider : ProviderBase
    {
        public ChannelProvider(NiconicoSession niconicoSession) : base(niconicoSession)
        {
        }




        public async Task<ChannelVideoResponse> GetChannelVideo(string channelId, int page)
        {
            var res = await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Channel.GetChannelVideosAsync(channelId, page);
            });

            return new ChannelVideoResponse(res);
        }

        public async Task<ChannelInfo> GetChannelInfo(string channelId)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Channel.GetChannelInfo(channelId);
            });

            return new ChannelInfo(res);
        }
    }

}

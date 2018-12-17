using Mntone.Nico2.Channels.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class ChannelProvider : ProviderBase
    {
        public ChannelProvider(NiconicoSession niconicoSession) : base(niconicoSession)
        {
        }




        public async Task<ChannelVideoResponse> GetChannelVideo(string channelId, int page)
        {
            await WaitNicoPageAccess();

            return await Context.Channel.GetChannelVideosAsync(channelId, page);
        }

        public async Task<Mntone.Nico2.Channels.Info.ChannelInfo> GetChannelInfo(string channelId)
        {
            return await Context.Channel.GetChannelInfo(channelId);
        }



    }
}

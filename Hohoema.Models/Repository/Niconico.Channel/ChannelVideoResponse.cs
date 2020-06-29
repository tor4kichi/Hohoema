using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Channel
{
    public sealed class ChannelVideoResponse
    {
        private readonly Mntone.Nico2.Channels.Video.ChannelVideoResponse _res;

        public ChannelVideoResponse(Mntone.Nico2.Channels.Video.ChannelVideoResponse res)
        {
            _res = res;
        }

        public int TotalCount => _res.TotalCount;
        public int Page => _res.Page;

        List<ChannelVideoInfo> _Videos;
        public List<ChannelVideoInfo> Videos => _Videos ??= _res.Videos.Select(x => new ChannelVideoInfo(x)).ToList();
    }
}

using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowChannelIncrementalSource : FollowIncrementalSourceBase
    {
        private readonly ChannelFollowProvider _channelFollowProvider;

        public FollowChannelIncrementalSource(ChannelFollowProvider channelFollowProvider)
        {
            _channelFollowProvider = channelFollowProvider;
            MaxCount = long.MaxValue;
        }

        bool isTailReached;
        public override async Task<IEnumerable<IFollowable>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (isTailReached)
            {
                return Enumerable.Empty<IFollowable>();
            }

            uint offset = (uint)(pageIndex * pageSize);
            var res = await _channelFollowProvider.GetChannelsAsync((uint)pageSize, offset: offset);

            TotalCount = res.Meta.Total;
            
            if (offset + res.Meta.Count >= TotalCount)
            {
                isTailReached = true;
            }

            return res.Data.Select(x => new FollowChannelViewModel(x));
        }
    }
}

#nullable enable
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Follow.LoginUser;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowChannelIncrementalSource : FollowIncrementalSourceBase<IChannel>
{
    private readonly ChannelFollowProvider _channelFollowProvider;

    public FollowChannelIncrementalSource(ChannelFollowProvider channelFollowProvider)
    {
        _channelFollowProvider = channelFollowProvider;
        MaxCount = long.MaxValue;
    }

    bool isTailReached;
    public override async Task<IEnumerable<IChannel>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (isTailReached)
        {
            return Enumerable.Empty<IChannel>();
        }

        int offset = pageIndex * pageSize;
        var res = await _channelFollowProvider.GetChannelsAsync(offset: offset, pageSize);

        TotalCount = res.Meta.Total;
        
        if (offset + res.Meta.Count >= TotalCount)
        {
            isTailReached = true;
        }

        return res.Data
            .Select(x => new FollowChannelViewModel(x))
            .ToArray() // Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
            ;
    }
}

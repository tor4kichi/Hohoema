using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowCommunityIncrementalSource : FollowIncrementalSourceBase
    {
        private readonly CommunityFollowProvider _communityFollowProvider;

        public FollowCommunityIncrementalSource(CommunityFollowProvider communityFollowProvider)
        {
            _communityFollowProvider = communityFollowProvider;
        }

        bool isTailReached;
        public override async Task<IEnumerable<IFollowable>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (isTailReached)
            {
                return Enumerable.Empty<IFollowable>();
            }

            var res = await _communityFollowProvider.GetCommunityItemsAsync((uint)pageSize, (uint)pageIndex);
            TotalCount = res.Meta.Total;

            if (pageSize * pageIndex + res.Meta.Count >= TotalCount)
            {
                isTailReached = true;
            }

            return res.Data.Select(x => new FollowCommunityViewModel(x));
        }
    }
}

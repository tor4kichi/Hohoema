using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Follow;
using Hohoema.ViewModels.Niconico.Follow;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Hohoema.Models.Niconico.Community;
using NiconicoToolkit.Follow;

namespace Hohoema.ViewModels.Niconico.Follow
{
    public sealed class FollowCommunityIncrementalSource : FollowIncrementalSourceBase<ICommunity>
    {
        private readonly CommunityFollowProvider _communityFollowProvider;
        private readonly uint _loginUserId;

        public FollowCommunityIncrementalSource(CommunityFollowProvider communityFollowProvider, uint loginUserId)
        {
            _communityFollowProvider = communityFollowProvider;
            _loginUserId = loginUserId;
        }

        bool isTailReached;
        private UserOwnedCommunityResponse _ownedCommunities;
        HashSet<string> _OwnedCommunitiesIdHashSet;
        public override async Task<IEnumerable<ICommunity>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (isTailReached)
            {
                return Enumerable.Empty<ICommunity>();
            }

            if (pageIndex == 0)
            {
                if (_loginUserId != 0)
                {
                    _ownedCommunities = await _communityFollowProvider.GetUserOwnedCommunitiesAsync(_loginUserId);
                    _OwnedCommunitiesIdHashSet = _ownedCommunities.Data.OwnedCommunities.Select(x => x.Id.ToString()).ToHashSet();
                }
            }

            var res = await _communityFollowProvider.GetCommunityItemsAsync((uint)pageSize, (uint)pageIndex);
            TotalCount = res.Meta.Total;

            if (pageSize * pageIndex + res.Meta.Count >= TotalCount)
            {
                isTailReached = true;
            }

            if (pageIndex == 0)
            {
                return Enumerable.Concat<IFollowCommunity>(
                    _ownedCommunities.Data.OwnedCommunities,
                    res.Data.Where(x => !_OwnedCommunitiesIdHashSet.Contains(x.GlobalId))
                    )
                    .Select(x => new FollowCommunityViewModel(x, _OwnedCommunitiesIdHashSet.Contains(x.GlobalId)))
                    .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                    ;
            }
            else
            {
                return res.Data
                    .Where(x => !_OwnedCommunitiesIdHashSet.Contains(x.GlobalId))
                    .Select(x => new FollowCommunityViewModel(x, _OwnedCommunitiesIdHashSet.Contains(x.GlobalId)))
                    .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                    ;
            }
        }
    }
}

using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Hohoema.Models.Domain.Niconico.Community;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
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
        private Mntone.Nico2.Users.Follow.UserOwnedCommunityResponse _ownedCommunities;
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
                    _OwnedCommunitiesIdHashSet = _ownedCommunities.Data.OwnedCommunities.Select(x => x.Id).ToHashSet();
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
                return Enumerable.Concat(
                    _ownedCommunities.Data.OwnedCommunities,
                    res.Data.Where(x => !_OwnedCommunitiesIdHashSet.Contains(x.GlobalId))
                    )
                    .Select(x => new FollowCommunityViewModel(x, _OwnedCommunitiesIdHashSet.Contains(x.GlobalId)));
            }
            else
            {
                return res.Data
                    .Where(x => !_OwnedCommunitiesIdHashSet.Contains(x.GlobalId))
                    .Select(x => new FollowCommunityViewModel(x, _OwnedCommunitiesIdHashSet.Contains(x.GlobalId)));
            }
        }
    }
}

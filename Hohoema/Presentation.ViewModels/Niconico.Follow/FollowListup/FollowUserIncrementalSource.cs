using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Follow;
using Mntone.Nico2.Users.Follow;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowUserIncrementalSource : FollowIncrementalSourceBase
    {
        private readonly UserFollowProvider _userFollowProvider;
        private FollowUsersResponse _lastRes;

        public FollowUserIncrementalSource(UserFollowProvider userFollowProvider)
        {
            _userFollowProvider = userFollowProvider;
            MaxCount = 600;
        }

        public override async Task<IEnumerable<IFollowable>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            try
            {
                _lastRes = await _userFollowProvider.GetItemsAsync(pageSize, _lastRes);

                TotalCount = _lastRes.Data.Summary.Followees;
            }
            catch
            {
                _lastRes = null;
                return Enumerable.Empty<IFollowable>();
            }

            return _lastRes?.Data.Items.Select(x => new FollowUserViewModel(x));
        }
    }


}

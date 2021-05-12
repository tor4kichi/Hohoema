using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowMylistIncrementalSource : FollowIncrementalSourceBase
    {
        private readonly MylistFollowProvider _mylistFollowProvider;

        public FollowMylistIncrementalSource(MylistFollowProvider mylistFollowProvider)
        {
            _mylistFollowProvider = mylistFollowProvider;
        }

        public override async Task<IEnumerable<IFollowable>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageIndex != 0) 
            {
                return Enumerable.Empty<IFollowable>();  
            }

            
            var res = await _mylistFollowProvider.GetFollowMylistsAsync(sampleItemsCount: 1);

            TotalCount = res.Data.Mylists.Count;
            MaxCount = res.Data.FollowLimit;
            return res.Data.Mylists.Select(x => new FollowMylistViewModel(x));
        }
    }
}

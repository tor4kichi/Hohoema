using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Hohoema.Models.Domain.Niconico.Follow;
using Hohoema.Presentation.ViewModels.Niconico.Follow;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowTagIncrementalSource : FollowIncrementalSourceBase
    {
        private readonly TagFollowProvider _tagFollowProvider;

        public FollowTagIncrementalSource(TagFollowProvider tagFollowProvider)
        {
            _tagFollowProvider = tagFollowProvider;
            MaxCount = 30;
        }

        public override async Task<IEnumerable<IFollowable>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageIndex != 0)
            {
                return Enumerable.Empty<IFollowable>();
            }
            
            var res = await _tagFollowProvider.GetAllAsync();
            if (res == null)
            {
                return Enumerable.Empty<IFollowable>();
            }

            TotalCount = res.Count;

            return res.Select(x => new FollowTagViewModel(x));
        }
    }
}

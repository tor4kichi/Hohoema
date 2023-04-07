using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Follow;
using Hohoema.ViewModels.Niconico.Follow;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.ViewModels.Niconico.Follow
{
    public sealed class FollowTagIncrementalSource : FollowIncrementalSourceBase<ITag>
    {
        private readonly TagFollowProvider _tagFollowProvider;

        public FollowTagIncrementalSource(TagFollowProvider tagFollowProvider)
        {
            _tagFollowProvider = tagFollowProvider;
            MaxCount = 30;
        }

        public override async Task<IEnumerable<ITag>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            if (pageIndex != 0)
            {
                return Enumerable.Empty<ITag>();
            }
            
            var res = await _tagFollowProvider.GetAllAsync();
            if (res == null)
            {
                return Enumerable.Empty<ITag>();
            }

            TotalCount = res.Count;

            return res.Select(x => new FollowTagViewModel(x))
                .ToArray() // Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                ;
        }
    }
}

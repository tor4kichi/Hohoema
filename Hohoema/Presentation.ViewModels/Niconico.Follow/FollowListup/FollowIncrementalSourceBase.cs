using System.Collections.Generic;
using System.Threading.Tasks;
using Prism.Mvvm;
using System.Threading;
using Microsoft.Toolkit.Collections;
using Hohoema.Models.Domain.Niconico.Follow;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public abstract class FollowIncrementalSourceBase : BindableBase, IIncrementalSource<IFollowable>
    {
        private long _MaxCount;
        public long MaxCount
        {
            get => _MaxCount;
            protected set => SetProperty(ref _MaxCount, value);
        }

        private long _TotalCount;
        public long TotalCount
        {
            get => _TotalCount;
            protected set => SetProperty(ref _TotalCount, value);
        }

        public abstract Task<IEnumerable<IFollowable>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    }


}

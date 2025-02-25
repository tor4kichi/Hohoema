﻿#nullable enable
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Mylist;
using NiconicoToolkit.Mylist;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Follow;

public sealed class FollowMylistIncrementalSource : FollowIncrementalSourceBase<IMylist>
{
    private readonly MylistFollowProvider _mylistFollowProvider;

    public FollowMylistIncrementalSource(MylistFollowProvider mylistFollowProvider)
    {
        _mylistFollowProvider = mylistFollowProvider;
    }

    public override async Task<IEnumerable<IMylist>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex != 0) 
        {
            return Enumerable.Empty<IMylist>();  
        }

        
        var res = await _mylistFollowProvider.GetFollowMylistsAsync(sampleItemsCount: 1);

        TotalCount = res.Data.Mylists.Count;
        MaxCount = res.Data.FollowLimit;
        return res.Data.Mylists
            .Select(x => new FollowMylistViewModel(new MylistId((int)x.Id), x.Detail, x.Status))
            .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
            ;
    }
}

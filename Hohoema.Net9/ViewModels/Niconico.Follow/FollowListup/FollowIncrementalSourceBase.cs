﻿#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.Niconico.Follow;
using Microsoft.Toolkit.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.ViewModels.Niconico.Follow;

public abstract class FollowIncrementalSourceBase<ItemType> : ObservableObject, IIncrementalSource<ItemType>
    where ItemType : IFollowable
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

    public abstract Task<IEnumerable<ItemType>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}

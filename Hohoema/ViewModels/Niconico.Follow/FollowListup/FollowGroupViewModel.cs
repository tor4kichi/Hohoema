#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Channel;
using Hohoema.Models.Niconico.Community;
using Hohoema.Models.Niconico.Follow;
using Hohoema.Models.Niconico.Follow.LoginUser;
using Hohoema.Models.Niconico.Mylist;
using Hohoema.Models.Niconico.Video;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using NiconicoToolkit.Follow;
using Reactive.Bindings.Extensions;
using System;
using System.Reactive.Disposables;
using ZLogger;

namespace Hohoema.ViewModels.Niconico.Follow;

public class FollowGroupViewModel<ItemType> : ObservableObject, IDisposable 
    where ItemType : IFollowable
{
    public FollowItemType FollowItemType { get; }

    private long _MaxCount;
    public long MaxCount
    {
        get => _MaxCount;
        set => SetProperty(ref _MaxCount, value);
    }

    private long _TotalCount;
    public long TotalCount
    {
        get => _TotalCount;
        set => SetProperty(ref _TotalCount, value);
    }

    private readonly CompositeDisposable _disposables = new CompositeDisposable();
    protected readonly IFollowProvider<ItemType> _followProvider;
    protected readonly IMessenger _messenger;

    public IncrementalLoadingCollection<IIncrementalSource<ItemType>, ItemType> Items { get; }

    private readonly ILogger<FollowGroupViewModel<ItemType>> _logger;

    public FollowGroupViewModel(
        FollowItemType followItemType, 
        IFollowProvider<ItemType> followProvider, 
        FollowIncrementalSourceBase<ItemType> loadingSource, 
        IMessenger messenger
        )
    {
        FollowItemType = followItemType;
        _followProvider = followProvider;
        _messenger = messenger;
        Items = new IncrementalLoadingCollection<IIncrementalSource<ItemType>, ItemType>(source: loadingSource);
        _logger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<ILoggerFactory>().CreateLogger<FollowGroupViewModel<ItemType>>();

        loadingSource.ObserveProperty(x => x.MaxCount).Subscribe(x => MaxCount = x).AddTo(_disposables);
        loadingSource.ObserveProperty(x => x.TotalCount).Subscribe(x => TotalCount = x).AddTo(_disposables);
    }

    public virtual void Dispose()
    {
        ((IDisposable)_disposables).Dispose();
    }


    private RelayCommand<ItemType>? _RemoveFollowCommand;
    public RelayCommand<ItemType> RemoveFollowCommand =>
        _RemoveFollowCommand ??= new RelayCommand<ItemType>(async item => 
        {
            try
            {
                await _followProvider.RemoveFollowAsync(item);
            }
            catch (Exception e)
            {
                _logger.ZLogError(e, e.Message);
            }
        }
        , (item) => FollowItemType is not FollowItemType.Community and not FollowItemType.Channel
        );

    private RelayCommand<ItemType> _OpenPageCommand;
    public virtual RelayCommand<ItemType> OpenPageCommand =>
        _OpenPageCommand ??= new RelayCommand<ItemType>(item =>
        {
            _ = FollowItemType switch
            {
                FollowItemType.User => _messenger.OpenVideoListPageAsync((Models.Niconico.IUser)item),
                FollowItemType.Tag => _messenger.OpenVideoListPageAsync((ITag)item),
                FollowItemType.Mylist => _messenger.OpenVideoListPageAsync((IMylist)item),
                FollowItemType.Channel => _messenger.OpenVideoListPageAsync((IChannel)item),
                FollowItemType.Community => _messenger.OpenVideoListPageAsync((ICommunity)item),
                _ => throw new NotSupportedException(FollowItemType.ToString()),
            };
        });
}

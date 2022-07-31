using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Hohoema.Models.Domain.Niconico.Follow;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using CommunityToolkit.Mvvm.Input;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.UseCase;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
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

        CompositeDisposable _disposables = new CompositeDisposable();
        protected readonly IFollowProvider<ItemType> _followProvider;
        protected readonly PageManager _pageManager;

        public IncrementalLoadingCollection<IIncrementalSource<ItemType>, ItemType> Items { get; }

        private readonly ILogger<FollowGroupViewModel<ItemType>> _logger;

        public FollowGroupViewModel(FollowItemType followItemType, IFollowProvider<ItemType> followProvider, FollowIncrementalSourceBase<ItemType> loadingSource, PageManager pageManager)
        {
            FollowItemType = followItemType;
            _followProvider = followProvider;
            _pageManager = pageManager;
            Items = new IncrementalLoadingCollection<IIncrementalSource<ItemType>, ItemType>(source: loadingSource);
            _logger = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<ILoggerFactory>().CreateLogger<FollowGroupViewModel<ItemType>>();

            loadingSource.ObserveProperty(x => x.MaxCount).Subscribe(x => MaxCount = x).AddTo(_disposables);
            loadingSource.ObserveProperty(x => x.TotalCount).Subscribe(x => TotalCount = x).AddTo(_disposables);
        }

        public virtual void Dispose()
        {
            ((IDisposable)_disposables).Dispose();
        }


        private RelayCommand<ItemType> _RemoveFollowCommand;
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
                _pageManager.OpenVideoListPageCommand.Execute(item);
            });
    }


}

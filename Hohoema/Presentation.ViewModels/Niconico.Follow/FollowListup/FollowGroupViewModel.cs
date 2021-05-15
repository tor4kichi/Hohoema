using System;
using Prism.Mvvm;
using Hohoema.Models.Domain.Niconico.Follow;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;
using Hohoema.Models.Domain.Niconico.Follow.LoginUser;
using Prism.Commands;
using Hohoema.Models.UseCase.PageNavigation;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public class FollowGroupViewModel<ItemType> : BindableBase, IDisposable 
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

        public FollowGroupViewModel(FollowItemType followItemType, IFollowProvider<ItemType> followProvider, FollowIncrementalSourceBase<ItemType> loadingSource, PageManager pageManager)
        {
            FollowItemType = followItemType;
            _followProvider = followProvider;
            _pageManager = pageManager;
            Items = new IncrementalLoadingCollection<IIncrementalSource<ItemType>, ItemType>(source: loadingSource);

            loadingSource.ObserveProperty(x => x.MaxCount).Subscribe(x => MaxCount = x).AddTo(_disposables);
            loadingSource.ObserveProperty(x => x.TotalCount).Subscribe(x => TotalCount = x).AddTo(_disposables);
        }

        public virtual void Dispose()
        {
            ((IDisposable)_disposables).Dispose();
        }


        private DelegateCommand<ItemType> _RemoveFollowCommand;
        public DelegateCommand<ItemType> RemoveFollowCommand =>
            _RemoveFollowCommand ??= new DelegateCommand<ItemType>(item => 
            {
                _followProvider.RemoveFollowAsync(item);
            }
            , (item) => FollowItemType is not FollowItemType.Community and not FollowItemType.Channel
            );

        private DelegateCommand<ItemType> _OpenPageCommand;
        public virtual DelegateCommand<ItemType> OpenPageCommand =>
            _OpenPageCommand ??= new DelegateCommand<ItemType>(item =>
            {
                _pageManager.OpenVideoListPageCommand.Execute(item);
            });
    }


}

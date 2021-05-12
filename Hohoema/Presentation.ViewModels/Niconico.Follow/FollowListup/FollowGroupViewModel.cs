using System;
using Prism.Mvvm;
using Hohoema.Models.Domain.Niconico.Follow;
using Microsoft.Toolkit.Collections;
using Microsoft.Toolkit.Uwp;
using Reactive.Bindings.Extensions;
using System.Reactive.Disposables;

namespace Hohoema.Presentation.ViewModels.Niconico.Follow
{
    public sealed class FollowGroupViewModel : BindableBase, IDisposable
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
        public IncrementalLoadingCollection<IIncrementalSource<IFollowable>, IFollowable> Items { get; }

        public FollowGroupViewModel(FollowItemType followItemType, FollowIncrementalSourceBase loadingSource)
        {
            FollowItemType = followItemType;
            Items = new IncrementalLoadingCollection<IIncrementalSource<IFollowable>, IFollowable>(source: loadingSource);

            loadingSource.ObserveProperty(x => x.MaxCount).Subscribe(x => MaxCount = x).AddTo(_disposables);
            loadingSource.ObserveProperty(x => x.TotalCount).Subscribe(x => TotalCount = x).AddTo(_disposables);
        }

        public void Dispose()
        {
            ((IDisposable)_disposables).Dispose();
        }
    }


}

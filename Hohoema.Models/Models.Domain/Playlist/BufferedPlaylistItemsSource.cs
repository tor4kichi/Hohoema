using Hohoema.FixPrism;
using Hohoema.Models.Domain.Niconico.Video;
using Microsoft.Toolkit.Collections;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Threading;

namespace Hohoema.Models.Domain.Playlist
{
    public interface IBufferedPlaylistItemsSource : IIncrementalSource<IVideoContent>
    {
        int Count { get; }

        int IndexOf(IVideoContent item);

        ValueTask<IVideoContent> GetAsync(int index, CancellationToken ct = default);

        IPlaylistSortOption SortOption { get; }
    }
    public sealed class BufferedPlaylistItemsSource : ReadOnlyObservableCollection<IVideoContent>, IBufferedPlaylistItemsSource
    {
        public const int MaxBufferSize = 2000;
        private readonly IUnlimitedPlaylist _playlistItemsSource;
        public IPlaylistSortOption SortOption { get; }

        public ReadOnlyObservableCollection<IVideoContent> BufferedItems { get; }

        public int Count => this.Count;


        public bool CanLoadMoreItems { get; private set; } = true;

        public BufferedPlaylistItemsSource(IUnlimitedPlaylist playlistItemsSource, IPlaylistSortOption sortOption)
            : base(new ObservableCollection<IVideoContent>())
        {
            _playlistItemsSource = playlistItemsSource;
            SortOption = sortOption;
        }

        public int OneTimeLoadingItemsCount => _playlistItemsSource.OneTimeLoadItemsCount;

       

        public async ValueTask<IVideoContent?> GetAsync(int index, CancellationToken ct = default)
        {
            if (index < 0) { return null; }

            var page = index / OneTimeLoadingItemsCount;
            while (CanLoadMoreItems && IsLoadedPage(page) is false)
            {
                await GetPagedItemsAsync(page, OneTimeLoadingItemsCount, ct);
            }

            if (index >= this.Items.Count) { return null; }

            return this.Items[index];
        }

        FastAsyncLock _loadingLock = new FastAsyncLock();

        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
        {
            using var _ = await _loadingLock.LockAsync(ct);

            int head = pageIndex * pageSize;
            if (_loadedPageIndex == pageIndex)
            {
                return this.Items.Skip(head).Take(pageSize);
            }

            if (CanLoadMoreItems)
            {
                var items = await _playlistItemsSource.GetPagedItemsAsync(pageIndex, pageSize, SortOption, ct);
                foreach (var item in items)
                {
                    this.Items.Add(item);
                }

                if (items.Any() is false)
                {
                    CanLoadMoreItems = false;
                }

                _loadedPageIndex = pageIndex;
            }

            return this.Items.Skip(head).Take(pageSize);
        }

        private bool IsLoadedPage(int page)
        {
            var loadedPageCount = _loadedPageIndex;
            return page <= loadedPageCount;
        }

        int _loadedPageIndex = -1;
        
    }



    public sealed class BufferedShufflePlaylistItemsSource : IBufferedPlaylistItemsSource, INotifyCollectionChanged, IDisposable
    {
        public const int MaxBufferSize = 2000;
        public const int DefaultBufferSize = 500;


        CompositeDisposable _disposables;
        private readonly ISortablePlaylist _playlistItemsSource;
        public IPlaylistSortOption SortOption { get; }
        private readonly IScheduler _scheduler;

        public int Count => Items.Count;

        BehaviorSubject<Unit> _IndexUpdateTimingSubject = new BehaviorSubject<Unit>(Unit.Default);
        public IObservable<Unit> IndexUpdateTiming => _IndexUpdateTimingSubject;

        DispatchObservableCollection<IVideoContent> Items;

        public ReadOnlyReactiveCollection<IVideoContent> CreateItemsReadOnlyReactiveCollection(IScheduler scheduler)
        {
            return Items.ToReadOnlyReactiveCollection(scheduler);
        }

        public int IndexOf(IVideoContent video)
        {
            return Items.IndexOf(video);
        }

        public IVideoContent this[int index]
        {
            get => Items[index];
            set => Items[index] = value;
        }

        public BufferedShufflePlaylistItemsSource(ISortablePlaylist shufflePlaylist, IPlaylistSortOption sortOption, IScheduler scheduler)
        {
            Items = new ();
            _playlistItemsSource = shufflePlaylist;
            SortOption = sortOption;
            _scheduler = scheduler;
            _disposables = new CompositeDisposable();

            if (shufflePlaylist is IUserManagedPlaylist userManagedPlaylist)
            {
                userManagedPlaylist.ObserveAddChangedItems<IVideoContent>()
                    .Subscribe(args =>
                    {
                        scheduler.Schedule(() =>
                        {
                            Items.Clear();
                            isItemsFilled = false;
                            _ = GetAsync(0);
                            _IndexUpdateTimingSubject.OnNext(Unit.Default);
                        });
                    })
                    .AddTo(_disposables);

                userManagedPlaylist.ObserveRemoveChangedItems<IVideoContent>()
                    .Subscribe(args =>
                    {
                        scheduler.Schedule(() =>
                        {
                            foreach (var remove in args)
                            {
                                Items.Remove(remove);
                            }

                            _IndexUpdateTimingSubject.OnNext(Unit.Default);
                        });
                    })
                    .AddTo(_disposables);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                ((INotifyCollectionChanged)Items).CollectionChanged += value;
            }

            remove
            {
                ((INotifyCollectionChanged)Items).CollectionChanged -= value;
            }
        }

        public int OneTimeLoadingItemsCount = 30;

        public PlaylistId PlaylistId => _playlistItemsSource.PlaylistId;

        public async ValueTask<IVideoContent> GetAsync(int index, CancellationToken ct = default)
        {
            if (index < 0) { return null; }
            if (index >= _playlistItemsSource.TotalCount) { return null; }

            var indexInsidePage = index / OneTimeLoadingItemsCount;
            await GetPagedItemsAsync(indexInsidePage, OneTimeLoadingItemsCount, ct);

            return Items.ElementAt(index);
        }

        bool isItemsFilled = false;

        public async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
        {
            if (isItemsFilled is false)
            {
                isItemsFilled = true;
                var items = await _playlistItemsSource.GetAllItemsAsync(SortOption, ct);

                Items.AddRange(items);
            }

            var start = pageIndex * OneTimeLoadingItemsCount;
            return this.Items.Skip(start).Take(OneTimeLoadingItemsCount);
        }


        public bool CanLoadMoreItems => false;

        public ValueTask<bool> LoadMoreItemsAsync(CancellationToken ct = default)
        {
            return new(false);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

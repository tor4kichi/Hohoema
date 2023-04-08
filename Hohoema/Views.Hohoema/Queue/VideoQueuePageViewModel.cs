using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Playlist;
using Hohoema.Services;
using Hohoema.Services.Playlist;
using Hohoema.ViewModels.Niconico.Video.Commands;
using Hohoema.ViewModels.VideoListPage;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Collections;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Contracts.Services.Navigations;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.ViewModels.Pages.Hohoema.Queue
{
    public sealed class VideoQueuePageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, INavigationAware
    {
        public QueuePlaylist QueuePlaylist { get; }

        private readonly IMessenger _messenger;
        private readonly QueuePlaylistSetting _queuePlaylistSetting;
        private readonly NicoVideoProvider _nicoVideoProvider;
        
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RemoveWatchedItemsInAfterWatchPlaylistCommand RemoveWatchedItemsInAfterWatchPlaylistCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }

        public VideoQueuePageViewModel(
            ILoggerFactory loggerFactory,
            IMessenger messenger,
            QueuePlaylist queuePlaylist,
            QueuePlaylistSetting queuePlaylistSetting,
            ApplicationLayoutManager applicationLayoutManager,
            RemoveWatchedItemsInAfterWatchPlaylistCommand removeWatchedItemsInAfterWatchPlaylistCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            SelectionModeToggleCommand selectionModeToggleCommand,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            NicoVideoProvider nicoVideoProvider
            )
            : base(loggerFactory.CreateLogger<VideoQueuePageViewModel>())
        {
            _messenger = messenger;
            QueuePlaylist = queuePlaylist;
            _queuePlaylistSetting = queuePlaylistSetting;
            ApplicationLayoutManager = applicationLayoutManager;
            RemoveWatchedItemsInAfterWatchPlaylistCommand = removeWatchedItemsInAfterWatchPlaylistCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            _nicoVideoProvider = nicoVideoProvider;

            IsEnableGroupingByTitleSimulality = _queuePlaylistSetting.ToReactivePropertyAsSynchronized(x => x.IsGroupingNearByTitleThenByTitleAscending, mode: ReactivePropertyMode.DistinctUntilChanged)
                .AddTo(_CompositeDisposable);

            CurrentPlaylistToken = this.ObserveProperty(x => x.SelectedSortOptionItem)
                .Select(x => new PlaylistToken(QueuePlaylist, x))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);

        }

        public ReactiveProperty<bool> IsEnableGroupingByTitleSimulality { get; }

        public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }


        private QueuePlaylistSortOption _selectedSearchOptionItem;
        public QueuePlaylistSortOption SelectedSortOptionItem
        {
            get { return _selectedSearchOptionItem; }
            set { SetProperty(ref _selectedSearchOptionItem, value); }
        }

        public QueuePlaylistSortOption[] SortOptionItems { get; } = QueuePlaylist.SortOptions;


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            try
            {
                if (!string.IsNullOrEmpty(_queuePlaylistSetting.LastSelectedSortOptions))
                {
                    var option = QueuePlaylistSortOption.Deserialize(_queuePlaylistSetting.LastSelectedSortOptions);
                    SelectedSortOptionItem = SortOptionItems.FirstOrDefault(x => x.SortKey == option.SortKey && x.SortOrder == option.SortOrder) ?? QueuePlaylist.DefaultSortOption;
                }
                else
                {
                    SelectedSortOptionItem = QueuePlaylist.DefaultSortOption;
                }
            }
            catch
            {
                SelectedSortOptionItem = QueuePlaylist.DefaultSortOption;
            }
            

            Observable.Merge(
                IsEnableGroupingByTitleSimulality.ToUnit(),
                this.ObserveProperty(x => x.SelectedSortOptionItem, isPushCurrentValueAtFirst: false).ToUnit()
                )
                .Subscribe(sort => ResetList())
                .AddTo(_navigationDisposables);

            this.ObserveProperty(x => x.SelectedSortOptionItem)
                .Subscribe(x => _queuePlaylistSetting.LastSelectedSortOptions = x.Serialize())
                .AddTo(_navigationDisposables);

            _messenger.Register<PlaylistItemRemovedMessage, PlaylistId>(this, QueuePlaylist.Id, (r, m) => 
            {
                foreach (var item in m.Value.RemovedItems)
                {
                    var remove = ItemsView.Cast<IVideoContent>().FirstOrDefault(x => x.VideoId == item.VideoId);
                    ItemsView.Remove(remove);
                }
            });

            _messenger.Register<PlaylistItemAddedMessage, PlaylistId>(this, QueuePlaylist.Id, (r, m) =>
            {
                ItemsView.Clear();
                ItemsView.Refresh();
            });
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);

            _messenger.Unregister<PlaylistItemRemovedMessage, PlaylistId>(this, QueuePlaylist.Id);
            _messenger.Unregister<PlaylistItemAddedMessage, PlaylistId>(this, QueuePlaylist.Id);
        }

        protected override (int PageSize, IIncrementalSource<VideoListItemControlViewModel> IncrementalSource) GenerateIncrementalSource()
        {
            return (100, new QueuePlaylistIncrementalLoadingSource(QueuePlaylist, SelectedSortOptionItem, _nicoVideoProvider));
        }
    }


    public class QueuePlaylistIncrementalLoadingSource : IIncrementalSource<VideoListItemControlViewModel>
    {
        private readonly QueuePlaylist _playlist;
        private readonly QueuePlaylistSortOption _sortOption;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public QueuePlaylistIncrementalLoadingSource(
            QueuePlaylist playlist,
            QueuePlaylistSortOption sortOption,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _playlist = playlist;
            _sortOption = sortOption;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public const int OneTimeLoadingCount = 10;

        List<QueuePlaylistItem> _items;
        async Task<IEnumerable<VideoListItemControlViewModel>> IIncrementalSource<VideoListItemControlViewModel>.GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct)
        {
            if (pageIndex == 0)
            {
                var items = await _playlist.GetAllItemsAsync(_sortOption, ct);
                _items = items.Cast<QueuePlaylistItem>().ToList();
            }
            var head = pageIndex * pageSize;


            ct.ThrowIfCancellationRequested();
            return _items.Skip(head).Take(pageSize)
                .Select((item, i) => new VideoListItemControlViewModel(item) { PlaylistItemToken = new PlaylistItemToken(_playlist, _sortOption, item) })
                .ToArray()// Note: IncrementalLoadingSourceが複数回呼び出すためFreezeしたい
                ;
        }
    }
}

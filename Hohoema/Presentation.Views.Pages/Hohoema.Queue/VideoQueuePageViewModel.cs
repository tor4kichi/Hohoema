using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Microsoft.Toolkit.Collections;
using Prism.Navigation;
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

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Queue
{
    public sealed class VideoQueuePageViewModel : HohoemaListingPageViewModelBase<VideoListItemControlViewModel>, INavigationAware
    {
        public QueuePlaylist QueuePlaylist { get; }
        private readonly NicoVideoProvider _nicoVideoProvider;
        
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RemoveWatchedItemsInAfterWatchPlaylistCommand RemoveWatchedItemsInAfterWatchPlaylistCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public VideoPlayWithQueueCommand VideoPlayWithQueueCommand { get; }

        public VideoQueuePageViewModel(
            QueuePlaylist queuePlaylist,
            ApplicationLayoutManager applicationLayoutManager,
            RemoveWatchedItemsInAfterWatchPlaylistCommand removeWatchedItemsInAfterWatchPlaylistCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            SelectionModeToggleCommand selectionModeToggleCommand,
            VideoPlayWithQueueCommand videoPlayWithQueueCommand,
            NicoVideoProvider nicoVideoProvider
            )
        {
            QueuePlaylist = queuePlaylist;
            ApplicationLayoutManager = applicationLayoutManager;
            RemoveWatchedItemsInAfterWatchPlaylistCommand = removeWatchedItemsInAfterWatchPlaylistCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            VideoPlayWithQueueCommand = videoPlayWithQueueCommand;
            _nicoVideoProvider = nicoVideoProvider;

            CurrentPlaylistToken = this.ObserveProperty(x => x.SelectedSortOptionItem)
                .Select(x => new PlaylistToken(QueuePlaylist, x))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(_CompositeDisposable);

        }

        public ReadOnlyReactivePropertySlim<PlaylistToken> CurrentPlaylistToken { get; }


        private LocalPlaylistSortOption _selectedSearchOptionItem;
        public LocalPlaylistSortOption SelectedSortOptionItem
        {
            get { return _selectedSearchOptionItem; }
            set { SetProperty(ref _selectedSearchOptionItem, value); }
        }

        public LocalPlaylistSortOption[] SortOptionItems { get; } = QueuePlaylist.SortOptions;


        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            SelectedSortOptionItem = QueuePlaylist.DefaultSortOption;

            this.ObserveProperty(x => x.SelectedSortOptionItem)
                .Subscribe(sort => ResetList())
                .AddTo(_NavigatingCompositeDisposable);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);
        }

        protected override (int PageSize, IIncrementalSource<VideoListItemControlViewModel> IncrementalSource) GenerateIncrementalSource()
        {
            return (100, new QueuePlaylistIncrementalLoadingSource(QueuePlaylist, SelectedSortOptionItem, _nicoVideoProvider));
        }
    }


    public class QueuePlaylistIncrementalLoadingSource : IIncrementalSource<VideoListItemControlViewModel>
    {
        private readonly QueuePlaylist _playlist;
        private readonly LocalPlaylistSortOption _sortOption;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public QueuePlaylistIncrementalLoadingSource(
            QueuePlaylist playlist,
            LocalPlaylistSortOption sortOption,
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
            return _items.Skip(head).Take(pageSize).Select((item, i) => new VideoListItemControlViewModel(item) { PlaylistItemToken = new PlaylistItemToken(_playlist, _sortOption, item, head + i) });
        }
    }
}

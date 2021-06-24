using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Hohoema.Models.UseCase;
using Hohoema.Models.UseCase.Playlist;
using Hohoema.Presentation.ViewModels.Niconico.Video.Commands;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Prism.Navigation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.Presentation.ViewModels.Pages.Hohoema.Queue
{
    public sealed class VideoQueuePageViewModel : HohoemaPageViewModelBase, INavigationAware
    {
        private readonly QueuePlaylist _queuePlaylist;
        private readonly NicoVideoProvider _nicoVideoProvider;

        public IReadOnlyCollection<VideoListItemControlViewModel> PlaylistItems { get; private set; }
        
        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RemoveWatchedItemsInAfterWatchPlaylistCommand RemoveWatchedItemsInAfterWatchPlaylistCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public SelectionModeToggleCommand SelectionModeToggleCommand { get; }
        public VideoPlayCommand VideoPlayCommand { get; }

        public VideoQueuePageViewModel(
            QueuePlaylist queuePlaylist,
            ApplicationLayoutManager applicationLayoutManager,
            RemoveWatchedItemsInAfterWatchPlaylistCommand removeWatchedItemsInAfterWatchPlaylistCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            SelectionModeToggleCommand selectionModeToggleCommand,
            VideoPlayCommand videoPlayCommand,
            NicoVideoProvider nicoVideoProvider
            )
        {
            _queuePlaylist = queuePlaylist;
            ApplicationLayoutManager = applicationLayoutManager;
            RemoveWatchedItemsInAfterWatchPlaylistCommand = removeWatchedItemsInAfterWatchPlaylistCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            SelectionModeToggleCommand = selectionModeToggleCommand;
            VideoPlayCommand = videoPlayCommand;
            _nicoVideoProvider = nicoVideoProvider;
        }

        public override void OnNavigatedTo(INavigationParameters parameters)
        {
            base.OnNavigatedTo(parameters);

            PlaylistItems = _queuePlaylist.ToReadOnlyReactiveCollection(x => new VideoListItemControlViewModel(_nicoVideoProvider.GetCachedVideoInfo(x.ItemId)))
                .AddTo(_NavigatingCompositeDisposable);
            RaisePropertyChanged(nameof(PlaylistItems));
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {
            base.OnNavigatedFrom(parameters);

            PlaylistItems = null;
            RaisePropertyChanged(nameof(PlaylistItems));
        }
    }
}

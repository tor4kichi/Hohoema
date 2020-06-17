using NicoPlayerHohoema.FixPrism;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.UseCase;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.UseCase.Playlist.Commands;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
    public sealed class WatchAfterPageViewModel : HohoemaViewModelBase, INavigationAware
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PlaylistObservableCollection _watchAfterPlaylist;

        public IReadOnlyCollection<IVideoContent> PlaylistItems { get; }

        public IPlaylist Playlist => _watchAfterPlaylist;
        public ICommand PlayCommand => _hohoemaPlaylist.PlayCommand;

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RemoveWatchedItemsInAfterWatchPlaylistCommand RemoveWatchedItemsInAfterWatchPlaylistCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }

        public WatchAfterPageViewModel(
            HohoemaPlaylist hohoemaPlaylist,
            ApplicationLayoutManager applicationLayoutManager,
            RemoveWatchedItemsInAfterWatchPlaylistCommand removeWatchedItemsInAfterWatchPlaylistCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand
            )
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            ApplicationLayoutManager = applicationLayoutManager;
            RemoveWatchedItemsInAfterWatchPlaylistCommand = removeWatchedItemsInAfterWatchPlaylistCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            _watchAfterPlaylist = _hohoemaPlaylist.WatchAfterPlaylist;
            PlaylistItems = _watchAfterPlaylist;
        }

        public override void OnNavigatingTo(INavigationParameters parameters)
        {
            base.OnNavigatingTo(parameters);
        }

        public override void OnNavigatedFrom(INavigationParameters parameters)
        {

            base.OnNavigatedFrom(parameters);
        }
    }
}

using Hohoema.FixPrism;
using Hohoema.Interfaces;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico.Mylist;
using Hohoema.UseCase;
using Hohoema.UseCase.Playlist;
using Hohoema.UseCase.Playlist.Commands;
using Hohoema.ViewModels.Player.Commands;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Hohoema.ViewModels
{
    public sealed class WatchAfterPageViewModel : HohoemaViewModelBase, INavigationAware
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PlaylistObservableCollection _watchAfterPlaylist;

        public IReadOnlyCollection<IVideoContent> PlaylistItems { get; }

        public IPlaylist Playlist => _watchAfterPlaylist;
        public ICommand PlayCommand { get; }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }
        public RemoveWatchedItemsInAfterWatchPlaylistCommand RemoveWatchedItemsInAfterWatchPlaylistCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }

        public WatchAfterPageViewModel(
            HohoemaPlaylist hohoemaPlaylist,
            ApplicationLayoutManager applicationLayoutManager,
            RemoveWatchedItemsInAfterWatchPlaylistCommand removeWatchedItemsInAfterWatchPlaylistCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand,
            PlayVideoCommand playVideoCommand
            )
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            ApplicationLayoutManager = applicationLayoutManager;
            RemoveWatchedItemsInAfterWatchPlaylistCommand = removeWatchedItemsInAfterWatchPlaylistCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
            PlayCommand = playVideoCommand;
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

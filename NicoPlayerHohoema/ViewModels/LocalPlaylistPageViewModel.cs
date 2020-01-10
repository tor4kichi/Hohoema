using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Repository.Playlist;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Page;
using NicoPlayerHohoema.UseCase.Playlist;
using NicoPlayerHohoema.UseCase.Playlist.Commands;
using Prism.Commands;
using Prism.Navigation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.ViewModels
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public sealed class LocalPlaylistPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync
    {
        private readonly PageManager _pageManager;
        private readonly LocalMylistManager _localMylistManager;
        private readonly PlaylistAggregateGetter _playlistAggregate;

        public LocalPlaylistPageViewModel(
            PageManager pageManager,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            PlaylistAggregateGetter playlistAggregate,
            LocalPlaylistDeleteCommand localPlaylistDeleteCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand
            )
        {
            _pageManager = pageManager;
            _localMylistManager = localMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            _playlistAggregate = playlistAggregate;
            LocalPlaylistDeleteCommand = localPlaylistDeleteCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
        }


        public HohoemaPlaylist HohoemaPlaylist { get; }
        public LocalPlaylistDeleteCommand LocalPlaylistDeleteCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }

        public IReadOnlyCollection<IVideoContent> PlaylistItems { get; private set; }

        public IPlaylist Playlist { get; private set; }
        public bool IsWatchAfterPlaylist => Playlist.IsWatchAfterPlaylist();
        public bool IsQueuePlaylist => Playlist.IsQueuePlaylist();

        public async Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            string id = null;

            if (parameters.TryGetValue<string>("id", out var idString))
            {
                id = idString;
            }
            else if (parameters.TryGetValue<int>("id", out var idInt))
            {
                id = idInt.ToString();
            }

            var playlist = await _playlistAggregate.FindPlaylistAsync(id);

            if (playlist is MylistPlaylist) { return; }
            if (playlist == null) { return; }

            Playlist = playlist;

            RefreshItems();
        }

        protected override bool TryGetHohoemaPin(out HohoemaPin pin)
        {
            if (Playlist == null)
            {
                pin = null;
                return false;
            }
            else
            {
                pin = new HohoemaPin() { PageType = HohoemaPageType.LocalPlaylist, Parameter = Playlist.Id, Label = Playlist.Label };
                return true;
            }
        }


        void RefreshItems()
        {
            if (Playlist is LocalPlaylist localPlaylist)
            {
                var localPlaylistItems = new ObservableCollection<IVideoContent>(_localMylistManager.GetPlaylistItems(localPlaylist).ToList());
                PlaylistItems = localPlaylistItems;

                // キューと「あとで見る」はHohoemaPlaylistがリスト内容を管理しているが
                // ローカルプレイリストは内部DBが書き換えられるだけなので
                // 表示向けの更新をVMで引き受ける必要がある
                Observable.FromEventPattern<LocalPlaylistItemRemovedEventArgs>(
                    h => _localMylistManager.ItemRemoved += h,
                    h => _localMylistManager.ItemRemoved -= h
                    )
                    .Subscribe(e =>
                    {
                        var args = e.EventArgs;
                        foreach (var itemId in args.RemovedItems)
                        {
                            var removedItem = PlaylistItems.FirstOrDefault(x => x.Id == itemId);
                            if (removedItem != null)
                            {
                                localPlaylistItems.Remove(removedItem);
                            }
                        }
                    })
                    .AddTo(_NavigatingCompositeDisposable);

                _localMylistManager.LocalPlaylists.ObserveRemoveChanged()
                    .Subscribe(removed =>
                    {
                        if (Playlist.Id == removed.Id)
                        {
                            _pageManager.ForgetLastPage();
                            _pageManager.OpenPage(HohoemaPageType.UserMylist);
                        }
                    })
                    .AddTo(_NavigatingCompositeDisposable);
            }
            else if (Playlist is PlaylistObservableCollection collection)
            {
                PlaylistItems = collection;
            }
        }
    }
}

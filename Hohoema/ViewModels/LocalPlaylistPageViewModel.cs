﻿using I18NPortable;
using Hohoema.Interfaces;
using Hohoema.Models;
using Hohoema.Services;
using Hohoema.ViewModels.Pages;
using Hohoema.UseCase;
using Hohoema.UseCase.Playlist;
using Hohoema.UseCase.Playlist.Commands;
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
using Hohoema.Models.Pages;
using Hohoema.Models.Repository;
using Hohoema.Models.Repository.Niconico.Mylist;

namespace Hohoema.ViewModels
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public sealed class LocalPlaylistPageViewModel : HohoemaViewModelBase, INavigatedAwareAsync, IPinablePage, ITitleUpdatablePage
    {
        Models.Pages.HohoemaPin IPinablePage.GetPin()
        {
            return new Models.Pages.HohoemaPin()
            {
                Label = Playlist.Label,
                PageType = HohoemaPageType.LocalPlaylist,
                Parameter = $"id={Playlist.Id}"
            };
        }

        IObservable<string> ITitleUpdatablePage.GetTitleObservable()
        {
            return this.ObserveProperty(x => x.Playlist).Select(x => x?.Label);
        }

        private readonly PageManager _pageManager;
        private readonly LocalMylistManager _localMylistManager;
        private readonly PlaylistAggregateGetter _playlistAggregate;

        public LocalPlaylistPageViewModel(
            ApplicationLayoutManager applicationLayoutManager,
            PageManager pageManager,
            LocalMylistManager localMylistManager,
            HohoemaPlaylist hohoemaPlaylist,
            PlaylistAggregateGetter playlistAggregate,
            LocalPlaylistDeleteCommand localPlaylistDeleteCommand,
            PlaylistPlayAllCommand playlistPlayAllCommand
            )
        {
            ApplicationLayoutManager = applicationLayoutManager;
            _pageManager = pageManager;
            _localMylistManager = localMylistManager;
            HohoemaPlaylist = hohoemaPlaylist;
            _playlistAggregate = playlistAggregate;
            LocalPlaylistDeleteCommand = localPlaylistDeleteCommand;
            PlaylistPlayAllCommand = playlistPlayAllCommand;
        }

        public ApplicationLayoutManager ApplicationLayoutManager { get; }

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public LocalPlaylistDeleteCommand LocalPlaylistDeleteCommand { get; }
        public PlaylistPlayAllCommand PlaylistPlayAllCommand { get; }
        public IReadOnlyCollection<IVideoContent> PlaylistItems { get; private set; }

        public IPlaylist Playlist { get; private set; }

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


        void RefreshItems()
        {
            if (Playlist is LocalPlaylist localPlaylist)
            {
                var localPlaylistItems = new ObservableCollection<IVideoContent>(localPlaylist.GetPlaylistItems().ToList());
                PlaylistItems = localPlaylistItems;

                Observable.FromEventPattern<LocalPlaylistItemRemovedEventArgs>(
                    h => localPlaylist.ItemRemoved += h,
                    h => localPlaylist.ItemRemoved -= h
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
        }

        private DelegateCommand<IVideoContent> _PlayWithCurrentPlaylistCommand;
        public DelegateCommand<IVideoContent> PlayWithCurrentPlaylistCommand
        {
            get
            {
                return _PlayWithCurrentPlaylistCommand
                    ?? (_PlayWithCurrentPlaylistCommand = new DelegateCommand<IVideoContent>((video) =>
                    {
                        HohoemaPlaylist.PlayContinueWithPlaylist(video, Playlist);
                    }
                    ));
            }
        }
    }
}

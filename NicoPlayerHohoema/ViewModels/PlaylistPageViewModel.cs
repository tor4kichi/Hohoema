using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NicoPlayerHohoema.Models;
using System.Collections.ObjectModel;
using Prism.Mvvm;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace NicoPlayerHohoema.ViewModels
{
    public class PlaylistPageViewModel : HohoemaViewModelBase
    {
        public HohoemaPlaylist Playlist { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistViewModel> Playlists { get; private set; }


        public PlaylistPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
            : base(hohoemaApp, pageManager, canActivateBackgroundUpdate:true)
        {
            Playlist = hohoemaApp.Playlist;
            Playlists = Playlist.Playlists.ToReadOnlyReactiveCollection(x => 
                new PlaylistViewModel(HohoemaApp.Playlist, x)
                );
        }



        private DelegateCommand _CreatePlaylistCommand;
        public DelegateCommand CreatePlaylistCommand
        {
            get
            {
                return _CreatePlaylistCommand
                    ?? (_CreatePlaylistCommand = new DelegateCommand(() => 
                    {
                        Playlist.CreatePlaylist(Guid.NewGuid().ToString(), "New Playlist");
                    }));
            }
        }

        
    }


    public class PlaylistViewModel : BindableBase
    {
        public ReadOnlyReactiveProperty<string> PlaylistName { get; private set; }
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }
        public Playlist Playlist { get; private set; }

        public ReadOnlyReactiveCollection<PlaylistItem> PlaylistItems { get; private set; }


        public PlaylistViewModel(HohoemaPlaylist hohoemaPlaylist, Playlist playlist)
        {
            HohoemaPlaylist = hohoemaPlaylist;
            Playlist = playlist;
            PlaylistItems = Playlist.PlaylistItems.ToReadOnlyReactiveCollection();

            PlaylistName = Playlist.ObserveProperty(x => x.Name)
                .ToReadOnlyReactiveProperty();
        }


        private DelegateCommand _DeletePlaylistCommand;
        public DelegateCommand DeletePlaylistCommand
        {
            get
            {
                return _DeletePlaylistCommand
                    ?? (_DeletePlaylistCommand = new DelegateCommand(() =>
                    {
                        // 削除
                        HohoemaPlaylist.RemovePlaylist(Playlist);
                    }));
            }
        }

        private DelegateCommand _RenamePlaylistCommand;
        public DelegateCommand RenamePlaylistCommand
        {
            get
            {
                return _RenamePlaylistCommand
                    ?? (_RenamePlaylistCommand = new DelegateCommand(() =>
                    {
                        // TODO: Playlistのリネーム
                        // ダイアログで名前変更
                    }));
            }
        }

        

        private DelegateCommand<PlaylistItem> _PlayStartPlaylistCommand;
        public DelegateCommand<PlaylistItem> PlayStartPlaylistCommand
        {
            get
            {
                return _PlayStartPlaylistCommand
                    ?? (_PlayStartPlaylistCommand = new DelegateCommand<PlaylistItem>((item) =>
                    {
                        item.Play();
                    }));
            }
        }


        private DelegateCommand<PlaylistItem> _RemovePlaylistItemCommand;
        public DelegateCommand<PlaylistItem> RemovePlaylistItemCommand
        {
            get
            {
                return _RemovePlaylistItemCommand
                    ?? (_RemovePlaylistItemCommand = new DelegateCommand<PlaylistItem>((item) =>
                    {
                        // 
                        
                    }));
            }
        }
    }


}

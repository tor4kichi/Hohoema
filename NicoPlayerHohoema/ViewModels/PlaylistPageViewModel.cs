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
using System.Reactive.Linq;

namespace NicoPlayerHohoema.ViewModels
{
    public class PlaylistPageViewModel : HohoemaViewModelBase
    {
        public HohoemaPlaylist Playlist { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistViewModel> Playlists { get; private set; }

        public ReactiveProperty<PlaylistViewModel> SelectedItem { get; private set; }

        public PlaylistPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager) 
            : base(hohoemaApp, pageManager, canActivateBackgroundUpdate:true)
        {
            Playlist = hohoemaApp.Playlist;
            Playlists = Playlist.Playlists.ToReadOnlyReactiveCollection(x => 
                new PlaylistViewModel(HohoemaApp.Playlist, x)
                );

            SelectedItem = new ReactiveProperty<PlaylistViewModel>();

            // MDVでコンテンツが選択済みの場合、戻る動作を常にコンテンツのクローズに割り当てます
            // この仕様は仮です。
            // 本来はディテールのみを表示している状態にフックして戻る動作をブロックすべきです。
            // UWPCommunityToolkitの次期バージョン 1.3 で
            // MasterDetailsView.ViewState プロパティが導入されるようなので、
            // それがあればディテールのみ表示している状態が取得できるようになります。
            SelectedItem.Select(x => x != null)
                .Subscribe(isContentSelected => 
                {
                    if (isContentSelected)
                    {
                        AddSubsitutionBackNavigateAction("mdv_back", () => 
                        {
                            SelectedItem.Value = null;
                        });
                    }
                });
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

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
using NicoPlayerHohoema.Util;
using Microsoft.Practices.Unity;
using Prism.Windows.Navigation;
using System.Threading;
using System.Windows.Input;

namespace NicoPlayerHohoema.ViewModels
{
    public class PlaylistPageViewModel : HohoemaViewModelBase
    {
        Views.Service.MylistRegistrationDialogService _MylistRegistrationDialogService;

        public HohoemaPlaylist Playlist { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistViewModel> Playlists { get; private set; }

        public ReactiveProperty<PlaylistViewModel> SelectedItem { get; private set; }


        Dictionary<string, PlaylistViewModel> _CachedPlaylistVMs = new Dictionary<string, PlaylistViewModel>();

        PlaylistViewModel _PreviewSelectedPlaylistVM;

        public PlaylistPageViewModel(HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService) 
            : base(hohoemaApp, pageManager, canActivateBackgroundUpdate:true)
        {
            _MylistRegistrationDialogService = mylistDialogService;
            Playlist = hohoemaApp.Playlist;
            Playlists = Playlist.Playlists.ToReadOnlyReactiveCollection(x =>
                GetPlaylistViewModel(x)
                );

            SelectedItem = new ReactiveProperty<PlaylistViewModel>();

            SelectedItem.Subscribe(x => 
            {
                if (_PreviewSelectedPlaylistVM != null)
                {
                    _PreviewSelectedPlaylistVM.OnNavigatingFrom(new Prism.Windows.Navigation.NavigatingFromEventArgs()
                    {
                        NavigationMode = Windows.UI.Xaml.Navigation.NavigationMode.New,
                    }
                    , null, false);
                }

                if (x != null)
                {
                    x.OnNavigatedTo(new Prism.Windows.Navigation.NavigatedToEventArgs()
                    {
                        NavigationMode = Windows.UI.Xaml.Navigation.NavigationMode.New
                    }, null);
                }

                _PreviewSelectedPlaylistVM = x;
            });

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

                            return true;
                        });
                    }
                });
        }


        private PlaylistViewModel GetPlaylistViewModel(Playlist playlist)
        {
            var playlistId = playlist.Id;
            if (_CachedPlaylistVMs.ContainsKey(playlistId))
            {
                return _CachedPlaylistVMs[playlistId];
            }
            else
            {
                var newVM = new PlaylistViewModel(playlist, HohoemaApp, PageManager, _MylistRegistrationDialogService);
                _CachedPlaylistVMs[playlistId] = newVM;
                return newVM;
            }
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

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);

            if (e.Parameter != null && e.Parameter is string)
            {
                var maybePlaylistId = e.Parameter as string;
                var playlist = Playlists.FirstOrDefault(x => x.Playlist.Id == maybePlaylistId);

                if (playlist != null)
                {
                    SelectedItem.Value = playlist;
                }
            }
        }

        public override void OnNavigatingFrom(NavigatingFromEventArgs e, Dictionary<string, object> viewModelState, bool suspending)
        {
            base.OnNavigatingFrom(e, viewModelState, suspending);

            // ディテールに表示中のプレイリストページに対してナビゲートアクションを強制的に引き起こす目的でnull代入
            SelectedItem.Value = null;
        }

    }


    public class PlaylistViewModel : HohoemaVideoListingPageViewModelBase<PlaylistItemViewModel>
    {
        public bool IsDefaultPlaylist { get; private set; }
        public ReadOnlyReactiveProperty<string> PlaylistName { get; private set; }
        public HohoemaPlaylist HohoemaPlaylist { get; private set; }
        public Playlist Playlist { get; private set; }

        public ReactiveProperty<string> PlaylistNameCandidate { get; private set; }

        public ReactiveCommand RemovePlaylistItemsCommand { get; private set; }


        public PlaylistViewModel(Playlist playlist, HohoemaApp hohoemaApp, PageManager pageManager, Views.Service.MylistRegistrationDialogService mylistDialogService)
            : base(hohoemaApp, pageManager, mylistDialogService, isRequireSignIn:false)
        {
            HohoemaPlaylist = hohoemaApp.Playlist;
            Playlist = playlist;
            IsDefaultPlaylist = Playlist.Id == HohoemaPlaylist.DefaultPlaylist.Id;

            PlaylistName = Playlist.ObserveProperty(x => x.Name)
                .ToReadOnlyReactiveProperty();

            PlaylistNameCandidate = new ReactiveProperty<string>();
            PlaylistName.Subscribe(x => PlaylistNameCandidate.Value = x);
            RenamePlaylistCommand = PlaylistNameCandidate
                .Select(x => !string.IsNullOrEmpty(x))
                .ToReactiveCommand();
            RenamePlaylistCommand.Subscribe(_ => 
            {
                Playlist.Name = PlaylistNameCandidate.Value;
            });



            RemovePlaylistItemsCommand = IsItemSelected
                .ToReactiveCommand()
                .AddTo(_CompositeDisposable);




            RemovePlaylistItemsCommand.Subscribe(x =>
            {
                var selectedItems = SelectedItems.ToArray();

                foreach (var item in selectedItems)
                {
                    RemoveItem(item);
                }

                ClearSelection();
            })
            .AddTo(_CompositeDisposable);
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


        private DelegateCommand _PlayPlaylistCommand;
        public DelegateCommand PlayPlaylistCommand
        {
            get
            {
                return _PlayPlaylistCommand
                    ?? (_PlayPlaylistCommand = new DelegateCommand(() =>
                    {
                        this.Playlist.Play();
                    }));
            }
        }

        public ReactiveCommand RenamePlaylistCommand { get; private set; }

        internal void PlayItem(PlaylistItemViewModel itemVM)
        {
            itemVM.PlaylistItem.Play();
        }

        internal void RemoveItem(PlaylistItemViewModel itemVM)
        {
            Playlist.Remove(itemVM.PlaylistItem);
            IncrementalLoadingItems.Remove(itemVM);
        }

        protected override IIncrementalSource<PlaylistItemViewModel> GenerateIncrementalSource()
        {
            return new PlaylistItemIncrementalSource(this);
        }
    }



    public class PlaylistItemViewModel : VideoInfoControlViewModel
    {
        public PlaylistViewModel PlaylistVM { get; private set; }
        public PlaylistItem PlaylistItem { get; private set; }

        public PlaylistItemViewModel(PlaylistItem item, NicoVideo nicoVideo, PageManager pageManager, PlaylistViewModel parentVM)
            : base(nicoVideo, pageManager)
        {
            PlaylistItem = item;
            PlaylistVM = parentVM;

        }

        
        private DelegateCommand _PlayStartPlaylistCommand;
        public DelegateCommand PlayStartPlaylistCommand
        {
            get
            {
                return _PlayStartPlaylistCommand
                    ?? (_PlayStartPlaylistCommand = new DelegateCommand(() =>
                    {
                        PlaylistVM.PlayItem(this);
                    }));
            }
        }


        public override ICommand PrimaryCommand
        {
            get
            {
                return PlayStartPlaylistCommand;
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
                        PlaylistVM.RemoveItem(this);
                    }));
            }
        }
    }



    public class PlaylistItemIncrementalSource : IIncrementalSource<PlaylistItemViewModel>
    {

        public PlaylistViewModel PlaylistVM { get; private set; }
        public Playlist Playlist { get; private set; }

        public HohoemaApp HohoemaApp { get; private set; }
        public PageManager PageManager { get; private set; }

        public PlaylistItemIncrementalSource(PlaylistViewModel playlistVM)
        {
            PlaylistVM = playlistVM;
            Playlist = playlistVM.Playlist;

            HohoemaApp = App.Current.Container.Resolve<HohoemaApp>();
            PageManager = App.Current.Container.Resolve<PageManager>();
        }



        public uint OneTimeLoadCount
        {
            get
            {
                return 100;
            }
        }

        public async Task<IEnumerable<PlaylistItemViewModel>> GetPagedItems(int head, int count)
        {
            var items = Playlist.PlaylistItems.Skip(head).Take(count).ToArray();

            var vmItems = new List<PlaylistItemViewModel>();

            foreach (var item in items)
            {
                var nicoVideo = await HohoemaApp.MediaManager.GetNicoVideoAsync(item.ContentId);
                var vmItem = new PlaylistItemViewModel(item, nicoVideo, PageManager, PlaylistVM);
                vmItems.Add(vmItem);
            }

            return vmItems.AsEnumerable();
        }

        public Task<int> ResetSource()
        {
            return Task.FromResult(Playlist.PlaylistItems.Count);
        }
    }
}

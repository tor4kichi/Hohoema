using NicoPlayerHohoema.Models;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Playback;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public class PlaylistSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        public IPlayableList CurrentPlaylist { get; private set; }
        public ReactiveProperty<string> CurrentPlaylistName { get; private set; }
        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistItem> PlaylistItems { get; private set; }

        private PlaylistItem _CurrentPlayingItem;
        public PlaylistItem CurrentPlayingItem
        {
            get { return _CurrentPlayingItem; }
            set { SetProperty(ref _CurrentPlayingItem, value); }
        }


        HohoemaPlaylist _Player;
        PlaylistSettings _PlaylistSettings;
        MediaPlayer _MediaPlayer;
        PageManager _PageManager;
        public PlaylistSidePaneContentViewModel(MediaPlayer mediaPlayer, HohoemaPlaylist playerModel, PlaylistSettings playlistSettings, PageManager pageManager)
        {
            _MediaPlayer = mediaPlayer;
            _Player = playerModel;
            _PlaylistSettings = playlistSettings;
            _PageManager = pageManager;

            CurrentPlaylist = playerModel.CurrentPlaylist;
            CurrentPlayingItem = playerModel.Player.Current;
            CurrentPlaylistName = new ReactiveProperty<string>(CurrentWindowContextScheduler, _Player.CurrentPlaylist?.Name)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = _PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            IsTrackRepeatModeEnable = _PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            IsListRepeatModeEnable = _PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.List)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            IsTrackRepeatModeEnable.Subscribe(x =>
            {
                _MediaPlayer.IsLoopingEnabled = x;
            })
                .AddTo(_CompositeDisposable);

            PlaylistCanGoBack = _Player.Player.ObserveProperty(x => x.CanGoBack)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            PlaylistCanGoNext = _Player.Player.ObserveProperty(x => x.CanGoNext)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            PlaylistItems = CurrentPlaylist.PlaylistItems
                .ToReadOnlyReactiveCollection(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            RaisePropertyChanged(nameof(PlaylistItems));
        }




        private DelegateCommand<PlaylistItem> _OpenPlaylistItemCommand;
        public DelegateCommand<PlaylistItem> OpenPlaylistItemCommand
        {
            get
            {
                return _OpenPlaylistItemCommand
                    ?? (_OpenPlaylistItemCommand = new DelegateCommand<PlaylistItem>((item) =>
                    {
                        if (item != CurrentPlayingItem)
                        {
                            _Player.Play(item);
                        }
                    }
                    ));
            }
        }

        private DelegateCommand _OpenCurrentPlaylistPageCommand;
        public DelegateCommand OpenCurrentPlaylistPageCommand
        {
            get
            {
                return _OpenCurrentPlaylistPageCommand
                    ?? (_OpenCurrentPlaylistPageCommand = new DelegateCommand(() =>
                    {
                        if (_Player.PlayerDisplayType == PlayerDisplayType.PrimaryView)
                        {
                            _Player.PlayerDisplayType = PlayerDisplayType.PrimaryWithSmall;
                        }

                        _PageManager.OpenPage(HohoemaPageType.Mylist,
                            new MylistPagePayload(_Player.CurrentPlaylist).ToParameterString()
                            );
                    }
                    ));
            }
        }

        private DelegateCommand _ToggleRepeatModeCommand;
        public DelegateCommand ToggleRepeatModeCommand
        {
            get
            {
                return _ToggleRepeatModeCommand
                    ?? (_ToggleRepeatModeCommand = new DelegateCommand(() =>
                    {
                        var playlistSettings = _PlaylistSettings;
                        switch (playlistSettings.RepeatMode)
                        {
                            case MediaPlaybackAutoRepeatMode.List:
                                playlistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.Track;
                                break;
                            case MediaPlaybackAutoRepeatMode.Track:
                                playlistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.None;
                                break;
                            case MediaPlaybackAutoRepeatMode.None:
                                playlistSettings.RepeatMode = MediaPlaybackAutoRepeatMode.List;
                                break;
                            default:
                                break;
                        }
                    }
                    ));
            }
        }

        private DelegateCommand _ToggleShuffleCommand;
        public DelegateCommand ToggleShuffleCommand
        {
            get
            {
                return _ToggleShuffleCommand
                    ?? (_ToggleShuffleCommand = new DelegateCommand(() =>
                    {
                        _PlaylistSettings.IsShuffleEnable = !_PlaylistSettings.IsShuffleEnable;
                    }
                    ));
            }
        }


    }
}

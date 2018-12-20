using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using Windows.Media;
using Windows.Media.Playback;

namespace NicoPlayerHohoema.ViewModels.PlayerSidePaneContent
{
    public class PlaylistSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        
        public PlaylistSidePaneContentViewModel(
            MediaPlayer mediaPlayer,
            HohoemaPlaylist playerModel,
            PlaylistSettings playlistSettings,
            PageManager pageManager
            )
        {
            MediaPlayer = mediaPlayer;
            HohoemaPlaylist = playerModel;
            PlaylistSettings = playlistSettings;
            PageManager = pageManager;

            CurrentPlaylist = playerModel.CurrentPlaylist;
            CurrentPlayingItem = playerModel.Player.Current;
            CurrentPlaylistName = new ReactiveProperty<string>(CurrentWindowContextScheduler, HohoemaPlaylist.CurrentPlaylist?.Label)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            IsTrackRepeatModeEnable = PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            IsListRepeatModeEnable = PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.List)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            IsTrackRepeatModeEnable.Subscribe(x =>
            {
                MediaPlayer.IsLoopingEnabled = x;
            })
                .AddTo(_CompositeDisposable);


            IsReverseEnabled = PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsReverseModeEnable, CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            PlaylistCanGoBack = HohoemaPlaylist.Player.ObserveProperty(x => x.CanGoBack)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            PlaylistCanGoNext = HohoemaPlaylist.Player.ObserveProperty(x => x.CanGoNext)
                .ToReactiveProperty(CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);

            PlaylistItems = CurrentPlaylist.Select(x => 
            {
                var video = Database.NicoVideoDb.Get(x);
                return new PlaylistItem()
                {
                    ContentId = x,
                    Title = video.Title,
                    Owner = CurrentPlaylist,
                    Type = PlaylistItemType.Video
                };
            }).ToObservable()
                .ToReadOnlyReactiveCollection(scheduler: CurrentWindowContextScheduler)
                .AddTo(_CompositeDisposable);
            RaisePropertyChanged(nameof(PlaylistItems));
        }

        public Services.HohoemaPlaylist HohoemaPlaylist { get; }
        public PlaylistSettings PlaylistSettings { get; }
        public MediaPlayer MediaPlayer { get; }
        public PageManager PageManager { get; }


        public Interfaces.IMylist CurrentPlaylist { get; private set; }
        public ReactiveProperty<string> CurrentPlaylistName { get; private set; }
        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsReverseEnabled { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }
        public ReadOnlyReactiveCollection<PlaylistItem> PlaylistItems { get; private set; }

        private PlaylistItem _CurrentPlayingItem;
        public PlaylistItem CurrentPlayingItem
        {
            get { return _CurrentPlayingItem; }
            set { SetProperty(ref _CurrentPlayingItem, value); }
        }


        private DelegateCommand _ToggleRepeatModeCommand;
        public DelegateCommand ToggleRepeatModeCommand
        {
            get
            {
                return _ToggleRepeatModeCommand
                    ?? (_ToggleRepeatModeCommand = new DelegateCommand(() =>
                    {
                        var playlistSettings = PlaylistSettings;
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
                        PlaylistSettings.IsShuffleEnable = !PlaylistSettings.IsShuffleEnable;
                    }
                    ));
            }
        }

        private DelegateCommand _ToggleReverseModeCommand;
        public DelegateCommand ToggleReverseModeCommand
        {
            get
            {
                return _ToggleReverseModeCommand
                    ?? (_ToggleReverseModeCommand = new DelegateCommand(() =>
                    {
                        PlaylistSettings.IsReverseModeEnable = !PlaylistSettings.IsReverseModeEnable;
                    }
                    ));
            }
        }

        
    }
}

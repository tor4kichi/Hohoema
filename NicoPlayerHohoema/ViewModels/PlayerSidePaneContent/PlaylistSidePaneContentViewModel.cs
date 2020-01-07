using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.Playlist;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
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
            PageManager pageManager,
            IScheduler scheduler
            )
        {
            MediaPlayer = mediaPlayer;
            HohoemaPlaylist = playerModel;
            PlaylistSettings = playlistSettings;
            PageManager = pageManager;
            _scheduler = scheduler;

            CurrentPlaylist = playerModel.CurrentPlaylist;
            
            CurrentPlaylistName = new ReactiveProperty<string>(_scheduler, HohoemaPlaylist.CurrentPlaylist?.Label)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            IsTrackRepeatModeEnable = PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);
            IsListRepeatModeEnable = PlaylistSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.List)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            IsReverseEnabled = PlaylistSettings.ToReactivePropertyAsSynchronized(x => x.IsReverseModeEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            PlaylistCanGoBack = HohoemaPlaylist.ObserveProperty(x => x.CanGoBack)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);
            PlaylistCanGoNext = HohoemaPlaylist.ObserveProperty(x => x.CanGoNext)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);
        }

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public PlaylistSettings PlaylistSettings { get; }
        public MediaPlayer MediaPlayer { get; }
        public PageManager PageManager { get; }


        public Interfaces.IPlaylist CurrentPlaylist { get; private set; }
        public ReactiveProperty<string> CurrentPlaylistName { get; private set; }
        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsReverseEnabled { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }
        

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
        private readonly IScheduler _scheduler;

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

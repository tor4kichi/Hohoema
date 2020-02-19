using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.Playlist;
using Prism.Commands;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
            PlayerSettings playerSettings,
            PageManager pageManager,
            IScheduler scheduler
            )
        {
            MediaPlayer = mediaPlayer;
            HohoemaPlaylist = playerModel;
            _playerSettings = playerSettings;
            PageManager = pageManager;
            _scheduler = scheduler;
            
            CurrentPlaylistName = new ReactiveProperty<string>(_scheduler, HohoemaPlaylist.CurrentPlaylist?.Label)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            IsTrackRepeatModeEnable = _playerSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.Track)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);
            IsListRepeatModeEnable = _playerSettings.ObserveProperty(x => x.RepeatMode)
                .Select(x => x == MediaPlaybackAutoRepeatMode.List)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            IsReverseEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsReverseModeEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            PlaylistCanGoBack = HohoemaPlaylist.ObserveProperty(x => x.CanGoBack)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);
            PlaylistCanGoNext = HohoemaPlaylist.ObserveProperty(x => x.CanGoNext)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            CurrentItems = HohoemaPlaylist.PlaylistItems.ToReadOnlyReactiveCollection(_scheduler)
                .AddTo(_CompositeDisposable);
        }


        public ReadOnlyReactiveCollection<IVideoContent> CurrentItems { get; }

        public HohoemaPlaylist HohoemaPlaylist { get; }
        public MediaPlayer MediaPlayer { get; }
        public PageManager PageManager { get; }

        public ReactiveProperty<string> CurrentPlaylistName { get; private set; }
        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsReverseEnabled { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }

        private readonly PlayerSettings _playerSettings;
        private readonly IScheduler _scheduler;


        private DelegateCommand _ToggleRepeatModeCommand;
        public DelegateCommand ToggleRepeatModeCommand
        {
            get
            {
                return _ToggleRepeatModeCommand
                    ?? (_ToggleRepeatModeCommand = new DelegateCommand(() =>
                    {
                        switch (_playerSettings.RepeatMode)
                        {
                            case MediaPlaybackAutoRepeatMode.List:
                                _playerSettings.RepeatMode = MediaPlaybackAutoRepeatMode.Track;
                                MediaPlayer.IsLoopingEnabled = true;
                                break;
                            case MediaPlaybackAutoRepeatMode.Track:
                                _playerSettings.RepeatMode = MediaPlaybackAutoRepeatMode.None;
                                MediaPlayer.IsLoopingEnabled = false;
                                break;
                            case MediaPlaybackAutoRepeatMode.None:
                                _playerSettings.RepeatMode = MediaPlaybackAutoRepeatMode.List;
                                MediaPlayer.IsLoopingEnabled = false;
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
                        _playerSettings.IsShuffleEnable = !_playerSettings.IsShuffleEnable;
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
                        _playerSettings.IsReverseModeEnable = !_playerSettings.IsReverseModeEnable;
                    }
                    ));
            }
        }

        
    }
}

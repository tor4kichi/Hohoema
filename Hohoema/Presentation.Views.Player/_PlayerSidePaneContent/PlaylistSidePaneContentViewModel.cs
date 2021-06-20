using I18NPortable;

using Hohoema.Models.Domain;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.NicoVideos;
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
using Windows.UI.Xaml.Data;
using Hohoema.Models.UseCase.PageNavigation;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Player;

namespace Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent
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
            
            CurrentPlaylistName = HohoemaPlaylist.ObserveProperty(x => x.CurrentPlaylist).Select(x => x?.Name)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            IsListRepeatModeEnable = _playerSettings.ObserveProperty(x => x.IsPlaylistLoopingEnabled)
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

            _currentItemCollectionView = new ObservableCollection<IVideoContent>();

            _currentPlaylistViewItem = new PlaylistCollectionViewItem()
            {
                Id = "playlist",
                Items = CurrentItems
            };

            _playlistCollectionViews = new ObservableCollection<PlaylistCollectionViewItem>()
            {
                new PlaylistCollectionViewItem()
                {
                    Id = "_current_video_",
                    Label = "PlayingVideo".Translate(),
                    Items = _currentItemCollectionView
                },
                _currentPlaylistViewItem
            };

            HohoemaPlaylist.ObserveProperty(x => x.CurrentPlaylist).Subscribe(x =>
            {
                _currentPlaylistViewItem.Label = x?.Name ?? string.Empty;
            })
                .AddTo(_CompositeDisposable);

            HohoemaPlaylist.ObserveProperty(x => x.CurrentItem)
                .Subscribe(x => 
                {
                    _currentItemCollectionView.Clear();
                    _currentItemCollectionView.Add(x);
                })
                .AddTo(_CompositeDisposable);

            CollectionViewSource = new CollectionViewSource()
            {
                IsSourceGrouped = true,
                Source = _playlistCollectionViews,
                ItemsPath = new Windows.UI.Xaml.PropertyPath(nameof(PlaylistCollectionViewItem.Items))
            };
        }

        public override void Dispose()
        {
            CurrentItems?.Dispose();
            CurrentPlaylistName?.Dispose();
            IsShuffleEnabled?.Dispose();
            IsListRepeatModeEnable?.Dispose();
            IsReverseEnabled?.Dispose();
            PlaylistCanGoBack?.Dispose();
            PlaylistCanGoNext?.Dispose();
            base.Dispose();
        }

        PlaylistCollectionViewItem _currentPlaylistViewItem;

        public class PlaylistCollectionViewItem
        {
            public string Id { get; set; }
            public string Label { get; set; }
            public object Items { get; set; }
        }

        ObservableCollection<IVideoContent> _currentItemCollectionView;
        ObservableCollection<PlaylistCollectionViewItem> _playlistCollectionViews;

        public CollectionViewSource CollectionViewSource { get; }


        public ReadOnlyReactiveCollection<IVideoContent> CurrentItems { get; }
        public HohoemaPlaylist HohoemaPlaylist { get; }
        public MediaPlayer MediaPlayer { get; }
        public PageManager PageManager { get; }

        public IReadOnlyReactiveProperty<string> CurrentPlaylistName { get; private set; }
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
                        _playerSettings.IsPlaylistLoopingEnabled = !_playerSettings.IsPlaylistLoopingEnabled;
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


        private DelegateCommand<IVideoContent> _PlayWithCurrentPlaylistCommand;
        public DelegateCommand<IVideoContent> PlayWithCurrentPlaylistCommand
        {
            get
            {
                return _PlayWithCurrentPlaylistCommand
                    ?? (_PlayWithCurrentPlaylistCommand = new DelegateCommand<IVideoContent>((video) =>
                    {
                        HohoemaPlaylist.PlayContinueWithPlaylist(video, HohoemaPlaylist.CurrentPlaylist);
                    }
                    ));
            }
        }
    }
}

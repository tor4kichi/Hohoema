using I18NPortable;

using Hohoema.Models.Domain;
using Hohoema.Presentation.Services;
using Hohoema.Models.UseCase.Playlist;
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
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Mvvm.Messaging;

namespace Hohoema.Presentation.ViewModels.Player.PlayerSidePaneContent
{
    public class PlaylistSidePaneContentViewModel : SidePaneContentViewModelBase
    {
        
        public PlaylistSidePaneContentViewModel(
            MediaPlayer mediaPlayer,
            HohoemaPlaylistPlayer hohoemaPlaylistPlayer,
            PlayerSettings playerSettings,
            PageManager pageManager,
            NicoVideoProvider nicoVideoProvider,
            IScheduler scheduler,
            IMessenger messenger
            )
        {
            MediaPlayer = mediaPlayer;
            _hohoemaPlaylistPlayer = hohoemaPlaylistPlayer;
            _playerSettings = playerSettings;
            PageManager = pageManager;
            _nicoVideoProvider = nicoVideoProvider;
            _scheduler = scheduler;
            _messenger = messenger;
            CurrentPlaylistName = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylist).Select(x => x?.Name)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            IsListRepeatModeEnable = _playerSettings.ObserveProperty(x => x.IsPlaylistLoopingEnabled)
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            IsReverseEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsReverseModeEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            PlaylistCanGoBack = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlayingIndex)
                .SelectMany(async _ => await _hohoemaPlaylistPlayer.CanGoPreviewAsync())
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);
            PlaylistCanGoNext = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlayingIndex)
                .SelectMany(async _ => await _hohoemaPlaylistPlayer.CanGoNextAsync())
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            CurrentItems = new ObservableCollection<IVideoContent>();
            foreach (var item in _hohoemaPlaylistPlayer.CopyBufferedItems() ?? Enumerable.Empty<PlaylistItem>())
            {
                if (item == null) { continue; }
                var video = _nicoVideoProvider.GetCachedVideoInfo(item.ItemId);
                CurrentItems.Add(video);
            }

            // TODO: HohoemaPlaylistPlayerの内部アイテムの更新にフックしてCurrentItemsを更新する

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

            _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylist).Subscribe(x =>
            {
                _currentPlaylistViewItem.Label = x?.Name ?? string.Empty;
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


        public ObservableCollection<IVideoContent> CurrentItems { get; }
        public MediaPlayer MediaPlayer { get; }
        public PageManager PageManager { get; }

        public IReadOnlyReactiveProperty<string> CurrentPlaylistName { get; private set; }
        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReactiveProperty<bool> IsTrackRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }
        public ReactiveProperty<bool> IsReverseEnabled { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoBack { get; private set; }
        public ReactiveProperty<bool> PlaylistCanGoNext { get; private set; }

        private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;
        private readonly PlayerSettings _playerSettings;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly IScheduler _scheduler;
        private readonly IMessenger _messenger;
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
                        if (_hohoemaPlaylistPlayer.CurrentPlaylist == null)
                        {
                            return;
                        }
                        var playlistId = _hohoemaPlaylistPlayer.CurrentPlaylist.PlaylistId;
                        _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(playlistId.Id, playlistId.Origin, playlistId.SortOptions, video.VideoId));
                    }
                    ));
            }
        }
    }
}

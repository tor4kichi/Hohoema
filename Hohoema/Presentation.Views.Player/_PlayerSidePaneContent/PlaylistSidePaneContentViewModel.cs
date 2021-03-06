﻿using I18NPortable;

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
using Microsoft.Toolkit.Diagnostics;
using Hohoema.Presentation.ViewModels.VideoListPage;
using Uno;
using Uno.Disposables;
using Microsoft.Toolkit.Uwp;
using System.Collections.Generic;

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
            CurrentPlaylist = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylist)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            CurrentPlaylistItem = _hohoemaPlaylistPlayer.ObserveProperty(x => x.CurrentPlaylistItem)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);
            IsShuffleEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsShuffleEnable, _scheduler)
                .AddTo(_CompositeDisposable);

            IsShuffleAvailable = _hohoemaPlaylistPlayer.ObserveProperty(x => x.IsShuffleAndRepeatAvailable)
                .ToReadOnlyReactiveProperty(eventScheduler: _scheduler)
                .AddTo(_CompositeDisposable);

            IsListRepeatModeEnable = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsPlaylistLoopingEnabled)
                .AddTo(_CompositeDisposable);

            PlaylistCanGoBack = _hohoemaPlaylistPlayer.GetCanGoNextOrPreviewObservable()
                .SelectMany(async _ => await _hohoemaPlaylistPlayer.CanGoPreviewAsync())
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);
            PlaylistCanGoNext = _hohoemaPlaylistPlayer.GetCanGoNextOrPreviewObservable()
                .SelectMany(async _ => await _hohoemaPlaylistPlayer.CanGoNextAsync())
                .ToReactiveProperty(_scheduler)
                .AddTo(_CompositeDisposable);

            _hohoemaPlaylistPlayer.GetBufferedItems()
                .Subscribe(items => 
                {
                    _scheduler.Schedule(async () => 
                    {
                        if (items is BufferedPlaylistItemsSource bufferedPlaylistItemsSource)
                        {
                            //                            var items = new HohoemaListingPageViewModelBase<IVideoContent>.HohoemaIncrementalLoadingCollection(bufferedPlaylistItemsSource);
                            var items = new IncrementalLoadingCollection<BufferedPlaylistItemsSource, IVideoContent>(bufferedPlaylistItemsSource, bufferedPlaylistItemsSource.OneTimeLoadingItemsCount);
                            //await items.LoadMoreItemsAsync((uint)BufferedPlaylistItemsSource.OneTimeLoadingItemsCount);
                            CurrentItems = items;
                        }
                        else
                        {
                            CurrentItems = items;
                        }
                    });
                })
                .AddTo(_CompositeDisposable);
        }


        


        public override void Dispose()
        {
            IsShuffleEnabled?.Dispose();
            IsListRepeatModeEnable?.Dispose();
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

        public IReadOnlyReactiveProperty<IPlaylist> CurrentPlaylist { get; }
        public IReadOnlyReactiveProperty<IVideoContent> CurrentPlaylistItem{ get; }

        private IList<IVideoContent> _currentItems;
        public IList<IVideoContent> CurrentItems
        {
            get { return _currentItems; }
            set { SetProperty(ref _currentItems, value); }
        }
        public MediaPlayer MediaPlayer { get; }
        public PageManager PageManager { get; }

        public ReactiveProperty<bool> IsShuffleEnabled { get; private set; }
        public ReadOnlyReactiveProperty<bool> IsShuffleAvailable { get; }
        public ReactiveProperty<bool> IsListRepeatModeEnable { get; private set; }      
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

                        var index = CurrentItems.IndexOf(video);
                        _messenger.Send(VideoPlayRequestMessage.PlayPlaylist(_hohoemaPlaylistPlayer.CurrentPlaylist, video));
                    }
                    ));
            }
        }
    }
}

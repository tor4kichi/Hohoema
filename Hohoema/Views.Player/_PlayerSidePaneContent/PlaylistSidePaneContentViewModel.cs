#nullable enable
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.Player;
using Hohoema.Models.Playlist;
using Hohoema.ViewModels.Navigation.Commands;
using Microsoft.Toolkit.Uwp;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Windows.Media.Playback;

namespace Hohoema.ViewModels.Player.PlayerSidePaneContent;

public class PlaylistSidePaneContentViewModel : SidePaneContentViewModelBase
{
    
    public PlaylistSidePaneContentViewModel(
        MediaPlayer mediaPlayer,
        HohoemaPlaylistPlayer hohoemaPlaylistPlayer,
        PlayerSettings playerSettings,
        NicoVideoProvider nicoVideoProvider,
        IScheduler scheduler,
        IMessenger messenger,
        OpenPageCommand openPageCommand
        )
    {
        MediaPlayer = mediaPlayer;
        _hohoemaPlaylistPlayer = hohoemaPlaylistPlayer;
        _playerSettings = playerSettings;        
        _nicoVideoProvider = nicoVideoProvider;
        _scheduler = scheduler;
        _messenger = messenger;
        OpenPageCommand = openPageCommand;
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

        IsAutoMovePlaylistEnabled = _playerSettings.ToReactivePropertyAsSynchronized(x => x.IsPlaylistAutoMoveEnabled, raiseEventScheduler: _scheduler)
            .AddTo(_CompositeDisposable);

        _hohoemaPlaylistPlayer.GetBufferedItems()
            .Subscribe(items => 
            {
                _scheduler.Schedule(async () => 
                {
                    if (CurrentItems is IDisposable disposeItems)
                    {
                        disposeItems.Dispose();
                    }

                    if (items == null)
                    {
                        CurrentItems = null;
                    }
                    else if (items is BufferedPlaylistItemsSource bufferedPlaylistItemsSource)
                    {
                        //                            var items = new HohoemaListingPageViewModelBase<IVideoContent>.HohoemaIncrementalLoadingCollection(bufferedPlaylistItemsSource);
                        var items = new IncrementalLoadingCollection<BufferedPlaylistItemsSource, IVideoContent>(bufferedPlaylistItemsSource, bufferedPlaylistItemsSource.OneTimeLoadingItemsCount);
                        //await items.LoadMoreItemsAsync((uint)BufferedPlaylistItemsSource.OneTimeLoadingItemsCount);
                        CurrentItems = items;
                    }
                    else if (items is BufferedShufflePlaylistItemsSource bufferedShufflePlaylistItemsSource)
                    {
                        CurrentItems = bufferedShufflePlaylistItemsSource.CreateItemsReadOnlyReactiveCollection(_scheduler);
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
    public OpenPageCommand OpenPageCommand { get; }

    public ReactiveProperty<bool> IsShuffleEnabled { get; }
    public ReadOnlyReactiveProperty<bool> IsShuffleAvailable { get; }
    public ReactiveProperty<bool> IsListRepeatModeEnable { get;  }
    public ReactiveProperty<bool> IsAutoMovePlaylistEnabled { get; }
    public ReactiveProperty<bool> PlaylistCanGoBack { get; }
    public ReactiveProperty<bool> PlaylistCanGoNext { get; }

    private readonly HohoemaPlaylistPlayer _hohoemaPlaylistPlayer;
    private readonly PlayerSettings _playerSettings;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly IScheduler _scheduler;
    private readonly IMessenger _messenger;
    private RelayCommand _ToggleRepeatModeCommand;
    public RelayCommand ToggleRepeatModeCommand
    {
        get
        {
            return _ToggleRepeatModeCommand
                ?? (_ToggleRepeatModeCommand = new RelayCommand(() =>
                {
                    _playerSettings.IsPlaylistLoopingEnabled = !_playerSettings.IsPlaylistLoopingEnabled;
                }
                ));
        }
    }

    private RelayCommand _ToggleShuffleCommand;
    public RelayCommand ToggleShuffleCommand
    {
        get
        {
            return _ToggleShuffleCommand
                ?? (_ToggleShuffleCommand = new RelayCommand(() =>
                {
                    _playerSettings.IsShuffleEnable = !_playerSettings.IsShuffleEnable;
                }
                ));
        }
    }

    private RelayCommand _ToggleReverseModeCommand;
    public RelayCommand ToggleReverseModeCommand
    {
        get
        {
            return _ToggleReverseModeCommand
                ?? (_ToggleReverseModeCommand = new RelayCommand(() =>
                {
                    _playerSettings.IsReverseModeEnable = !_playerSettings.IsReverseModeEnable;
                }
                ));
        }
    }


    private RelayCommand<IVideoContent> _PlayWithCurrentPlaylistCommand;
    public RelayCommand<IVideoContent> PlayWithCurrentPlaylistCommand
    {
        get
        {
            return _PlayWithCurrentPlaylistCommand
                ?? (_PlayWithCurrentPlaylistCommand = new RelayCommand<IVideoContent>(async (video) =>
                {
                    await _hohoemaPlaylistPlayer.PlayOnCurrentPlaylistAsync(video);
                }
                ));
        }
    }
}

using Hohoema.Presentation.Services;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Helpers;
using Prism.Navigation;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Uno.Threading;
using Hohoema.Models.UseCase.Niconico.Player.Events;
using Hohoema.Models.Domain.Playlist;
using NiconicoToolkit.Video;
using Hohoema.Models.Infrastructure;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Niconico.Video;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public enum PlayerDisplayView
    {
        PrimaryView,
        SecondaryView,
    }

    public sealed class VideoPlayRequestBridgeToPlayer 
        : IDisposable,
        IRecipient<VideoPlayRequestMessage>,
        IRecipient<PlayerPlayLiveRequestMessage>,
        IRecipient<ChangePlayerDisplayViewRequestMessage>
    {
        private readonly IMessenger _messenger;
        private readonly ScondaryViewPlayerManager _secondaryPlayerManager;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        

        FastAsyncLock _asyncLock = new FastAsyncLock();

        public VideoPlayRequestBridgeToPlayer(
            IMessenger messenger,
            ScondaryViewPlayerManager playerViewManager,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            LocalObjectStorageHelper localObjectStorageHelper,
            QueuePlaylist queuePlaylist
            )
        {
            _messenger = messenger;
            _secondaryPlayerManager = playerViewManager;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _localObjectStorage = localObjectStorageHelper;
            _queuePlaylist = queuePlaylist;

            DisplayMode = ReadDisplayMode();

            _messenger.Register<VideoPlayRequestMessage>(this);
            _messenger.Register<PlayerPlayLiveRequestMessage>(this);
            _messenger.Register<ChangePlayerDisplayViewRequestMessage>(this);
        }


        async Task PlayWithCurrentView(PlayerDisplayView displayMode, string pageName, INavigationParameters parameters)
        {
            using (await _asyncLock.LockAsync(default))
            {
                if (string.IsNullOrEmpty(pageName)) { throw new ArgumentException(nameof(pageName)); }

                if (displayMode == PlayerDisplayView.PrimaryView)
                {
                    if (_primaryViewPlayerManager.DisplayMode != PrimaryPlayerDisplayMode.Close)
                    {
                        if (DisplayMode == displayMode
                        && _lastNavigatedPageName == pageName
                        && (_lastNavigatedParameters?.SequenceEqual(parameters) ?? false))
                        {
                            System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                            return;
                        }
                    }

                    await _secondaryPlayerManager.CloseAsync().ConfigureAwait(false);

                    await Task.Delay(10);
                    
                    _ = _primaryViewPlayerManager.NavigationAsync(pageName, parameters);
                }
                else
                {
                    if (DisplayMode == displayMode
                    && _secondaryPlayerManager.LastNavigationPageName == pageName
                    && (_lastNavigatedParameters?.SequenceEqual(parameters) ?? false))
                    {
                        System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                        await _secondaryPlayerManager.ShowSecondaryViewAsync();
                        return;
                    }

                    _primaryViewPlayerManager.Close();

                    await Task.Delay(10);

                    _ = _secondaryPlayerManager.NavigationAsync(pageName, parameters).ConfigureAwait(false);
                }

                _lastNavigatedPageName = pageName;
                _lastNavigatedParameters = parameters;
            }
        }

        public PlayerDisplayView DisplayMode { get; private set; }
        string _lastNavigatedPageName;
        INavigationParameters _lastNavigatedParameters;

        private PlaylistItem? _lastPlayedItem;


        public void Dispose()
        {
            SaveDisplayMode();
        }


        private readonly LocalObjectStorageHelper _localObjectStorage;
        private readonly QueuePlaylist _queuePlaylist;
        
        void SaveDisplayMode()
        {
            _localObjectStorage.Save(nameof(PlayerDisplayView), DisplayMode);
        }

        public PlayerDisplayView ReadDisplayMode()
        {
            return _localObjectStorage.Read<PlayerDisplayView>(nameof(PlayerDisplayView));
        }

        public async void Receive(PlayerPlayLiveRequestMessage message)
        {
            var pageName = nameof(Presentation.Views.Player.LivePlayerPage);
            var parameters = new NavigationParameters("id=" + message.Value.LiveId);

            // 生放送再生開始前に動画の再生を中止
            if (DisplayMode == PlayerDisplayView.PrimaryView)
            {
                await _primaryViewPlayerManager.PlaylistPlayer.ClearAsync();
            }
            else
            {
                if (_secondaryPlayerManager.PlaylistPlayer != null)
                {
                    await _secondaryPlayerManager.PlaylistPlayer.ClearAsync();
                }
            }

            await PlayWithCurrentView(DisplayMode, pageName, parameters);

            // 生放送視聴した場合には動画視聴の情報をクリア
            _lastPlayedItem = null;
        }

        public async void Receive(ChangePlayerDisplayViewRequestMessage message)
        {
            var mode = DisplayMode == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
            DisplayMode = mode;
            SaveDisplayMode();

            if (_lastPlayedItem != null)
            {
                await VideoPlayAsync(_lastPlayedItem, mode);
            }
            else if (_lastNavigatedPageName != null && _lastNavigatedParameters != null)
            {
                await PlayWithCurrentView(mode, _lastNavigatedPageName, _lastNavigatedParameters);
            }
        }

        public void Receive(VideoPlayRequestMessage message)
        {
            static PlaylistItem ToPlaylistItem(VideoPlayRequestMessage message, QueuePlaylist queuePlaylist)
            {
                if (message.PlaylistItem != null) { return message.PlaylistItem; }


                bool isPlaylistReady = message.PlaylistId != null
                    && message.PlaylistOrigin != null;
                bool isVideoReady = message.VideoId != null && message.VideoId != default(VideoId);


                if (isPlaylistReady && isVideoReady)
                {
                    if (message.PlaylistId == QueuePlaylist.Id.Id && message.PlaylistOrigin == PlaylistItemsSourceOrigin.Local)
                    {
                        if (!queuePlaylist.Contains(message.VideoId.Value))
                        {
                            return queuePlaylist.Insert(0, message.VideoId.Value);
                        }
                        else
                        {
                            return queuePlaylist.First(x => x.ItemId == message.VideoId.Value);
                        }
                    }
                    else
                    {
                        return new PlaylistItem(new PlaylistId() { Id = message.PlaylistId, Origin = message.PlaylistOrigin.Value, SortOptions = message.PlaylistSortOptions }, -1, message.VideoId.Value);
                    }
                }
                else if (isPlaylistReady)
                {
                    return new PlaylistItem(new PlaylistId() { Id = message.PlaylistId, Origin = message.PlaylistOrigin.Value, SortOptions = message.PlaylistSortOptions }, -1, default(VideoId));
                }
                else if (isVideoReady)
                {
                    if (!queuePlaylist.Contains(message.VideoId.Value))
                    {
                        return queuePlaylist.Insert(0, message.VideoId.Value);
                    }
                    else
                    {
                        return queuePlaylist.First(x => x.ItemId == message.VideoId.Value);
                    }
                }
                else
                {
                    throw new HohoemaExpception();
                }

            }
            message.Reply(VideoPlayAsync(ToPlaylistItem(message, _queuePlaylist), DisplayMode));
        }

        private async Task<VideoPlayRequestMessageData> VideoPlayAsync(PlaylistItem playlistItem, PlayerDisplayView displayMode, TimeSpan? initialPosition = null)
        {
            using (await _asyncLock.LockAsync(default))
            {
                var pageName = nameof(Presentation.Views.Player.VideoPlayerPage);
                
                static async Task Play(HohoemaPlaylistPlayer player, PlaylistItem playlistItem, TimeSpan? initialPosition)
                {
                    await player.PlayAsync(playlistItem, initialPosition);
                }

                if (displayMode == PlayerDisplayView.PrimaryView)
                {
                    if (_secondaryPlayerManager.PlaylistPlayer != null 
                        && _secondaryPlayerManager.PlaylistPlayer.CurrentPlaylistItem == playlistItem)
                    {
                        initialPosition = _secondaryPlayerManager.PlaylistPlayer.GetCurrentPlaybackPosition();
                    }

                    await _secondaryPlayerManager.CloseAsync().ConfigureAwait(false);

                    // 動画ページへの遷移が必要なときだけ遷移
                    if (_primaryViewPlayerManager.DisplayMode != PrimaryPlayerDisplayMode.Close
                        || DisplayMode != displayMode
                        || _lastNavigatedPageName != pageName
                        )
                    {
                        System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                        await _primaryViewPlayerManager.NavigationAsync(pageName, null);
                    }

                    await Task.Delay(10);

                    await Play(_primaryViewPlayerManager.PlaylistPlayer, playlistItem, initialPosition);

                    _lastNavigatedPageName = pageName;
                    _lastNavigatedParameters = null;
                    _lastPlayedItem = playlistItem;

                    return new VideoPlayRequestMessageData() { IsSuccess = true };
                }
                else
                {
                    if (_primaryViewPlayerManager.PlaylistPlayer != null
                        && _primaryViewPlayerManager.PlaylistPlayer.CurrentPlaylistItem == playlistItem)
                    {
                        initialPosition = _primaryViewPlayerManager.PlaylistPlayer.GetCurrentPlaybackPosition();
                    }

                    _primaryViewPlayerManager.Close();

                    // TODO: PlayAsyncは呼び出しつつ、NavigationAsyncだけskipしたい
                    if (DisplayMode != displayMode
                    || _secondaryPlayerManager.LastNavigationPageName != pageName
                    )
                    {
                        await _secondaryPlayerManager.NavigationAsync(pageName, null).ConfigureAwait(false);
                    }

                    await _secondaryPlayerManager.ShowSecondaryViewAsync();

                    await Task.Delay(10);

                    await Play(_secondaryPlayerManager.PlaylistPlayer, playlistItem, initialPosition);

                    _lastNavigatedPageName = pageName;
                    _lastNavigatedParameters = null;
                    _lastPlayedItem = playlistItem;

                    return new VideoPlayRequestMessageData() { IsSuccess = true };
                }
            }
        }
    }
}

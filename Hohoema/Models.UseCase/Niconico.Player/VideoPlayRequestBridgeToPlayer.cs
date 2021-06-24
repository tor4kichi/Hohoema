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
                    && _lastNavigatedPageName == pageName
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

        public async void Receive(PlayerPlayVideoRequestMessage message)
        {
            var pageName = nameof(Presentation.Views.Player.VideoPlayerPage);
            var parameters = new NavigationParameters($"id={message.Value.VideoId}&position={(int)message.Value.Position.TotalSeconds}");

            await PlayWithCurrentView(DisplayMode, pageName, parameters);
        }

        public async void Receive(PlayerPlayLiveRequestMessage message)
        {
            var pageName = nameof(Presentation.Views.Player.LivePlayerPage);
            var parameters = new NavigationParameters("id=" + message.Value.LiveId);

            await PlayWithCurrentView(DisplayMode, pageName, parameters);
        }

        public async void Receive(ChangePlayerDisplayViewRequestMessage message)
        {
            var mode = DisplayMode == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
            if (_lastNavigatedPageName != null && _lastNavigatedParameters != null)
            {
                await PlayWithCurrentView(mode, _lastNavigatedPageName, _lastNavigatedParameters);
            }

            DisplayMode = mode;
            SaveDisplayMode();
        }

        public void Receive(VideoPlayRequestMessage message)
        {
            message.Reply(VideoPlayAsync(message));
        }

        private async Task<VideoPlayRequestMessageData> VideoPlayAsync(VideoPlayRequestMessage message)
        {
            using (await _asyncLock.LockAsync(default))
            {
                var displayMode = DisplayMode == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
                var pageName = nameof(Presentation.Views.Player.VideoPlayerPage);
                
                static PlaylistItem ToPlaylistItem(VideoPlayRequestMessage message, QueuePlaylist queuePlaylist)
                {
                    if (message.PlaylistItem != null) { return message.PlaylistItem; }


                    bool isPlaylistReady = message.PlaylistId != null
                        && message.PlaylistOrigin != null;
                    bool isVideoReady = message.VideoId != null && message.VideoId != default(VideoId);


                    if (isPlaylistReady && isVideoReady)
                    {
                        return new PlaylistItem(new PlaylistId() { Id = message.PlaylistId, Origin = message.PlaylistOrigin.Value, SortOptions = message.PlaylistSortOptions }, -1, message.VideoId.Value);
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

                static async Task Play(HohoemaPlaylistPlayer player, PlaylistItem playlistItem, TimeSpan? initialPosition)
                {
                    await player.PlayAsync(playlistItem, null, initialPosition);
                }

                if (displayMode == PlayerDisplayView.PrimaryView)
                {
                    if (_primaryViewPlayerManager.DisplayMode != PrimaryPlayerDisplayMode.Close)
                    {
                        if (DisplayMode == displayMode
                        && _lastNavigatedPageName == pageName
                        )
                        {
                            System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                            return new VideoPlayRequestMessageData() { IsSuccess = false };
                        }
                    }

                    
                    await _secondaryPlayerManager.CloseAsync().ConfigureAwait(false);

                    await Task.Delay(10);

                    await Play(_primaryViewPlayerManager.PlaylistPlayer, ToPlaylistItem(message, _queuePlaylist), message.Potision);

                    await _primaryViewPlayerManager.NavigationAsync(pageName, null);

                    _lastNavigatedPageName = pageName;
                    _lastNavigatedParameters = null;

                    return new VideoPlayRequestMessageData() { IsSuccess = true };
                }
                else
                {
                    if (DisplayMode == displayMode
                    && _lastNavigatedPageName == pageName
                    )
                    {
                        System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                        await _secondaryPlayerManager.ShowSecondaryViewAsync();
                        return new VideoPlayRequestMessageData() { IsSuccess = false };
                    }

                    _primaryViewPlayerManager.Close();

                    await Task.Delay(10);

                    await Play(_secondaryPlayerManager.PlaylistPlayer, ToPlaylistItem(message, _queuePlaylist), message.Potision);

                    await _secondaryPlayerManager.NavigationAsync(pageName, null).ConfigureAwait(false);

                    _lastNavigatedPageName = pageName;
                    _lastNavigatedParameters = null;
                    
                    return new VideoPlayRequestMessageData() { IsSuccess = true };
                }
            }
        }
    }
}

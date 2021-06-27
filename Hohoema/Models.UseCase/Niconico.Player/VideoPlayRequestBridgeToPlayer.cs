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
using Hohoema.Models.Domain.Niconico.Live;
using NiconicoToolkit.Live;

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

        private readonly LocalObjectStorageHelper _localObjectStorage;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly NicoLiveProvider _nicoLiveProvider;

        FastAsyncLock _asyncLock = new FastAsyncLock();

        public VideoPlayRequestBridgeToPlayer(
            IMessenger messenger,
            ScondaryViewPlayerManager playerViewManager,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            LocalObjectStorageHelper localObjectStorageHelper,
            QueuePlaylist queuePlaylist,
            NicoVideoProvider nicoVideoProvider,
            NicoLiveProvider nicoLiveProvider
            )
        {
            _messenger = messenger;
            _secondaryPlayerManager = playerViewManager;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _localObjectStorage = localObjectStorageHelper;
            _queuePlaylist = queuePlaylist;
            _nicoVideoProvider = nicoVideoProvider;
            _nicoLiveProvider = nicoLiveProvider;
            DisplayMode = ReadDisplayMode();

            _messenger.Register<VideoPlayRequestMessage>(this);
            _messenger.Register<PlayerPlayLiveRequestMessage>(this);
            _messenger.Register<ChangePlayerDisplayViewRequestMessage>(this);
        }



        public PlayerDisplayView DisplayMode { get; private set; }
        
        private LiveId? _lastPlayedLive;
        private PlaylistItem? _lastPlayedItem;


        public void Dispose()
        {
            SaveDisplayMode();
        }



        void SaveDisplayMode()
        {
            _localObjectStorage.Save(nameof(PlayerDisplayView), DisplayMode);
        }

        public PlayerDisplayView ReadDisplayMode()
        {
            return _localObjectStorage.Read<PlayerDisplayView>(nameof(PlayerDisplayView));
        }


        public async void Receive(ChangePlayerDisplayViewRequestMessage message)
        {
            var mode = DisplayMode == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
            var nowViewChanging = DisplayMode != mode;
            DisplayMode = mode;
            SaveDisplayMode();

            if (_lastPlayedItem != null)
            {
                await VideoPlayAsync(_lastPlayedItem, mode, nowViewChanging);
            }
            else if (_lastPlayedLive != null)
            {
                await PlayLiveAsync(_lastPlayedLive.Value, mode, nowViewChanging);
            }
        }

        public void Receive(VideoPlayRequestMessage message)
        {
            _lastPlayedLive = null;

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

        public async void Receive(PlayerPlayLiveRequestMessage message)
        {
            // 生放送視聴した場合には動画視聴の情報をクリア
            _lastPlayedItem = null;

            // 生放送再生開始前に動画の再生を中止
            if (DisplayMode == PlayerDisplayView.PrimaryView)
            {
                await _primaryViewPlayerManager.ClearVideoPlayerAsync();
            }
            else 
            {
                await _secondaryPlayerManager.ClearVideoPlayerAsync();
            }

            await PlayLiveAsync(message.Value.LiveId, DisplayMode, false);
        }




        // 動画はHohoemaPlaylistPlayerのモデルによって動画コンテンツの読み込み管理を行っているため
        // ViewModelの動画ページへのナビゲーションは一回だけでいい

        private async Task<VideoPlayRequestMessageData> VideoPlayAsync(PlaylistItem playlistItem, PlayerDisplayView displayMode, bool nowViewChanging = false)
        {
            static async Task Play(HohoemaPlaylistPlayer player, PlaylistItem playlistItem, TimeSpan? initialPosition)
            {
                await player.PlayAsync(playlistItem, initialPosition);
            }

            var pageName = nameof(Presentation.Views.Player.VideoPlayerPage);

            using var _ = await _asyncLock.LockAsync(default);            
            if (displayMode == PlayerDisplayView.PrimaryView)
            {
                TimeSpan? initialPosition = null;
                if (_secondaryPlayerManager.PlaylistPlayer != null
                    && _secondaryPlayerManager.PlaylistPlayer.CurrentPlaylistItem == playlistItem)
                {
                    initialPosition = _secondaryPlayerManager.PlaylistPlayer.GetCurrentPlaybackPosition();
                }

                await _secondaryPlayerManager.CloseAsync().ConfigureAwait(false);

                if (_primaryViewPlayerManager.DisplayMode != PrimaryPlayerDisplayMode.Close
                    || nowViewChanging
                    || _primaryViewPlayerManager.LastNavigatedPageName != pageName
                    )
                {
                    System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                    await _primaryViewPlayerManager.NavigationAsync(pageName, null);
                }

                await Play(_primaryViewPlayerManager.PlaylistPlayer, playlistItem, initialPosition);

                _primaryViewPlayerManager.SetTitle(await ResolveVideoContentNameAsync(playlistItem.ItemId));

                _lastPlayedItem = playlistItem;

                return new VideoPlayRequestMessageData() { IsSuccess = true };
            }
            else
            {
                TimeSpan? initialPosition = null;
                if (_primaryViewPlayerManager.PlaylistPlayer != null 
                    && _primaryViewPlayerManager.PlaylistPlayer.CurrentPlaylistItem == playlistItem)
                {
                    initialPosition = _primaryViewPlayerManager.PlaylistPlayer.GetCurrentPlaybackPosition();
                }

                await _primaryViewPlayerManager.CloseAsync();

                if (nowViewChanging
                    || _secondaryPlayerManager.LastNavigationPageName != pageName
                )
                {
                    await _secondaryPlayerManager.NavigationAsync(pageName, null).ConfigureAwait(false);
                }

                await _secondaryPlayerManager.ShowSecondaryViewAsync();

                await Play(_secondaryPlayerManager.PlaylistPlayer, playlistItem, initialPosition);

                _secondaryPlayerManager.SetTitle(await ResolveVideoContentNameAsync(playlistItem.ItemId));

                _lastPlayedItem = playlistItem;

                return new VideoPlayRequestMessageData() { IsSuccess = true };
            }
           
        }


        // 生放送はViewModelのナビゲーションベースでコンテンツ読み込みを管理しているため
        // 新しいコンテンツごとにページをナビゲーションし直す必要がある

        async Task PlayLiveAsync(LiveId liveId, PlayerDisplayView displayMode, bool nowViewChanging)
        {
            var pageName = nameof(Presentation.Views.Player.LivePlayerPage);
            var parameters = new NavigationParameters("id=" + liveId);

            using var _ = await _asyncLock.LockAsync(default);
            if (displayMode == PlayerDisplayView.PrimaryView)
            {
                if (_primaryViewPlayerManager.DisplayMode != PrimaryPlayerDisplayMode.Close)
                {
                    if (!nowViewChanging
                        && _lastPlayedLive == liveId
                    )
                    {
                        System.Diagnostics.Debug.WriteLine("Navigation skiped. (same LiveId)");
                        return;
                    }
                }

                await _secondaryPlayerManager.CloseAsync().ConfigureAwait(false);

                await _primaryViewPlayerManager.NavigationAsync(pageName, parameters);

                _primaryViewPlayerManager.SetTitle(await ResolveLiveContentNameAsync(liveId));

                _lastPlayedLive = liveId;
            }
            else
            {
                if (DisplayMode == displayMode
                    && _secondaryPlayerManager.LastNavigationPageName == pageName
                    && _lastPlayedLive == liveId)
                {
                    System.Diagnostics.Debug.WriteLine("Navigation skiped. (same LiveId)");
                    await _secondaryPlayerManager.ShowSecondaryViewAsync();
                    return;
                }

                await _primaryViewPlayerManager.CloseAsync();

                await _secondaryPlayerManager.NavigationAsync(pageName, parameters).ConfigureAwait(false);

                _secondaryPlayerManager.SetTitle(await ResolveLiveContentNameAsync(liveId));

                _lastPlayedLive = liveId;
            }            
        }


        async ValueTask<string> ResolveVideoContentNameAsync(VideoId videoId)
        {
            return await _nicoVideoProvider.ResolveVideoTitleAsync(videoId);
        }

        async ValueTask<string> ResolveLiveContentNameAsync(LiveId liveId)
        {
            return await _nicoLiveProvider.ResolveLiveContentNameAsync(liveId);
        }
    }
}

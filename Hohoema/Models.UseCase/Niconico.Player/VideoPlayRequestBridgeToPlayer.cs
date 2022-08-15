using Hohoema.Presentation.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Uwp.Helpers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Helpers;
using Hohoema.Models.UseCase.Niconico.Player.Events;
using Hohoema.Models.Domain.Playlist;
using NiconicoToolkit.Video;
using Hohoema.Models.Infrastructure;
using Hohoema.Models.Domain.Player;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Niconico.Live;
using NiconicoToolkit.Live;
using Hohoema.Models.UseCase.Playlist;
using Microsoft.Toolkit.Diagnostics;
using Reactive.Bindings.Extensions;
using Hohoema.Models.Domain.Application;
using Hohoema.Presentation.Navigations;

namespace Hohoema.Models.UseCase.Niconico.Player
{
    public sealed class VideoPlayRequestBridgeToPlayer 
        : IDisposable,
        IRecipient<VideoPlayRequestMessage>,
        IRecipient<PlayerPlayLiveRequestMessage>,
        IRecipient<ChangePlayerDisplayViewRequestMessage>
    {
        private readonly IMessenger _messenger;
        private readonly AppearanceSettings _appearanceSettings;
        private readonly Lazy<AppWindowSecondaryViewPlayerManager> __secondaryPlayerManager;
        private readonly Lazy<PrimaryViewPlayerManager> __primaryViewPlayerManager;

        private AppWindowSecondaryViewPlayerManager _secondaryPlayerManager => __secondaryPlayerManager.Value;
        private PrimaryViewPlayerManager _primaryViewPlayerManager => __primaryViewPlayerManager.Value;

        private readonly LocalObjectStorageHelper _localObjectStorage;
        private readonly QueuePlaylist _queuePlaylist;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly NicoLiveProvider _nicoLiveProvider;
        private readonly PlaylistItemsSourceResolver _playlistItemsSourceResolver;
        private readonly ApplicationLayoutManager _applicationLayoutManager;
        AsyncLock _asyncLock = new AsyncLock();

        public VideoPlayRequestBridgeToPlayer(
            IMessenger messenger,
            AppearanceSettings appearanceSettings,
            Lazy<AppWindowSecondaryViewPlayerManager> secondaryPlayerViewManager,
            Lazy<PrimaryViewPlayerManager> primaryViewPlayerManager,
            LocalObjectStorageHelper localObjectStorageHelper,
            QueuePlaylist queuePlaylist,
            NicoVideoProvider nicoVideoProvider,
            NicoLiveProvider nicoLiveProvider,
            PlaylistItemsSourceResolver playlistItemsSourceResolver,
            ApplicationLayoutManager applicationLayoutManager
            )
        {
            _messenger = messenger;
            _appearanceSettings = appearanceSettings;
            __secondaryPlayerManager = secondaryPlayerViewManager;
            __primaryViewPlayerManager = primaryViewPlayerManager;
            _localObjectStorage = localObjectStorageHelper;
            _queuePlaylist = queuePlaylist;
            _nicoVideoProvider = nicoVideoProvider;
            _nicoLiveProvider = nicoLiveProvider;
            _playlistItemsSourceResolver = playlistItemsSourceResolver;
            _applicationLayoutManager = applicationLayoutManager;
            _messenger.Register<VideoPlayRequestMessage>(this);
            _messenger.Register<PlayerPlayLiveRequestMessage>(this);
            _messenger.Register<ChangePlayerDisplayViewRequestMessage>(this);
        }


        
        private LiveId? _lastPlayedLive;

        private IPlaylist _lastPlaylist;
        private IPlaylistSortOption _lastPlaylistSortOption;
        private IVideoContent? _lastPlayedItem;

        private IDisposable _titleUpdater;

        public void Dispose()
        {
            
        }

        public async void Receive(ChangePlayerDisplayViewRequestMessage message)
        {
            var mode = _appearanceSettings.PlayerDisplayView == PlayerDisplayView.PrimaryView ? PlayerDisplayView.SecondaryView : PlayerDisplayView.PrimaryView;
            var nowViewChanging = _appearanceSettings.PlayerDisplayView != mode;
            _appearanceSettings.PlayerDisplayView = mode;
            
            if (_lastPlayedItem != null)
            {
                await VideoPlayAsync(_lastPlaylist, _lastPlaylistSortOption, _lastPlayedItem, mode, nowViewChanging);
            }
            else if (_lastPlayedLive != null)
            {
                await PlayLiveAsync(_lastPlayedLive.Value, mode, nowViewChanging);
            }
        }

        public void Receive(VideoPlayRequestMessage message)
        {
            _lastPlayedLive = null;

            async Task<VideoPlayRequestMessageData> ResolvePlay(VideoPlayRequestMessage message)
            {
                IPlaylist playlist = null;
                IPlaylistSortOption sortOption = message.SortOptions;
                if (message.PlayWithQueue ?? false)
                {
                    playlist = _queuePlaylist;
                }
                else if (message.Playlist != null)
                {
                    playlist = message.Playlist;
                }
                else if (message.PlaylistId == QueuePlaylist.Id.Id && message.PlaylistOrigin == PlaylistItemsSourceOrigin.Local)
                {
                    playlist = _queuePlaylist;
                    sortOption = QueuePlaylist.DefaultSortOption;
                }
                else if (message.PlaylistId != null)
                {
                    Guard.IsNotNull(message.PlaylistId, nameof(message.PlaylistId));
                    Guard.IsNotNull(message.PlaylistOrigin, nameof(message.PlaylistOrigin));

                    var playlistId = new PlaylistId() { Id = message.PlaylistId, Origin = message.PlaylistOrigin.Value };
                    var factory = _playlistItemsSourceResolver.Resolve(playlistId.Origin);

                    playlist = await factory.Create(playlistId);
                }

                if (sortOption is null && message.PlaylistSortOptionsAsString is not null)
                {
                    var factory = _playlistItemsSourceResolver.Resolve(playlist.PlaylistId.Origin);
                    sortOption = factory.DeserializeSortOptions(message.PlaylistSortOptionsAsString);
                }
                else if (sortOption == null && playlist != null)
                {
                    sortOption = playlist.DefaultSortOption;
                }

                IVideoContent videoResolved = null;
                if (message.PlaylistItem != null)
                {
                    videoResolved = message.PlaylistItem;
                }
                else if (playlist?.PlaylistId == QueuePlaylist.Id)
                {
                    if (message.VideoId is not null and VideoId videoId)
                    {
                        if (!_queuePlaylist.Contains(message.VideoId.Value))
                        {
                            videoResolved = _nicoVideoProvider.GetCachedVideoInfo(message.VideoId.Value);
                            _queuePlaylist.Insert(0, videoResolved);
                        }
                        else
                        {
                            var queueItem = _queuePlaylist.First(x => x.VideoId == message.VideoId.Value);
                            videoResolved = queueItem;
                        }
                    }
                }
                else if (message.VideoId is not null and VideoId videoId)
                {
                    videoResolved = _nicoVideoProvider.GetCachedVideoInfo(message.VideoId.Value);
                }

                Guard.IsTrue(videoResolved != null || playlist != null, "Video and Playlist empty both of values. require not null each value or not null both.");

                return await VideoPlayAsync(playlist, sortOption, videoResolved, _appearanceSettings.PlayerDisplayView);
            }

            message.Reply(ResolvePlay(message));
        }

        public async void Receive(PlayerPlayLiveRequestMessage message)
        {
            // 生放送視聴した場合には動画視聴の情報をクリア
            _lastPlayedItem = null;

            // 生放送再生開始前に動画の再生を中止
            if (_appearanceSettings.PlayerDisplayView == PlayerDisplayView.PrimaryView)
            {
                await _primaryViewPlayerManager.ClearVideoPlayerAsync();
            }
            else 
            {
                await _secondaryPlayerManager.ClearVideoPlayerAsync();
            }

            await PlayLiveAsync(message.Value.LiveId, _appearanceSettings.PlayerDisplayView, false);
        }




        // 動画はHohoemaPlaylistPlayerのモデルによって動画コンテンツの読み込み管理を行っているため
        // ViewModelの動画ページへのナビゲーションは一回だけでいい

        private async Task<VideoPlayRequestMessageData> VideoPlayAsync(IPlaylist playlist, IPlaylistSortOption sortOption, IVideoContent playlistItem, PlayerDisplayView displayMode, bool nowViewChanging = false)
        {
            static Task<bool> Play(HohoemaPlaylistPlayer player, IPlaylist playlist, IPlaylistSortOption sortOption, IVideoContent playlistItem, TimeSpan? initialPosition)
            {
                if (playlistItem != null && playlist != null)
                {
                    Guard.IsAssignableToType<ISortablePlaylist>(playlist, nameof(playlist));
                    return player.PlayAsync(playlist as ISortablePlaylist, sortOption, playlistItem, initialPosition);
                }
                else if (playlistItem != null)
                {
                    return player.PlayWithoutPlaylistAsync(playlistItem, initialPosition);
                }
                else if (playlist != null)
                {
                    return player.PlayAsync(playlist, sortOption);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            
            using var _ = await _asyncLock.LockAsync(default);

            _titleUpdater?.Dispose();
            _titleUpdater = null;

            IPlayerView sourcePlayerView = displayMode == PlayerDisplayView.PrimaryView ? _primaryViewPlayerManager : _secondaryPlayerManager;
            IPlayerView destPlayerView = displayMode == PlayerDisplayView.PrimaryView ? _secondaryPlayerManager : _primaryViewPlayerManager;

            Guard.IsReferenceNotEqualTo(sourcePlayerView, destPlayerView, nameof(sourcePlayerView));

            TimeSpan? initialPosition = null;
            if (destPlayerView.PlaylistPlayer != null
                && destPlayerView.PlaylistPlayer.CurrentPlaylistItem == playlistItem)
            {
                initialPosition = destPlayerView.PlaylistPlayer.GetCurrentPlaybackPosition();
            }

            await destPlayerView.CloseAsync().ConfigureAwait(false);

            if (nowViewChanging
                || sourcePlayerView.LastNavigatedPageName != nameof(Presentation.Views.Player.VideoPlayerPage)
                )
            {
                System.Diagnostics.Debug.WriteLine("Navigation skiped. (same page name and parameter)");
                await sourcePlayerView.NavigationAsync(nameof(Presentation.Views.Player.VideoPlayerPage), null);
            }

            if (playlist.IsQueuePlaylist()
                && nowViewChanging
                && playlistItem != null
                && !_queuePlaylist.Contains(playlistItem)
                )
            {
                _queuePlaylist.Insert(0, playlistItem);
            }

            await sourcePlayerView.ShowAsync();

            if (_appearanceSettings.PlayerDisplayView == PlayerDisplayView.PrimaryView
                && _applicationLayoutManager.InteractionMode == ApplicationInteractionMode.Touch
                && await sourcePlayerView.IsWindowFilledScreenAsync()
                && await sourcePlayerView.GetDisplayModeAsync() == PlayerDisplayMode.FillWindow
                )
            {
                await sourcePlayerView.TrySetDisplayModeAsync(PlayerDisplayMode.FullScreen);
            }

            _titleUpdater = sourcePlayerView.PlaylistPlayer.ObserveProperty(x => x.CurrentPlaylistItem)
                .Subscribe(async playlistItem =>
                {
                    if (playlistItem != null)
                    {
                        sourcePlayerView.SetTitle(await ResolveVideoContentNameAsync(sourcePlayerView.PlaylistPlayer.CurrentPlaylistItem.VideoId));
                    }
                    else
                    {
                        sourcePlayerView.SetTitle(string.Empty);
                    }
                });


            if (await Play(sourcePlayerView.PlaylistPlayer, playlist, sortOption, playlistItem, initialPosition))
            {
                _lastPlaylist = playlist;
                _lastPlaylistSortOption = sortOption;
                _lastPlayedItem = playlistItem;
                return new VideoPlayRequestMessageData() { IsSuccess = true };
            }
            else
            {
                _lastPlaylist = null;
                _lastPlaylistSortOption = null;
                _lastPlayedItem = null;
                
                return new VideoPlayRequestMessageData() { IsSuccess = false };
            }
        }

        // 生放送はViewModelのナビゲーションベースでコンテンツ読み込みを管理しているため
        // 新しいコンテンツごとにページをナビゲーションし直す必要がある

        async Task PlayLiveAsync(LiveId liveId, PlayerDisplayView displayMode, bool nowViewChanging)
        {
            var pageName = nameof(Presentation.Views.Player.LivePlayerPage);
            var parameters = new NavigationParameters(("id", liveId));

            using var _ = await _asyncLock.LockAsync(default);
            if (displayMode == PlayerDisplayView.PrimaryView)
            {
                if (_primaryViewPlayerManager.DisplayMode != PlayerDisplayMode.Close)
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

                await _primaryViewPlayerManager.ShowAsync();
                await _primaryViewPlayerManager.NavigationAsync(pageName, parameters);

                _primaryViewPlayerManager.SetTitle(await ResolveLiveContentNameAsync(liveId));

                _lastPlayedLive = liveId;
            }
            else
            {
                if (_appearanceSettings.PlayerDisplayView == displayMode
                    && _secondaryPlayerManager.LastNavigatedPageName == pageName
                    && _lastPlayedLive == liveId)
                {
                    System.Diagnostics.Debug.WriteLine("Navigation skiped. (same LiveId)");
                    await _secondaryPlayerManager.ShowAsync();
                    return;
                }

                await _primaryViewPlayerManager.CloseAsync();

                await _secondaryPlayerManager.ShowAsync();
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

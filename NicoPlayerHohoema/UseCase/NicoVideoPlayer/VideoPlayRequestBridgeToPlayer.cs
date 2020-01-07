using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.UseCase.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.NicoVideoPlayer
{
    public sealed class VideoPlayRequestBridgeToPlayer : IDisposable
    {
        private readonly PlayerViewManager _playerViewManager;
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public VideoPlayRequestBridgeToPlayer(
            PlayerViewManager playerViewManager,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            _playerViewManager = playerViewManager;
            _hohoemaPlaylist = hohoemaPlaylist;

            _hohoemaPlaylist.PlayRequested += _hohoemaPlaylist_PlayRequested;
        }

        private void _hohoemaPlaylist_PlayRequested(object sender, PlayVideoRequestedEventArgs e)
        {
            _ = _playerViewManager.PlayWithCurrentPlayerView(e.RequestVideoItem);
        }

        public void Dispose()
        {
            _hohoemaPlaylist.PlayRequested -= _hohoemaPlaylist_PlayRequested;
        }
    }
}

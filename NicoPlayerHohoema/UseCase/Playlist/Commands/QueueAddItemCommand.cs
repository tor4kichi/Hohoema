using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Services;
using NicoPlayerHohoema.Services.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.Playlist.Commands
{
    public sealed class QueueAddItemCommand : VideoContentSelectionCommandBase
    {
        private readonly HohoemaPlaylist _hohoemaPlaylist;
        private readonly PrimaryViewPlayerManager _primaryViewPlayerManager;
        private readonly ScondaryViewPlayerManager _scondaryViewPlayerManager;

        public QueueAddItemCommand(
            HohoemaPlaylist hohoemaPlaylist,
            PrimaryViewPlayerManager primaryViewPlayerManager,
            ScondaryViewPlayerManager scondaryViewPlayerManager
            )
        {
            _hohoemaPlaylist = hohoemaPlaylist;
            _primaryViewPlayerManager = primaryViewPlayerManager;
            _scondaryViewPlayerManager = scondaryViewPlayerManager;
        }

        protected override void Execute(IVideoContent content)
        {
            bool isQueueEmpty = !_hohoemaPlaylist.QueuePlaylist.Any();
            _hohoemaPlaylist.AddQueue(content);

            if (isQueueEmpty)
            {
                var displayViewMode = NicoVideoPlayer.VideoPlayRequestBridgeToPlayer.ReadDisplayMode();
                if (displayViewMode == NicoVideoPlayer.PlayerDisplayView.SecondaryView)
                {
                    if (!_scondaryViewPlayerManager.IsShowSecondaryView)
                    {
                        _scondaryViewPlayerManager.OnceSurpressActivation();

                        _hohoemaPlaylist.Play(content);
                    }
                }
                else
                {
                    if (_primaryViewPlayerManager.DisplayMode == PrimaryPlayerDisplayMode.Close)
                    {
                        _primaryViewPlayerManager.ShowWithWindowInWindow();
                        _hohoemaPlaylist.Play(content);
                    }
                }
            }
        }
    }
}

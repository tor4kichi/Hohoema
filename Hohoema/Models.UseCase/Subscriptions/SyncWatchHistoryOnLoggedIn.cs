using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico;
using Hohoema.Models.Domain.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.UseCase.NicoVideos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.UseCase.Subscriptions
{
    public sealed class SyncWatchHistoryOnLoggedIn : IDisposable
    {
        private readonly NiconicoSession _niconicoSession;
        private readonly LoginUserVideoWatchHistoryProvider _LoginUserVideoWatchHistoryProvider;
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public SyncWatchHistoryOnLoggedIn(
            NiconicoSession niconicoSession,
            LoginUserVideoWatchHistoryProvider LoginUserVideoWatchHistoryProvider,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            _niconicoSession = niconicoSession;
            _LoginUserVideoWatchHistoryProvider = LoginUserVideoWatchHistoryProvider;
            _hohoemaPlaylist = hohoemaPlaylist;

            _niconicoSession.LogIn += _niconicoSession_LogIn;
        }

        public void Dispose()
        {
            _niconicoSession.LogIn -= _niconicoSession_LogIn;
        }

        private async void _niconicoSession_LogIn(object sender, NiconicoSessionLoginEventArgs e)
        {
            await _LoginUserVideoWatchHistoryProvider.GetHistory();
        }
    }
}

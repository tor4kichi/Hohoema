using Hohoema.Models.Domain;
using Hohoema.Models.Domain.Niconico.UserFeature;
using Hohoema.Models.UseCase.Playlist;
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
        private readonly LoginUserHistoryProvider _loginUserHistoryProvider;
        private readonly HohoemaPlaylist _hohoemaPlaylist;

        public SyncWatchHistoryOnLoggedIn(
            NiconicoSession niconicoSession,
            LoginUserHistoryProvider loginUserHistoryProvider,
            HohoemaPlaylist hohoemaPlaylist
            )
        {
            _niconicoSession = niconicoSession;
            _loginUserHistoryProvider = loginUserHistoryProvider;
            _hohoemaPlaylist = hohoemaPlaylist;

            _niconicoSession.LogIn += _niconicoSession_LogIn;
        }

        public void Dispose()
        {
            _niconicoSession.LogIn -= _niconicoSession_LogIn;
        }

        private async void _niconicoSession_LogIn(object sender, NiconicoSessionLoginEventArgs e)
        {
            await _loginUserHistoryProvider.GetHistory();
        }
    }
}

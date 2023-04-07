using Hohoema.Models;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video.WatchHistory.LoginUser;
using Hohoema.Models.UseCase.Playlist;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZLogger;

namespace Hohoema.Models.UseCase.Subscriptions
{
    public sealed class SyncWatchHistoryOnLoggedIn : IDisposable
    {
        private readonly ILogger<SyncWatchHistoryOnLoggedIn> _logger;
        private readonly NiconicoSession _niconicoSession;
        private readonly LoginUserVideoWatchHistoryProvider _LoginUserVideoWatchHistoryProvider;

        public SyncWatchHistoryOnLoggedIn(
            ILoggerFactory loggerFactory,
            NiconicoSession niconicoSession,
            LoginUserVideoWatchHistoryProvider LoginUserVideoWatchHistoryProvider
            )
        {
            _logger = loggerFactory.CreateLogger<SyncWatchHistoryOnLoggedIn>();
            _niconicoSession = niconicoSession;
            _LoginUserVideoWatchHistoryProvider = LoginUserVideoWatchHistoryProvider;

            _niconicoSession.LogIn += _niconicoSession_LogIn;
        }

        public void Dispose()
        {
            _niconicoSession.LogIn -= _niconicoSession_LogIn;
        }

        private async void _niconicoSession_LogIn(object sender, NiconicoSessionLoginEventArgs e)
        {
            try
            {
                await _LoginUserVideoWatchHistoryProvider.GetHistoryAsync();
            }
            catch (Exception ex)
            {
                _logger.ZLogError(ex, "ログインユーザーの視聴履歴をアプリの視聴済みに同期する処理に失敗");
            }
        }
    }
}

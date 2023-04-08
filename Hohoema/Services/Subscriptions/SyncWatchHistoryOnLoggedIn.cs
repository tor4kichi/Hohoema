#nullable enable
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video.WatchHistory.LoginUser;
using Microsoft.Extensions.Logging;
using System;
using ZLogger;

namespace Hohoema.Services.Subscriptions;

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

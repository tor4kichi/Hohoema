#nullable enable
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Microsoft.Extensions.Logging;
using R3;
using System;
using ZLogger;

namespace Hohoema.Services.Subscriptions;

public sealed class SyncWatchHistoryOnLoggedIn : IDisposable
{
    private readonly ILogger<SyncWatchHistoryOnLoggedIn> _logger;
    private readonly NiconicoSession _niconicoSession;
    private readonly LoginUserVideoWatchHistoryProvider _LoginUserVideoWatchHistoryProvider;

    IDisposable _disposable;
    public SyncWatchHistoryOnLoggedIn(
        ILoggerFactory loggerFactory,
        NiconicoSession niconicoSession,
        LoginUserVideoWatchHistoryProvider LoginUserVideoWatchHistoryProvider
        )
    {
        _logger = loggerFactory.CreateLogger<SyncWatchHistoryOnLoggedIn>();
        _niconicoSession = niconicoSession;
        _LoginUserVideoWatchHistoryProvider = LoginUserVideoWatchHistoryProvider;

        DisposableBuilder db = new();        
        _niconicoSession.ObservePropertyChanged(x => x.IsLoggedIn)
            .SubscribeAwait(async (x, ct) =>
            {
                if (!x) { return; }
                try
                {
                    await _LoginUserVideoWatchHistoryProvider.GetHistoryAsync();
                }
                catch (Exception ex)
                {
                    _logger.ZLogError(ex, "ログインユーザーの視聴履歴をアプリの視聴済みに同期する処理に失敗");
                }
            })
            .AddTo(ref db);
        _disposable = db.Build();
    }

    bool _isDisposed = false;
    public void Dispose()
    {
        if (_isDisposed) { return; }
        _isDisposed = true;
        _disposable?.Dispose();
    }
}

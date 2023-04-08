using Hohoema.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.Infra;

public abstract class ProviderBase
{
    public ProviderBase(NiconicoSession niconicoSession)
    {
        _niconicoSession = niconicoSession;
    }

    protected NiconicoSession _niconicoSession { get; }

    protected static AsyncLock _contextLock { get; } = new AsyncLock();

    private static readonly AsyncLock _pageAccessLock = new();
    private static DateTime LastPageApiAccessTime = DateTime.MinValue;
    private static readonly TimeSpan PageAccessMinimumInterval = TimeSpan.FromSeconds(0.5);

    protected static async Task WaitNicoPageAccess(CancellationToken ct = default)
    {
        using (await _pageAccessLock.LockAsync(ct))
        {
            TimeSpan duration = DateTime.Now - LastPageApiAccessTime;
            LastPageApiAccessTime = DateTime.Now;
            if (duration < PageAccessMinimumInterval)
            {
                await Task.Delay(PageAccessMinimumInterval - duration);
            }
        }
    }
}

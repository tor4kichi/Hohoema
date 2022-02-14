using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;

namespace Hohoema.Models.Infrastructure
{
    public abstract class ProviderBase
    {
        public ProviderBase(NiconicoSession niconicoSession)
        {
            _niconicoSession = niconicoSession;
        }

        protected NiconicoSession _niconicoSession { get; }
        
        protected static AsyncLock _contextLock { get; } = new AsyncLock();      

        static AsyncLock _pageAccessLock = new AsyncLock();
        static DateTime LastPageApiAccessTime = DateTime.MinValue;
        static readonly TimeSpan PageAccessMinimumInterval = TimeSpan.FromSeconds(0.5);

        static protected async Task WaitNicoPageAccess(CancellationToken ct = default)
        {
            using (await _pageAccessLock.LockAsync(ct))
            {
                var duration = DateTime.Now - LastPageApiAccessTime;
                LastPageApiAccessTime = DateTime.Now;
                if (duration < PageAccessMinimumInterval)
                {
                    await Task.Delay(PageAccessMinimumInterval - duration);
                }
            }
        }
    }
}

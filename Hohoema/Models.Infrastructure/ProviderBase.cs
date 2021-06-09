using Mntone.Nico2;
using Hohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Threading;
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
        
        protected static FastAsyncLock _contextLock { get; } = new FastAsyncLock();
        private NiconicoContext _context => _niconicoSession.Context;

        protected async Task<T> ContextActionAsync<T>(Func<NiconicoContext, Task<T>> taskFactory, CancellationToken ct = default)
        {
            using (await _contextLock.LockAsync(ct))
            {
                return await taskFactory.Invoke(_context);
            }
        }

        protected async Task ContextActionAsync(Func<NiconicoContext, Task> taskFactory, CancellationToken ct = default)
        {
            using (await _contextLock.LockAsync(ct))
            {
                await taskFactory.Invoke(_context);
            }
        }

        protected async Task<T> ContextActionWithPageAccessWaitAsync<T>(Func<NiconicoContext, Task<T>> taskFactory, CancellationToken ct = default)
        {
            await WaitNicoPageAccess(ct);

            return await ContextActionAsync(taskFactory, ct);
        }

        protected async Task ContextActionWithPageAccessWaitAsync(Func<NiconicoContext, Task> taskFactory, CancellationToken ct = default)
        {
            await WaitNicoPageAccess(ct);

            await ContextActionAsync(taskFactory, ct);
        }

        static FastAsyncLock _pageAccessLock = new FastAsyncLock();
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

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
            NiconicoSession = niconicoSession;
        }

        public NiconicoSession NiconicoSession { get; }

        protected static FastAsyncLock ContextLock { get; } = new FastAsyncLock();
        private NiconicoContext Context => NiconicoSession.Context;

        protected async Task<T> ContextActionAsync<T>(Func<NiconicoContext, Task<T>> taskFactory, CancellationToken ct = default)
        {
            using (await ContextLock.LockAsync(ct))
            {
                return await taskFactory.Invoke(Context);
            }
        }

        protected async Task ContextActionAsync(Func<NiconicoContext, Task> taskFactory, CancellationToken ct = default)
        {
            using (await ContextLock.LockAsync(ct))
            {
                await taskFactory.Invoke(Context);
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



        static FastAsyncLock _NicoPageAccessLock { get; } = new FastAsyncLock();
        static DateTime LastPageApiAccessTime = DateTime.MinValue;
        static readonly TimeSpan PageAccessMinimumInterval = TimeSpan.FromSeconds(0.5);


        static private async Task WaitNicoPageAccess(CancellationToken ct)
        {
            using (await _NicoPageAccessLock.LockAsync(ct))
            {
                var duration = DateTime.Now - LastPageApiAccessTime;
                if (duration < PageAccessMinimumInterval)
                {
                    await Task.Delay(PageAccessMinimumInterval - duration);
                }

                LastPageApiAccessTime = DateTime.Now;
            }
        }


    }
}

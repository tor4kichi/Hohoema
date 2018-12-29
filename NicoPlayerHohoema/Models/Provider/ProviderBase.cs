using Mntone.Nico2;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public abstract class ProviderBase
    {
        public ProviderBase(NiconicoSession niconicoSession)
        {
            NiconicoSession = niconicoSession;
        }

        public NiconicoSession NiconicoSession { get; }

        protected static AsyncLock ContextLock { get; } = new AsyncLock();
        private NiconicoContext Context => NiconicoSession.Context;

        protected async Task<T> ContextActionAsync<T>(Func<NiconicoContext, Task<T>> taskFactory)
        {
            using (await ContextLock.LockAsync())
            {
                return await taskFactory.Invoke(Context);
            }
        }

        protected async Task ContextActionAsync(Func<NiconicoContext, Task> taskFactory)
        {
            using (await ContextLock.LockAsync())
            {
                await taskFactory.Invoke(Context);
            }
        }

        protected async Task<T> ContextActionWithPageAccessWaitAsync<T>(Func<NiconicoContext, Task<T>> taskFactory)
        {
            await WaitNicoPageAccess();

            return await ContextActionAsync(taskFactory);
        }

        protected async Task ContextActionWithPageAccessWaitAsync(Func<NiconicoContext, Task> taskFactory)
        {
            await WaitNicoPageAccess();

            await ContextActionAsync(taskFactory);
        }



        static AsyncLock _NicoPageAccessLock { get; } = new AsyncLock();
        static DateTime LastPageApiAccessTime = DateTime.MinValue;
        static readonly TimeSpan PageAccessMinimumInterval = TimeSpan.FromSeconds(0.5);


        static private async Task WaitNicoPageAccess()
        {
            using (await _NicoPageAccessLock.LockAsync())
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

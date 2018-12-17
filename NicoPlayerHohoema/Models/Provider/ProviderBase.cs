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

        protected NiconicoContext Context => NiconicoSession.Context;



        static AsyncLock _NicoPageAccessLock = new AsyncLock();
        static DateTime LastPageApiAccessTime = DateTime.MinValue;
        static readonly TimeSpan PageAccessMinimumInterval = TimeSpan.FromSeconds(0.5);

        static protected async Task WaitNicoPageAccess()
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

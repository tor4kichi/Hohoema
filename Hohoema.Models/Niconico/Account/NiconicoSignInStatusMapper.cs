using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Account
{
    internal static class NiconicoSignInStatusMapper
    {
        public static NiconicoSignInStatus ToModelNiconicoSignInStatus(this Mntone.Nico2.NiconicoSignInStatus status)
        {
            return (NiconicoSignInStatus)status;
        }

        public static Mntone.Nico2.NiconicoSignInStatus ToInfrastructureNiconicoSignInStatus(this NiconicoSignInStatus status)
        {
            return (Mntone.Nico2.NiconicoSignInStatus)status;
        }
    }
}

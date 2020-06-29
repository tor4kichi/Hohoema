using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico
{
    public static class NiconicoContentIdExtensions
    {
        public static bool IsVideoId(this string maybeVideoId)
        {
            return maybeVideoId != null ? Mntone.Nico2.NiconicoRegex.IsVideoId(maybeVideoId) : false;
        }

        public static bool IsLiveId(this string maybeLiveId)
        {
            return maybeLiveId != null ? Mntone.Nico2.NiconicoRegex.IsLiveId(maybeLiveId) : false;
        }
    }
}

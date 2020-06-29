using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    internal static class CommunityTypeMapper
    {
        public static CommunityType ToModelCommunityType(this Mntone.Nico2.Live.CommunityType communityType)
        {
            return communityType switch
            {
                Mntone.Nico2.Live.CommunityType.Official => CommunityType.Official,
                Mntone.Nico2.Live.CommunityType.Community => CommunityType.Community,
                Mntone.Nico2.Live.CommunityType.Channel => CommunityType.Channel,
                _ => throw new NotSupportedException(communityType.ToString()),
            };
        }

        public static Mntone.Nico2.Live.CommunityType ToInfrastructureCommunityType(this CommunityType providerType)
        {
            return providerType switch
            {
                CommunityType.Official => Mntone.Nico2.Live.CommunityType.Official,
                CommunityType.Community => Mntone.Nico2.Live.CommunityType.Community,
                CommunityType.Channel => Mntone.Nico2.Live.CommunityType.Channel,
                _ => throw new NotSupportedException(providerType.ToString())
            };
        }
    }
}

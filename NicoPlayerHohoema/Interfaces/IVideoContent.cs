using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Interfaces
{
    public interface IVideoContent : INiconicoContent
    {
        string ProviderId { get; }
        string ProviderName { get; }
        Mntone.Nico2.Videos.Thumbnail.UserType ProviderType { get; }

        Interfaces.IMylist OnwerPlaylist { get; }
    }
}

using NicoPlayerHohoema.Models.Niconico;
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
        UserType ProviderType { get; }

        Interfaces.IMylist OnwerPlaylist { get; }
    }
}

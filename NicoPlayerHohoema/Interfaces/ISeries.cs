using Mntone.Nico2.Users.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Interfaces
{
    public interface ISeries
    {
        string Id { get; }
        string Title { get; }
        bool IsListed { get; }
        string Description { get; }
        string ThumbnailUrl { get; }
        int ItemsCount { get; }

        SeriesProviderType ProviderType { get; }
        string ProviderId { get; }
    }
}

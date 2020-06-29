using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Search
{
    internal static class LiveSearchSortTypeMapper
    {
        public static LiveSearchSortType ToModelLiveSearchSortType(this Mntone.Nico2.Searches.Live.LiveSearchSortType liveSearchSortType)
        {
            return (LiveSearchSortType)liveSearchSortType;
        }

        public static Mntone.Nico2.Searches.Live.LiveSearchSortType ToInfrastructureLiveSearchSortType(this LiveSearchSortType liveSearchSortType)
        {
            return (Mntone.Nico2.Searches.Live.LiveSearchSortType)liveSearchSortType;
        }
    }
}

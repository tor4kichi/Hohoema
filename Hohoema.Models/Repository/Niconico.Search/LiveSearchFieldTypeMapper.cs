using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Search
{
    internal static class LiveSearchFieldTypeMapper
    {
        public static LiveSearchFieldType ToModelSearchFieldType(this Mntone.Nico2.Searches.Live.LiveSearchFieldType liveSearchFieldType)
        {
            return (LiveSearchFieldType)liveSearchFieldType;
        }

        public static Mntone.Nico2.Searches.Live.LiveSearchFieldType ToInfrastructureSearchFieldType(this LiveSearchFieldType liveSearchFieldType)
        {
            return (Mntone.Nico2.Searches.Live.LiveSearchFieldType)liveSearchFieldType;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Search
{
    internal static class SearchTargetTypeMapper
    {
        public static SearchTargetType ToModelSearchTargetType(this Mntone.Nico2.Searches.SearchTargetType searchTargetType)
        {
            return (SearchTargetType)searchTargetType;
        }

        public static Mntone.Nico2.Searches.SearchTargetType ToInfrastructureSearchTargetType(this SearchTargetType searchTargetType)
        {
            return (Mntone.Nico2.Searches.SearchTargetType)searchTargetType;
        }
    }
}

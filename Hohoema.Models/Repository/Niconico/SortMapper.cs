using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{
    internal static class SortMapper
    {
        public static Sort ToModelSort(this Mntone.Nico2.Sort sort)
        {
            return (Sort)sort;
        }

        public static Mntone.Nico2.Sort ToInfrastructureSort(this Sort sort)
        {
            return (Mntone.Nico2.Sort)sort;
        }
    }
}

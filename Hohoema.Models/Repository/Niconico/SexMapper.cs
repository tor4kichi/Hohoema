using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{
    internal static class SexMapper
    {
        public static Sex ToModelSex(this Mntone.Nico2.Sex sex)
        {
            return (Sex)sex;
        }

        public static Mntone.Nico2.Sex ToInfrastructure(this Sex sex)
        {
            return (Mntone.Nico2.Sex)sex;
        }
    }
}

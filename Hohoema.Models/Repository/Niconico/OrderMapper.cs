using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico
{
    internal static class OrderMapper
    {
        public static Order ToModelOrder(this Mntone.Nico2.Order order)
        {
            return (Order)order;
        }

        public static Mntone.Nico2.Order ToInfrastructureOrder(this Order order)
        {
            return (Mntone.Nico2.Order)order;
        }

    }
}

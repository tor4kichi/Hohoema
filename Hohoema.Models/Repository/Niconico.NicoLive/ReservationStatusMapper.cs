using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    internal static class ReservationStatusMapper
    {
        public static ReservationStatus ToModelReservationStatus(this Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus reservationStatus)
        {
            return reservationStatus switch
            {
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.FIRST_WATCH => ReservationStatus.FIRST_WATCH,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.WATCH => ReservationStatus.WATCH,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.RESERVED => ReservationStatus.RESERVED,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.TSARCHIVE => ReservationStatus.TSARCHIVE,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.PRODUCT_ARCHIVE_WATCH => ReservationStatus.PRODUCT_ARCHIVE_WATCH,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.PRODUCT_ARCHIVE_TIMEOUT => ReservationStatus.PRODUCT_ARCHIVE_TIMEOUT,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.USER_TIMESHIFT_DATE_OUT => ReservationStatus.USER_TIMESHIFT_DATE_OUT,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.USE_LIMIT_DATE_OUT => ReservationStatus.USE_LIMIT_DATE_OUT,
                Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.LIMIT_DATE_OUT => ReservationStatus.LIMIT_DATE_OUT,
                _ => throw new NotSupportedException()
            };
        }

        public static Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus ToInfrastructureReservationStatus(this ReservationStatus reservationStatus)
        {
            return reservationStatus switch
            {
                ReservationStatus.FIRST_WATCH => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.FIRST_WATCH,
                ReservationStatus.WATCH => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.WATCH,
                ReservationStatus.RESERVED => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.RESERVED,
                ReservationStatus.TSARCHIVE => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.TSARCHIVE,
                ReservationStatus.PRODUCT_ARCHIVE_WATCH => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.PRODUCT_ARCHIVE_WATCH,
                ReservationStatus.PRODUCT_ARCHIVE_TIMEOUT => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.PRODUCT_ARCHIVE_TIMEOUT,
                ReservationStatus.USER_TIMESHIFT_DATE_OUT => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.USER_TIMESHIFT_DATE_OUT,
                ReservationStatus.USE_LIMIT_DATE_OUT => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.USE_LIMIT_DATE_OUT,
                ReservationStatus.LIMIT_DATE_OUT => Mntone.Nico2.Live.ReservationsInDetail.ReservationStatus.LIMIT_DATE_OUT,
                _ => throw new NotSupportedException(),
            };
        }
    }
}

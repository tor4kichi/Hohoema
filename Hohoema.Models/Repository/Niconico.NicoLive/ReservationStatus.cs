using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    public enum ReservationStatus
    {
        FIRST_WATCH,
        WATCH,
        RESERVED,
        TSARCHIVE,
        PRODUCT_ARCHIVE_WATCH,
        PRODUCT_ARCHIVE_TIMEOUT,
        USER_TIMESHIFT_DATE_OUT,
        USE_LIMIT_DATE_OUT,
        LIMIT_DATE_OUT,
    }
}

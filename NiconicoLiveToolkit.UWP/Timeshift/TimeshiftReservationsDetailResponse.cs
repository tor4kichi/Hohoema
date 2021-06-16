#if WINDOWS_UWP
#else
using System.Net.Http;
using System.Net.Http.Headers;
#endif

using System;
using System.Linq;

namespace NiconicoToolkit.Live.Timeshift
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

    public sealed class TimeshiftReservationsDetailResponse
    {
        public TimeshiftReservationDetailMeta Meta { get; internal set; }

        public TimeshiftReservationDetailData Data { get; internal set; }

        public bool IsSuccess => Meta.Status == "ok";
    }


    public sealed class TimeshiftReservationDetailMeta
    {
        public string Status { get; internal set; }
    }

    public sealed class TimeshiftReservationDetailData
    {
        public TimeshiftReservationDetailItem[] Items { get; internal set; }
    }

    public sealed class TimeshiftReservationDetailItem
    {
        /// <summary>
		/// ID
		/// </summary>
		public LiveId LiveId { get; internal set; }

        /// <summary>
        /// 題名
        /// </summary>
        public string Title { get; internal set; }

        /// <summary>
        /// 状態
        /// </summary>
        public string Status { get; internal set; }

        /// <summary>
        /// 未視聴か
        /// </summary>
        public bool IsUnwatched { get; internal set; }

        /// <summary>
        /// 有効期限日時
        /// </summary>
        public DateTimeOffset? ExpiredAt { get; internal set; }



        public ReservationStatus? GetReservationStatus()
        {
            return Enum.TryParse(Status, out ReservationStatus result)
                ? new ReservationStatus?(result)
                : default(ReservationStatus?)
                ;
        }

        private readonly static ReservationStatus[] OutDatedStatusList = { ReservationStatus.PRODUCT_ARCHIVE_TIMEOUT, ReservationStatus.USER_TIMESHIFT_DATE_OUT, ReservationStatus.USE_LIMIT_DATE_OUT, ReservationStatus.LIMIT_DATE_OUT };
        public bool IsOutDated
        {
            get
            {
                var status = GetReservationStatus();
                if (status != null)
                {
                    return OutDatedStatusList.Contains(status.Value);
                }
                else
                {
                    return false;
                }
            }
        }

        private readonly static ReservationStatus[] WatchAvairableStatusList = { ReservationStatus.WATCH, ReservationStatus.FIRST_WATCH, ReservationStatus.PRODUCT_ARCHIVE_WATCH, ReservationStatus.TSARCHIVE };
        public bool IsCanWatch
        {
            get
            {
                var status = GetReservationStatus();
                if (status != null)
                {
                    return WatchAvairableStatusList.Contains(status.Value);
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsReserved => GetReservationStatus() == ReservationStatus.RESERVED;
    }


}

using Mntone.Nico2.Live.ReservationsInDetail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    public sealed class TimeshiftProgram
    {
        private readonly Program _program;

        internal TimeshiftProgram(Mntone.Nico2.Live.ReservationsInDetail.Program program)
        {
            _program = program;
        }

        /// <summary>
        /// ID
        /// </summary>
        public string Id => _program.Id;

        /// <summary>
        /// 題名
        /// </summary>
        public string Title => _program.Title;

        /// <summary>
        /// 状態
        /// </summary>
        public string Status => _program.Status;

        /// <summary>
        /// 未視聴か
        /// </summary>
        public bool IsUnwatched => _program.IsUnwatched;

        /// <summary>
        /// 有効期限日時
        /// </summary>
        public DateTimeOffset ExpiredAt => _program.ExpiredAt;



        public ReservationStatus? GetReservationStatus()
        {
            return Enum.TryParse(Status, out ReservationStatus result)
                ? new ReservationStatus?(result)
                : default(ReservationStatus?)
                ;
        }

        ReservationStatus[] OutDatedStatusList = { ReservationStatus.PRODUCT_ARCHIVE_TIMEOUT, ReservationStatus.USER_TIMESHIFT_DATE_OUT, ReservationStatus.USE_LIMIT_DATE_OUT, ReservationStatus.LIMIT_DATE_OUT };
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

        ReservationStatus[] WatchAvairableStatusList = { ReservationStatus.WATCH, ReservationStatus.FIRST_WATCH, ReservationStatus.PRODUCT_ARCHIVE_WATCH, ReservationStatus.TSARCHIVE };
        
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

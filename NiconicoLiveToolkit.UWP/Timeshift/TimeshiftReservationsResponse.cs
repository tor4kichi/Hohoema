using System.Collections.Generic;

namespace NiconicoToolkit.Live.Timeshift
{
    public sealed class TimeshiftReservationsResponse : ResponseWithMeta
    {
        public TimeshiftReservationsData Data { get; set; }
    }


    public enum TimeshiftStatus
    {
        Unknown,

        TimeshiftWatch,
        TimeshiftReservation,
        TimeshiftDisable,
    }

    public sealed class TimeshiftReservationsData
    {
        public ReservationToken ReservationToken { get; internal set; }

        public List<TimeshiftReservation> Items { get; internal set; }
    }

    public sealed class TimeshiftReservation
    {
        internal TimeshiftReservation() { }

        public LiveId Id { get; internal set; }
        public string Title { get; internal set; }
        public string StatusText { get; internal set; }
        public TimeshiftStatus Status { get; internal set; }

        public bool IsCanWatch => Status is TimeshiftStatus.TimeshiftWatch;
    }
}

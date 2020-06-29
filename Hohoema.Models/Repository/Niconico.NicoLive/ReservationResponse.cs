using System;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    public sealed class ReservationResponse
    {
        private readonly Mntone.Nico2.Live.Reservation.ReservationResponse _res;

        internal ReservationResponse(Mntone.Nico2.Live.Reservation.ReservationResponse res)
        {
            _res = res;
        }

        private ReservationMeta _meta;
        public ReservationMeta Meta => _meta ??= new ReservationMeta(_res.Meta);

        private ReservationData _data;
        public ReservationData Data => _data ??= new ReservationData(_res.Data);



        public bool IsOK => Meta.Status == (int)System.Net.HttpStatusCode.OK;

        public bool IsReservationDeuplicated => Data.Description?.EndsWith("duplicated") ?? false;

        public bool IsReservationExpired => Data.Description?.EndsWith("expired general") ?? false;

        public bool IsCanOverwrite => Data.Description?.EndsWith("can overwrite") ?? false;




        public sealed class ReservationOverwrite
        {
            private Mntone.Nico2.Live.Reservation.ReservationOverwrite _overwrite;

            internal ReservationOverwrite(Mntone.Nico2.Live.Reservation.ReservationOverwrite overwrite)
            {
                _overwrite = overwrite;
            }

            public string Vid => _overwrite.Vid;

            public string Title => _overwrite.Title;
        }

        public sealed class ReservationMeta
        {
            private Mntone.Nico2.Live.Reservation.ReservationMeta _meta;

            internal ReservationMeta(Mntone.Nico2.Live.Reservation.ReservationMeta meta)
            {
                _meta = meta;
            }

            public int Status => _meta.Status;

            public string ErrorCode => _meta.ErrorCode;

        }

        public sealed class ReservationData
        {
            private Mntone.Nico2.Live.Reservation.ReservationData _data;

            internal ReservationData(Mntone.Nico2.Live.Reservation.ReservationData data)
            {
                _data = data;
            }

            public string Description => _data.Description;

            public string Uid => _data.Uid;

            public string Vid => _data.Vid;

            private ReservationOverwrite _overwrite;
            public ReservationOverwrite Overwrite => _overwrite ??= new ReservationOverwrite(_data.Overwrite);
        }
    }


}

using Hohoema.Models.Niconico;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.NicoLive
{
    public sealed class LoginUserLiveReservationProvider : ProviderBase
    {
        public LoginUserLiveReservationProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }


        public async Task<ReservationResponse> ReservtionAsync(string liveId, bool isOverwrite = false)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Live.ReservationAsync(liveId, isOverwrite);
            });

            return new ReservationResponse(res);
        }

        public async Task<ReservationsInDetailResponse> GetReservtionsAsync()
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Live.GetReservationsInDetailAsync();
            });

            return new ReservationsInDetailResponse(res);
        }

        public async Task<MyTimeshiftListData> GetTimeshiftListAsync()
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Live.GetMyTimeshiftListAsync();
            });

            return new MyTimeshiftListData(res);
        }

        public async Task<ReservationToken> GetReservationTokenAsync()
        {
            var token = await ContextActionAsync(async context =>
            {
                return await context.Live.GetReservationTokenAsync();
            });

            return new ReservationToken(token);
        }

        public async Task DeleteReservationAsync(string reservationId, ReservationToken token)
        {
            await ContextActionAsync(async context =>
            {
                await context.Live.DeleteReservationAsync(reservationId, token._token);
            });
        }

        public async Task UseReservationAsync(string liveId, ReservationToken token)
        {
            await ContextActionAsync(async context =>
            {
                await context.Live.UseReservationAsync(liveId, token._token);
            });
        }
    }
    public sealed class ReservationToken
    {
        internal readonly Mntone.Nico2.Live.Reservation.ReservationToken _token;

        internal ReservationToken(Mntone.Nico2.Live.Reservation.ReservationToken token)
        {
            _token = token;
        }

        public string Token => _token.Token;
    }
}

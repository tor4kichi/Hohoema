using Mntone.Nico2.Live.Reservation;
using Mntone.Nico2.Live.ReservationsInDetail;
using Hohoema.Models.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Live
{
    public sealed class LoginUserLiveReservationProvider : ProviderBase
    {
        public LoginUserLiveReservationProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }


        public async Task<ReservationResponse> ReservtionAsync(string liveId, bool isOverrwite = false)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.ReservationAsync(liveId, isOverrwite);
            });
        }

        public async Task<ReservationsInDetailResponse> GetReservtionsAsync()
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.GetReservationsInDetailAsync();
            });
        }

        public async Task<MyTimeshiftListData> GetTimeshiftListAsync()
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.GetMyTimeshiftListAsync();
            });
        }

        public async Task<ReservationToken> GetReservationTokenAsync()
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Live.GetReservationTokenAsync();
            });
        }

        public async Task DeleteReservationAsync(string reservationId, ReservationToken token)
        {
            await ContextActionAsync(async context =>
            {
                await context.Live.DeleteReservationAsync(reservationId, token);
            });
        }

        public async Task UseReservationAsync(string liveId, ReservationToken token)
        {
            await ContextActionAsync(async context =>
            {
                await context.Live.UseReservationAsync(liveId, token);
            });
        }
    }
}

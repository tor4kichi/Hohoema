using Mntone.Nico2.Live.Reservation;
using Mntone.Nico2.Live.ReservationsInDetail;
using System;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class LoginUserLiveReservationProvider : ProviderBase
    {
        public LoginUserLiveReservationProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<ReservationsInDetailResponse> GetReservtionsAsync()
        {

            return await Context.Live.GetReservationsInDetailAsync();
        }

        public async Task<MyTimeshiftListData> GetTimeshiftListAsync()
        {
            return await Context.Live.GetMyTimeshiftListAsync();
        }

        public async Task<ReservationToken> GetReservationTokenAsync()
        {
            return await NiconicoSession.Context.Live.GetReservationTokenAsync();
        }

        public async Task DeleteReservationAsync(string reservationId, ReservationToken token)
        {
            await Context.Live.DeleteReservationAsync(reservationId, token);
        }

        public async Task UseReservationAsync(string liveId, ReservationToken token)
        {
            await Context.Live.UseReservationAsync(liveId, token);
        }
    }
}

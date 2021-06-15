using Hohoema.Models.Infrastructure;
using NiconicoToolkit.Live.Timeshift;
using System;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Live.LoginUser
{
    public sealed class LoginUserLiveReservationProvider : ProviderBase
    {
        public LoginUserLiveReservationProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }


        private static ReservationToken _token;

        public Task<ReserveTimeshiftResponse> ReservtionAsync(string liveId, bool isOverrwite = false)
        {
            return _niconicoSession.ToolkitContext.Timeshift.ReserveTimeshiftAsync(liveId, isOverrwite);
        }

        public async Task<TimeshiftReservationsResponse> GetReservtionsAsync()
        {
            var res = await _niconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsAsync();

            _token = res?.Data?.ReservationToken;

            return res;
        }


        public Task<TimeshiftReservationsDetailResponse> GetReservtionsDetailAsync()
        {
            return _niconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsDetailAsync();
        }

        private async Task<ReservationToken> GetReservationTokenAsync(bool forceRefresh = false)
        {
            if (_token is null)
            {
                _token = await _niconicoSession.ToolkitContext.Timeshift.GetReservationTokenAsync();
            }

            if (_token == null)
            {
                throw new HohoemaExpception("Failed refresh ReservationToken.");
            }

            return _token;
        }

        public async Task DeleteReservationAsync(string liveId)
        {
            var token = await GetReservationTokenAsync();

            try
            {
                await _niconicoSession.ToolkitContext.Timeshift.DeleteTimeshiftReservationAsync(liveId, token);
            }
            catch
            {
                token = await GetReservationTokenAsync(forceRefresh: true);
                await _niconicoSession.ToolkitContext.Timeshift.DeleteTimeshiftReservationAsync(liveId, token);
            }
        }

        public async Task UseReservationAsync(string liveId)
        {
            var token = await GetReservationTokenAsync();

            try
            {
                await _niconicoSession.ToolkitContext.Timeshift.UseTimeshiftViewingAuthorityAsync(liveId, token);
            }
            catch
            {
                token = await GetReservationTokenAsync(forceRefresh: true);
                await _niconicoSession.ToolkitContext.Timeshift.UseTimeshiftViewingAuthorityAsync(liveId, token);
            }
        }
    }
}

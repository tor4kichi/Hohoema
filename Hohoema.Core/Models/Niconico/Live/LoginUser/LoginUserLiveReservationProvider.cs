#nullable enable
using Hohoema.Infra;
using NiconicoToolkit.Live.Timeshift;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Live.LoginUser;

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
        TimeshiftReservationsResponse res = await _niconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsAsync();

        //_token = res?.Data?.ReservationToken;

        return res;
    }


    public Task<TimeshiftReservationsDetailResponse> GetReservtionsDetailAsync()
    {
        return _niconicoSession.ToolkitContext.Timeshift.GetTimeshiftReservationsDetailAsync();
    }

    private async Task<ReservationToken> GetReservationTokenAsync(bool forceRefresh = false)
    {
        _token ??= await _niconicoSession.ToolkitContext.Timeshift.GetReservationTokenAsync();

        return _token ?? throw new HohoemaException("Failed refresh ReservationToken.");
    }

    public async Task DeleteReservationAsync(string liveId)
    {
        ReservationToken token = await GetReservationTokenAsync();

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
        ReservationToken token = await GetReservationTokenAsync();

        try
        {
            _ = await _niconicoSession.ToolkitContext.Timeshift.UseTimeshiftViewingAuthorityAsync(liveId, token);
        }
        catch
        {
            token = await GetReservationTokenAsync(forceRefresh: true);
            _ = await _niconicoSession.ToolkitContext.Timeshift.UseTimeshiftViewingAuthorityAsync(liveId, token);
        }
    }
}

#nullable enable
using Hohoema.Infra;
using NiconicoToolkit.NicoRepo;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.NicoRepo.LoginUser;

public sealed class LoginUserNicoRepoProvider : ProviderBase
{
    public LoginUserNicoRepoProvider(NiconicoSession niconicoSession)
        : base(niconicoSession)
    {
    }

    public Task<NicoRepoEntriesResponse> GetLoginUserNicoRepoAsync(NicoRepoType type, NicoRepoDisplayTarget target, NicoRepoEntriesResponse prevRes = null)
    {
        return _niconicoSession.ToolkitContext.NicoRepo.GetLoginUserNicoRepoEntriesAsync(type, target, prevRes?.Meta?.MinId);
    }
}

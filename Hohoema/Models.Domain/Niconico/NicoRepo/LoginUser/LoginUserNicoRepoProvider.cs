using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Infrastructure;
using NiconicoToolkit.NicoRepo;

namespace Hohoema.Models.Domain.Niconico.NicoRepo.LoginUser
{
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
}

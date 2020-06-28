using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.NicoRepo;

namespace Hohoema.Models.Provider
{
    public sealed class LoginUserNicoRepoProvider : ProviderBase
    {
        public LoginUserNicoRepoProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
        }

        public async Task<NicoRepoResponse> GetLoginUserNicoRepo(NicoRepoTimelineType type, string lastItemId = null)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.NicoRepo.GetLoginUserNicoRepo(type, lastItemId);
            });
        }
    }
}

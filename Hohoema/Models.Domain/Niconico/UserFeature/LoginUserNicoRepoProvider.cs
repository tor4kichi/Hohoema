using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.NicoRepo;
using Hohoema.Models.Infrastructure;

namespace Hohoema.Models.Domain.Niconico.UserFeature
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

        public async Task<NicoRepoEntriesResponse> GetLoginUserNicoRepoAsync(NicoRepoType type, NicoRepoDisplayTarget target, NicoRepoEntriesResponse prevRes = null)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.NicoRepo.GetLoginUserNicoRepoEntriesAsync(type, target, prevRes?.Meta?.MinId);
            });
        }
    }
}

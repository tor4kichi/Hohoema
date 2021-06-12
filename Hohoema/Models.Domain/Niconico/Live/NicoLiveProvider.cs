using Mntone.Nico2.Nicocas.Live;
using Hohoema.Database;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Live.Cas;

namespace Hohoema.Models.Domain.Niconico.Live
{
    public sealed class NicoLiveProvider : ProviderBase
    {
        private readonly NicoLiveCacheRepository _nicoLiveCacheRepository;

        public NicoLiveProvider(NiconicoSession niconicoSession,
            NicoLiveCacheRepository nicoLiveCacheRepository
            )
            : base(niconicoSession)
        {
            _nicoLiveCacheRepository = nicoLiveCacheRepository;
        }
        
        public async Task<LiveProgramResponse> GetLiveInfoAsync(string liveId)
        {
            return await _niconicoSession.ToolkitContext.Live.CasApi.GetLiveProgramAsync(liveId);
        }


    }
}

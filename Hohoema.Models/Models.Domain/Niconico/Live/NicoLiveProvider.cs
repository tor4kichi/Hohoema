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
            var res = await _niconicoSession.ToolkitContext.Live.CasApi.GetLiveProgramAsync(liveId);

            if (res.IsSuccess)
            {
                if (!_nicoLiveCacheRepository.Exists(x => x.LiveId == liveId))
                {
                    _nicoLiveCacheRepository.CreateItem(new NicoLive()
                    {
                        LiveId = liveId,
                        Title = res.Data.Title,
                        ProviderType = res.Data.ProviderType,
                        BroadcasterId = res.Data.ProviderId ?? res.Data.SocialGroupId,
                        ThumbnailUrl = res.Data.ThumbnailUrl.OriginalString,
                    });
                }
            }

            return res;
        }
    }
}

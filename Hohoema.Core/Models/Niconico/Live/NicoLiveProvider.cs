using Hohoema.Infra;
using NiconicoToolkit.Live.Cas;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Live;

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

    public async ValueTask<string> ResolveLiveContentNameAsync(string liveId)
    {
        NicoLive live = _nicoLiveCacheRepository.Get(liveId);
        if (live is not null)
        {
            return live.Title;
        }

        LiveProgramResponse info = await GetLiveInfoAsync(liveId);
        return info?.Data?.Title;
    }


    public async Task<LiveProgramResponse> GetLiveInfoAsync(string liveId)
    {
        LiveProgramResponse res = await _niconicoSession.ToolkitContext.Live.CasApi.GetLiveProgramAsync(liveId);

        if (res.IsSuccess)
        {
            if (!_nicoLiveCacheRepository.Exists(x => x.LiveId == liveId))
            {
                _ = _nicoLiveCacheRepository.CreateItem(new NicoLive()
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

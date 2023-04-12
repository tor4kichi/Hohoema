#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.Video;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Recommend;

public sealed class RecommendProvider : ProviderBase
{
    private readonly NicoVideoProvider _nicoVideoProvider;

    public RecommendProvider(
        NiconicoSession niconicoSession,
        NicoVideoProvider nicoVideoProvider
        )
        : base(niconicoSession)
    {
        _nicoVideoProvider = nicoVideoProvider;
    }

    public async Task<VideoRecommendResponse?> GetVideoRecommendAsync(VideoId videoId)
    {
        (var res, NicoVideo nicoVideo) = await _nicoVideoProvider.GetVideoInfoAsync(videoId);
        return nicoVideo.ProviderType switch
        {
            OwnerType.User => await _niconicoSession.ToolkitContext.Recommend.GetVideoRecommendForNotChannelAsync(nicoVideo.VideoAliasId),
            OwnerType.Channel => await _niconicoSession.ToolkitContext.Recommend.GetVideoRecommendForChannelAsync(nicoVideo.VideoAliasId, nicoVideo.ProviderId, res.Data.Tags.Tag.Select(x => x.Text)),
            _ => null
        };
    }
}

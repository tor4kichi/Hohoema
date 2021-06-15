using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using NiconicoToolkit.Recommend;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Niconico.Recommend
{
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

        public async Task<VideoRecommendResponse> GetVideoRecommendAsync(string videoId)
        {
            var (res, nicoVideo) = await _nicoVideoProvider.GetVideoInfoAsync(videoId);
            return nicoVideo.ProviderType switch
            {
                OwnerType.User => await _niconicoSession.ToolkitContext.Recommend.GetVideoRecommendForNotChannelAsync(nicoVideo.VideoId),
                OwnerType.Channel => await _niconicoSession.ToolkitContext.Recommend.GetVideoRecommendForChannelAsync(nicoVideo.VideoId, nicoVideo.ProviderId, res.Tags.TagInfo.Select(x => x.Tag)),
                _ => null
            };
        }
    }
}

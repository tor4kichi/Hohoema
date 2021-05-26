using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using NiconicoToolkit.Recommend;
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
            var nicoVideo = await _nicoVideoProvider.GetNicoVideoInfo(videoId);
            return await GetVideoRecommendAsync(nicoVideo);
        }

        public async Task<VideoRecommendResponse> GetVideoRecommendAsync(NicoVideo nicoVideo)
        {
            if (nicoVideo.Owner.OwnerId is null)
            {
                nicoVideo = await _nicoVideoProvider.GetNicoVideoInfo(nicoVideo.RawVideoId, true);
            }

            return nicoVideo.ProviderType switch
            {
                NicoVideoUserType.User => await NiconicoSession.ToolkitContext.Recommend.GetVideoReccommendAsync(nicoVideo.VideoId),
                NicoVideoUserType.Channel => await NiconicoSession.ToolkitContext.Recommend.GetChannelVideoReccommendAsync(nicoVideo.VideoId, nicoVideo.ProviderId, nicoVideo.Tags.Select(x => x.Tag)),
                _ => null
            };
        }
    }
}

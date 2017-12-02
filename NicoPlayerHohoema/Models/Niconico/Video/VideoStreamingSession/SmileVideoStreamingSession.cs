using Mntone.Nico2;
using System;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public class SmileVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public override NicoVideoQuality Quality { get; }

        public Uri VideoUrl { get; }

        public SmileVideoStreamingSession(Uri videoUrl, NiconicoContext context)
            : base(context)
        {
            VideoUrl = videoUrl;
            if (VideoUrl.OriginalString.EndsWith("low"))
            {
                Quality = NicoVideoQuality.Smile_Low;
            }
            else
            {
                Quality = NicoVideoQuality.Smile_Original;
            }
        }

        protected override Task<Uri> GetVideoContentUri()
        {
            return Task.FromResult(VideoUrl);
        }

    }
}

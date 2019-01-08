using Mntone.Nico2;
using System;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    public class SmileVideoStreamingSession : VideoStreamingSession, IVideoStreamingDownloadSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public override NicoVideoQuality Quality { get; }

        public Uri VideoUrl { get; }

        public SmileVideoStreamingSession(Uri videoUrl, NiconicoSession niconicoSession)
            : base(niconicoSession)
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


        public async Task<Uri> GetDownloadUrlAndSetupDonwloadSession()
        {
            var videoUri = await GetVideoContentUri();

            if (videoUri != null)
            {
                OnStartStreaming();

                return videoUri;
            }
            else
            {
                return null;
            }
        }
    }
}

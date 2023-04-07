using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;
using Hohoema.Models.VideoCache;
using Hohoema.Models.Niconico.Video;

namespace Hohoema.Models.Player.Video
{
    public class CachedVideoStreamingSession : VideoStreamingSession
    {
        private readonly VideoCacheItem _videoCacheItem;

        public override string QualityId { get; }
        public override NicoVideoQuality Quality { get; }

        public CachedVideoStreamingSession(VideoCacheItem videoCacheItem, NiconicoSession niconicoSession)
            : base(niconicoSession, null)
        {
            Quality = videoCacheItem.DownloadedVideoQuality;
            QualityId = Quality.ToString();
            _videoCacheItem = videoCacheItem;
        }

        protected override Task<MediaSource> GetPlyaingVideoMediaSource()
        {
            return _videoCacheItem.GetMediaSourceAsync();
        }


        protected override void OnStopStreaming()
        {
            base.OnStopStreaming();
        }

    }
}

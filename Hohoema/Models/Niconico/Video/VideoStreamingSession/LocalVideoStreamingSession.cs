using FFmpegInterop;
using Mntone.Nico2;
using Hohoema.Models.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Hohoema.Models
{
    public class LocalVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public override string QualityId { get; }
        public override NicoVideoQuality Quality { get; }

        public IStorageFile File { get; }

        FFmpegInteropMSS _VideoMSS;

        public LocalVideoStreamingSession(IStorageFile file, NicoVideoQuality requestQuality, NiconicoSession niconicoSession)
            : base(niconicoSession, null)
        {
            File = file;
            Quality = requestQuality;
            QualityId = requestQuality.ToString();
        }

        protected override async Task<MediaSource> GetPlyaingVideoMediaSource()
        {
            var file = await StorageFile.GetFileFromPathAsync(File.Path);
            var stream = await file.OpenReadAsync();
            var contentType = stream.ContentType;

            if (contentType == null) { throw new NotSupportedException("can not play video file. " + File.Path); }

            if (contentType == "video/mp4")
            {
                return MediaSource.CreateFromStream(stream, contentType);
            }
            else
            {
                _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, false, false);
                var mss = _VideoMSS.GetMediaStreamSource();
                mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                return MediaSource.CreateFromMediaStreamSource(mss);
            }
        }


        protected override void OnStopStreaming()
        {
            _VideoMSS?.Dispose();

            base.OnStopStreaming();
        }

    }
}

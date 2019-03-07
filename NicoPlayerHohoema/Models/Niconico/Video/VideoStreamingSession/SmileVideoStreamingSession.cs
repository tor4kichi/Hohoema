using FFmpegInterop;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models
{
    public class SmileVideoStreamingSession : VideoStreamingSession, IVideoStreamingDownloadSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public override NicoVideoQuality Quality { get; }

        public Uri VideoUrl { get; }

        FFmpegInteropMSS _VideoMSS;

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

        protected override async Task<MediaSource> GetPlyaingVideoMediaSource()
        {
            var videoUri = VideoUrl;

            MovieType videoContentType = MovieType.Mp4;
            var tempStream = await HttpSequencialAccessStream.CreateAsync(
                NiconicoSession.Context.HttpClient
                , videoUri
                );
            if (tempStream is IRandomAccessStreamWithContentType)
            {
                var contentType = (tempStream as IRandomAccessStreamWithContentType).ContentType;

                if (contentType.EndsWith("mp4"))
                {
                    videoContentType = MovieType.Mp4;
                }
                else if (contentType.EndsWith("flv"))
                {
                    videoContentType = MovieType.Flv;
                }
                else if (contentType.EndsWith("swf"))
                {
                    videoContentType = MovieType.Swf;
                }
                else
                {
                    throw new NotSupportedException($"{contentType} is not supported video format.");
                }
            }

            if (videoContentType != MovieType.Mp4)
            {
                _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(tempStream, false, false);
                var mss = _VideoMSS.GetMediaStreamSource();
                mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                return MediaSource.CreateFromMediaStreamSource(mss);
            }
            else
            {
                tempStream.Dispose();
                tempStream = null;

                return MediaSource.CreateFromUri(videoUri);
            }
        }


        public async Task<Uri> GetDownloadUrlAndSetupDonwloadSession()
        {
            var videoUri = await GetPlyaingVideoMediaSource();

            if (videoUri != null)
            {
                OnStartStreaming();

                return VideoUrl;
            }
            else
            {
                return null;
            }
        }
    }
}

using FFmpegInterop;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models
{
    public class LocalVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割


        public override NicoVideoQuality Quality { get; }

        public StorageFile File { get; }

        FFmpegInteropMSS _VideoMSS;

        public LocalVideoStreamingSession(StorageFile file, NicoVideoQuality requestQuality, NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
            File = file;
            Quality = requestQuality;
        }

        protected override async Task<MediaSource> GetPlyaingVideoMediaSource()
        {
            var videoUri = new Uri(File.Path);

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


        protected override void OnStopStreaming()
        {
            _VideoMSS?.Dispose();

            base.OnStopStreaming();
        }

    }
}

using Mntone.Nico2;
using Hohoema.Database;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Domain.Niconico.Video;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Storage.Streams;

namespace Hohoema.Models.Domain.Player.Video
{
    public class SmileVideoStreamingSession : VideoStreamingSession, IVideoStreamingDownloadSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public override string QualityId { get; }
        public override NicoVideoQuality Quality { get; }

        public Uri VideoUrl { get; }

        public SmileVideoStreamingSession(Uri videoUrl, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager.VideoSessionOwnership videoSessionOwnership)
            : base(niconicoSession, videoSessionOwnership)
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

            QualityId = Quality.ToString();
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
                throw new NotSupportedException("not supproted video type : " + videoContentType);
            }
            else
            {
                return MediaSource.CreateFromStream(tempStream, (tempStream as IRandomAccessStreamWithContentType).ContentType);
            }
        }


        public async Task<Uri> GetDownloadUrlAndSetupDownloadSession()
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

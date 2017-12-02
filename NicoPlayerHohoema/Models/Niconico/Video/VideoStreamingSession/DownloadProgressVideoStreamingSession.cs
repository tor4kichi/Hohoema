using FFmpegInterop;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models
{
    public class DownloadProgressVideoStreamingSession : IVideoStreamingSession, IDisposable
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割



        public NicoVideoQuality Quality { get; }

        public IRandomAccessStreamReference StreamRef { get; }
        public IRandomAccessStream _Stream;
        FFmpegInteropMSS _VideoMSS;
        MediaSource _MediaSource;

        MediaPlayer _PlayingMediaPlayer;

        public DownloadProgressVideoStreamingSession(IRandomAccessStreamReference streamRef, NicoVideoQuality requestQuality)
        {
            StreamRef = streamRef;
            Quality = requestQuality;
        }


        public void Dispose()
        {
            _MediaSource.Dispose();
            _MediaSource = null;
            _VideoMSS.Dispose();
            _VideoMSS = null;
            _Stream.Dispose();
            _Stream = null;

            _PlayingMediaPlayer = null;
        }

        public Task<Uri> GetDownloadUrlAndSetupDonwloadSession()
        {
            throw new NotSupportedException();
        }

        public async Task StartPlayback(MediaPlayer player)
        {
            string contentType = string.Empty;

            var stream = await StreamRef.OpenReadAsync();
            if (!stream.ContentType.EndsWith("mp4"))
            {
                _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, false, false);
                var mss = _VideoMSS.GetMediaStreamSource();
                mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                _MediaSource = MediaSource.CreateFromMediaStreamSource(mss);
            }
            else
            {
                _MediaSource = MediaSource.CreateFromStream(stream, stream.ContentType);
            }


            if (_MediaSource != null)
            {
                player.Source = _MediaSource;
                _Stream = stream;
                _PlayingMediaPlayer = player;
            }
            else
            {
                throw new NotSupportedException("can not play video. vide source from download progress stream.");
            }

        }
    }
}

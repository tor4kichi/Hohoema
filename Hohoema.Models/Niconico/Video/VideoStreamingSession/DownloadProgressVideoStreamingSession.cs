﻿using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace Hohoema.Models.Niconico.Video.VideoStreamingSession
{
    public class DownloadProgressVideoStreamingSession : IStreamingSession, IDisposable
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public string QualityId { get; }

        public NicoVideoQuality Quality { get; }

        public IRandomAccessStreamReference StreamRef { get; }
        public IRandomAccessStream _Stream;
        MediaSource _MediaSource;

        MediaPlayer _PlayingMediaPlayer;

        public DownloadProgressVideoStreamingSession(IRandomAccessStreamReference streamRef, NicoVideoQuality requestQuality)
        {
            StreamRef = streamRef;
            Quality = requestQuality;
            QualityId = requestQuality.ToString();
        }


        public void Dispose()
        {
            _MediaSource.Dispose();
            _MediaSource = null;
            _Stream.Dispose();
            _Stream = null;

            _PlayingMediaPlayer = null;
        }

        public Task<Uri> GetDownloadUrlAndSetupDonwloadSession()
        {
            throw new NotSupportedException();
        }

        public async Task StartPlayback(MediaPlayer player, TimeSpan startPosition)
        {
            string contentType = string.Empty;

            var stream = await StreamRef.OpenReadAsync();
            if (!stream.ContentType.EndsWith("mp4"))
            {
                throw new NotSupportedException(stream.ContentType);
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

                _PlayingMediaPlayer.PlaybackSession.Position = startPosition;
            }
            else
            {
                throw new NotSupportedException("can not play video. vide source from download progress stream.");
            }

        }
    }
}

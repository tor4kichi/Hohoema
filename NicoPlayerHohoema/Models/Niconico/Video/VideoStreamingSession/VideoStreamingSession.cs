using FFmpegInterop;
using Mntone.Nico2;
using Mntone.Nico2.Videos.Thumbnail;
using NicoPlayerHohoema.Helpers;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models
{
    public abstract class VideoStreamingSession : IVideoStreamingSession, IDisposable
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割
        


        public abstract NicoVideoQuality Quality { get; }

        protected NiconicoContext _Context;
        FFmpegInteropMSS _VideoMSS;
        MediaSource _MediaSource;

        MediaPlayer _PlayingMediaPlayer;



        public VideoStreamingSession(NiconicoContext context)
        {
            _Context = context;
        }

        public async Task StartPlayback(MediaPlayer player)
        {
            var videoUri = await GetVideoContentUri();

            // Note: HTML5プレイヤー移行中のFLV動画に対するフォールバック処理
            // サムネではContentType=FLV,SWFとなっていても、
            // 実際に渡される動画ストリームのContentTypeがMP4となっている場合がある

            var videoContentType = MovieType.Mp4;
            MediaSource mediaSource = null;
            
            if (!videoUri.IsFile)
            {
                // オンラインからの再生


                var tempStream = await HttpSequencialAccessStream.CreateAsync(
                    _Context.HttpClient
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
                    mediaSource = MediaSource.CreateFromMediaStreamSource(mss);
                }
                else
                {
                    tempStream.Dispose();
                    tempStream = null;

                    mediaSource = MediaSource.CreateFromUri(videoUri);
                }
            }
            else
            {
                // ローカル再生時


                var file = await StorageFile.GetFileFromPathAsync(videoUri.OriginalString);
                var stream = await file.OpenReadAsync();
                var contentType = stream.ContentType;

                if (contentType == null) { throw new NotSupportedException("can not play video file. " + videoUri.OriginalString); }

                if (contentType == "video/mp4")
                {
                    mediaSource = MediaSource.CreateFromStream(stream, contentType);
                }
                else
                {
                    _VideoMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, false, false);
                    var mss = _VideoMSS.GetMediaStreamSource();
                    mss.SetBufferedRange(TimeSpan.Zero, TimeSpan.Zero);
                    mediaSource = MediaSource.CreateFromMediaStreamSource(mss);
                }
            }

            
            if (mediaSource != null)
            {
                player.Source = mediaSource;
                _MediaSource = mediaSource;
                _PlayingMediaPlayer = player;

                OnStartStreaming();
            }
            else
            {
                throw new NotSupportedException("can not play video. Video URI: " + videoUri);
            }
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

        protected abstract Task<Uri> GetVideoContentUri();

        protected virtual void OnStartStreaming() { }
        protected virtual void OnStopStreaming() { }

        public void Dispose()
        {
            OnStopStreaming();

            if (_PlayingMediaPlayer != null)
            {
                _PlayingMediaPlayer.Pause();
                _PlayingMediaPlayer.Source = null;
                _PlayingMediaPlayer = null;

                _VideoMSS?.Dispose();
                _MediaSource?.Dispose();
            }
        }
    }
}

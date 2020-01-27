using FFmpegInterop;
using Mntone.Nico2;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using Windows.Storage;
using Windows.Storage.Streams;

namespace NicoPlayerHohoema.Models
{
    public abstract class VideoStreamingSession : IVideoStreamingSession, IDisposable
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割
        


        public abstract string QualityId { get; }
        public abstract NicoVideoQuality Quality { get; } 
        public NiconicoSession NiconicoSession { get; }

        MediaSource _MediaSource;

        MediaPlayer _PlayingMediaPlayer;

        public VideoStreamingSession(NiconicoSession niconicoSession)
        {
            NiconicoSession = niconicoSession;
        }

        public async Task StartPlayback(MediaPlayer player, TimeSpan initialPosition = default)
        {
            // Note: HTML5プレイヤー移行中のFLV動画に対するフォールバック処理
            // サムネではContentType=FLV,SWFとなっていても、
            // 実際に渡される動画ストリームのContentTypeがMP4となっている場合がある

            var mediaSource = await GetPlyaingVideoMediaSource();

            /*
            if (!videoUri.IsFile)
            {
                // オンラインからの再生
                
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
                    else if (AdaptiveMediaSource.IsContentTypeSupported(contentType))
                    {
                        var amsResult = await AdaptiveMediaSource.CreateFromUriAsync(videoUri, NiconicoSession.Context.HttpClient);
                        if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
                        {
                            amsResult.MediaSource.DownloadRequested += MediaSource_DownloadRequested;
                            mediaSource = MediaSource.CreateFromAdaptiveMediaSource(amsResult.MediaSource);
                            

                        }
                    }
                    else
                    {
                        videoContentType = MovieType.Mp4;

                        //throw new NotSupportedException($"{contentType} is not supported video format.");
                    }
                }

                if (mediaSource == null)
                {
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
            */
            
            if (mediaSource != null)
            {
                player.Source = mediaSource;
                _MediaSource = mediaSource;
                _PlayingMediaPlayer = player;

                _PlayingMediaPlayer.PlaybackSession.Position = initialPosition;

                OnStartStreaming();

                _PlayingMediaPlayer.Play();
            }
            else
            {
                throw new NotSupportedException("can not play video");
            }
        }

        protected abstract Task<MediaSource> GetPlyaingVideoMediaSource();

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

                _MediaSource?.Dispose();
            }
        }
    }
}

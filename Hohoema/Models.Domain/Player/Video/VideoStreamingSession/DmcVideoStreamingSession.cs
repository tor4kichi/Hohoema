using Mntone.Nico2;
using Mntone.Nico2.Videos.Dmc;
using Hohoema.Models.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Xaml;
using Windows.System;
using Uno.Threading;
using NiconicoSession = Hohoema.Models.Domain.Niconico.NiconicoSession;
using Hohoema.Models.Domain.Niconico.Video;

namespace Hohoema.Models.Domain.Player.Video
{
    public class DmcVideoStreamingSession : VideoStreamingSession, IVideoStreamingDownloadSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public DmcWatchResponse DmcWatchResponse { get; private set; }

        private Timer _DmcSessionHeartbeatTimer;

        private int _HeartbeatCount = 0;
        private bool IsFirstHeartbeat => _HeartbeatCount == 0;

        public VideoContent VideoContent { get; private set; }

        public override string QualityId { get; }
        public override NicoVideoQuality Quality { get; }


        DmcWatchData _DmcWatchData;
        static DmcSessionResponse _DmcSessionResponse;

        public DmcVideoStreamingSession(string qualityId, DmcWatchData res, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager.VideoSessionOwnership videoSessionOwnership)
            : base(niconicoSession, videoSessionOwnership)
        {
            _DmcWatchData = res;
            DmcWatchResponse = res.DmcWatchResponse;

            QualityId = qualityId;
            Quality = res.ToNicoVideoQuality(qualityId);

#if DEBUG
            Debug.WriteLine($"Id/Bitrate/Resolution/Available");
            foreach (var q in _DmcWatchData.DmcWatchResponse.Media.Delivery.Movie.Videos)
            {
                Debug.WriteLine($"{q.Id}/{q.Metadata.Bitrate}/{q.IsAvailable}/{q.Metadata.Resolution}");
            }
#endif

            VideoContent = DmcWatchResponse.Media.Delivery.Movie.Videos.FirstOrDefault(x => x.Id == qualityId);

            if (VideoContent != null)
            {
                Debug.WriteLine($"{VideoContent.Id}/{VideoContent.Metadata.Bitrate}/{VideoContent.IsAvailable}/w={VideoContent.Metadata.Resolution.Width} h={VideoContent.Metadata.Resolution.Height}");
            }
        }

        private async Task<DmcSessionResponse> GetDmcSessionAsync()
        {
            if (DmcWatchResponse == null) { return null; }

            if (DmcWatchResponse.Media == null) { return null; }

            if (VideoContent == null)
            {
                return null;
            }

            try
            {
                // 直前に同一動画を見ていた場合には、動画ページに再アクセスする
                DmcSessionResponse clearPreviousSession = null;
                if (_DmcSessionResponse != null)
                {
                    if (_DmcSessionResponse.Data.Session.RecipeId.EndsWith(DmcWatchResponse.Video.Id))
                    {
                        clearPreviousSession = _DmcSessionResponse;
                        _DmcSessionResponse = null;
                        DmcWatchResponse = await NiconicoSession.Context.Video.GetDmcWatchJsonAsync(DmcWatchResponse.Client.WatchId, NiconicoSession.IsLoggedIn, DmcWatchResponse.Client.WatchTrackId);
                    }
                }

                _DmcSessionResponse = await NiconicoSession.Context.Video.GetDmcSessionResponse(DmcWatchResponse, VideoContent);

                if (_DmcSessionResponse == null) { return null; }

                if (clearPreviousSession != null)
                {
                    await NiconicoSession.Context.Video.DmcSessionExitHeartbeatAsync(DmcWatchResponse, clearPreviousSession);
                }
            }
            catch
            {
                return null;
            }

            return _DmcSessionResponse;
        }

        protected override async Task<MediaSource> GetPlyaingVideoMediaSource()
        {
            if (!NiconicoSession.Context.HttpClient.DefaultRequestHeaders.ContainsKey("Origin"))
            {
                NiconicoSession.Context.HttpClient.DefaultRequestHeaders.Add("Origin", "https://www.nicovideo.jp");
            }

            NiconicoSession.Context.HttpClient.DefaultRequestHeaders.Referer = new Uri($"https://www.nicovideo.jp/watch/{DmcWatchResponse.Video.Id}");


            var session = await GetDmcSessionAsync();

            if (session == null)
            {
                if (DmcWatchResponse.Media.DeliveryLegacy != null)
                {
                    throw new NotSupportedException("DmcWatchResponse.Media.DeliveryLegacy not supported");
                    //return MediaSource.CreateFromUri(new Uri(DmcWatchResponse.Video.SmileInfo.Url));
                }
                else
                {
                    throw new Models.Infrastructure.HohoemaExpception();
                }
            }

            var uri = session != null ? new Uri(session.Data.Session.ContentUri) : null;

            if (session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HttpOutputDownloadParameters != null)
            {
                return MediaSource.CreateFromUri(uri);
            }
            else if (session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HlsParameters != null)
            {
                var hlsParameters = session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HlsParameters;
                 
                var key = await this.NiconicoSession.Context.HttpClient.GetStringAsync(new Uri(hlsParameters.Encryption.HlsEncryptionV1.KeyUri));

                var amsResult = await AdaptiveMediaSource.CreateFromUriAsync(uri, this.NiconicoSession.Context.HttpClient);
                if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
                {
                    await NiconicoSession.Context.Video.SendOfficialHlsWatchAsync(DmcWatchResponse.Video.Id, DmcWatchResponse.Media.Delivery.TrackingId);

                    return MediaSource.CreateFromAdaptiveMediaSource(amsResult.MediaSource);
                }
            }

            throw new NotSupportedException("");
        }

        public async Task<Uri> GetDownloadUrlAndSetupDownloadSession()
        {
            var session = await GetDmcSessionAsync();
            var videoUri = session != null ? new Uri(session.Data.Session.ContentUri) : null;

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

        protected override void OnStartStreaming()
        {
            if (DmcWatchResponse != null && _DmcSessionResponse != null)
            {
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを開始しました");

                _DmcSessionHeartbeatTimer = new Timer(_DmcSessionHeartbeatTimer_Tick, this, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            }
        }

        private async void _DmcSessionHeartbeatTimer_Tick(object state)
        {
            Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート {_HeartbeatCount + 1}回目");

            var _this = (DmcVideoStreamingSession)state;

            if (_this.IsFirstHeartbeat)
            {
                await _this.NiconicoSession.Context.Video.DmcSessionFirstHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                Debug.WriteLine($"{_this.DmcWatchResponse.Video.Title} の初回ハートビート実行");
                await Task.Delay(2);
            }
            else
            {
                try
                {
                    await _this.NiconicoSession.Context.Video.DmcSessionHeartbeatAsync(_this.DmcWatchResponse, _DmcSessionResponse);
                    Debug.WriteLine($"{_this.DmcWatchResponse.Video.Title} のハートビート実行");
                }
                catch
                {
                    _this.OnStopStreaming();
                }
            }

            _this._HeartbeatCount++;
        }

        protected override async void OnStopStreaming()
        {
            if (_DmcSessionHeartbeatTimer != null)
            {
                _DmcSessionHeartbeatTimer.Dispose();
                _DmcSessionHeartbeatTimer = null;
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを終了しました");
            }

            try
            {
                if (_DmcSessionResponse != null)
                {
                    await NiconicoSession.Context.Video.DmcSessionLeaveAsync(DmcWatchResponse, _DmcSessionResponse);
                }
            }
            catch { }
        }
    }
}

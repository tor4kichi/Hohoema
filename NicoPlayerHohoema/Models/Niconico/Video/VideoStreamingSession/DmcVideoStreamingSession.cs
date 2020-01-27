using Mntone.Nico2;
using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{
    public class DmcVideoStreamingSession : VideoStreamingSession, IVideoStreamingDownloadSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割

        public DmcWatchResponse DmcWatchResponse { get; private set; }

        private Timer _DmcSessionHeartbeatTimer;

        private static AsyncLock DmcSessionHeartbeatLock = new AsyncLock();

        private int _HeartbeatCount = 0;
        private bool IsFirstHeartbeat => _HeartbeatCount == 0;

        public VideoContent VideoContent { get; private set; }

        private byte[] _EncryptionKey;


        private Windows.Web.Http.HttpClient _HttpClient;


        public override string QualityId { get; }
        public override NicoVideoQuality Quality { get; }

        /*
        private VideoContent ResetActualQuality()
        {
            if (DmcWatchResponse?.Video.DmcInfo?.Quality?.Videos == null)
            {
                return null;
            }

            var videos = DmcWatchResponse.Video.DmcInfo.Quality.Videos;

            int qulity_position = 0;
            switch (RequestedQuality)
            {
                case NicoVideoQuality.Dmc_SuperHigh:
                    qulity_position = 5;
                    break;
                case NicoVideoQuality.Dmc_High:
                    // 4 -> 0
                    // 3 -> x
                    // 2 -> x
                    // 1 -> x
                    qulity_position = 4;
                    break;
                case NicoVideoQuality.Dmc_Midium:
                    // 4 -> 1
                    // 3 -> 0
                    // 2 -> x
                    // 1 -> x
                    qulity_position = 3;
                    break;
                case NicoVideoQuality.Dmc_Low:
                    // 4 -> 2
                    // 3 -> 1
                    // 2 -> 0
                    // 1 -> x
                    qulity_position = 2;
                    break;
                case NicoVideoQuality.Dmc_Mobile:
                    // 4 -> 3
                    // 3 -> 2
                    // 2 -> 1
                    // 1 -> 0
                    qulity_position = 1;
                    break;
                default:
                    throw new Exception();
            }

            VideoContent result = null;

            var pos = videos.Count - qulity_position;
            if (videos.Count >= qulity_position)
            {
                result = videos.ElementAtOrDefault(pos) ?? null;
            }
            
            return result;
        }
        */

        DmcWatchData _DmcWatchData;
        static DmcSessionResponse _DmcSessionResponse;

        public DmcVideoStreamingSession(string qualityId, DmcWatchData res, NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
            _DmcWatchData = res;
            DmcWatchResponse = res.DmcWatchResponse;

            QualityId = qualityId;
            Quality = res.ToNicoVideoQuality(qualityId);

#if DEBUG
            Debug.WriteLine($"Id/Bitrate/Resolution/Available");
            foreach (var q in _DmcWatchData.DmcWatchResponse.Video.DmcInfo.Quality.Videos)
            {
                Debug.WriteLine($"{q.Id}/{q.Bitrate}/{q.Available}/{q.Resolution}");
            }
#endif

            VideoContent = DmcWatchResponse.Video.DmcInfo.Quality.Videos.FirstOrDefault(x => x.Id == qualityId);

            if (VideoContent != null)
            {
                Debug.WriteLine($"{VideoContent.Id}/{VideoContent.Bitrate}/{VideoContent.Available}/w={VideoContent.Resolution.Width} h={VideoContent.Resolution.Height}");
            }
        }

        private async Task<DmcSessionResponse> GetDmcSessionAsync()
        {
            if (DmcWatchResponse == null) { return null; }

            if (DmcWatchResponse.Video.DmcInfo == null) { return null; }

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
                        DmcWatchResponse = await NiconicoSession.Context.Video.GetDmcWatchJsonAsync(DmcWatchResponse.Video.Id, _DmcWatchData.DmcWatchEnvironment.PlaylistToken);
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
                if (DmcWatchResponse.Video.SmileInfo != null)
                {
                    return MediaSource.CreateFromUri(new Uri(DmcWatchResponse.Video.SmileInfo.Url));
                }
                else
                {
                    throw new Exception();
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
                var amsResult = await AdaptiveMediaSource.CreateFromUriAsync(uri, this.NiconicoSession.Context.HttpClient);
                if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
                {
                    await NiconicoSession.Context.Video.SendOfficialHlsWatchAsync(DmcWatchResponse.Video.Id, DmcWatchResponse.Video.DmcInfo.TrackingId);

                    return MediaSource.CreateFromAdaptiveMediaSource(amsResult.MediaSource);
                }
            }

            throw new NotSupportedException("");
        }


        public async Task<Uri> GetDownloadUrlAndSetupDonwloadSession()
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
                _DmcSessionHeartbeatTimer = new Timer(_DmcSessionHeartbeatTimer_Tick, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            }
        }

        private async void _DmcSessionHeartbeatTimer_Tick(object sender)
        {
            using (var releaser = await DmcSessionHeartbeatLock.LockAsync())
            {
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート {_HeartbeatCount + 1}回目");

                if (IsFirstHeartbeat)
                {
                    await NiconicoSession.Context.Video.DmcSessionFirstHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                    Debug.WriteLine($"{DmcWatchResponse.Video.Title} の初回ハートビート実行");
                    await Task.Delay(2);
                }
                else
                {
                    try
                    {
                        await NiconicoSession.Context.Video.DmcSessionHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                        Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート実行");
                    }
                    catch
                    {
                        OnStopStreaming();
                    }
                }

                _HeartbeatCount++;
            }
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

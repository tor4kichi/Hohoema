using Mntone.Nico2;
using Mntone.Nico2.Videos.Dmc;
using NicoPlayerHohoema.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace NicoPlayerHohoema.Models
{
    public class DmcVideoStreamingSession : VideoStreamingSession
    {
        // Note: 再生中のハートビート管理を含めた管理
        // MediaSourceをMediaPlayerに設定する役割


        NicoVideoQuality _Quality;
        public override NicoVideoQuality Quality => _Quality;

        public DmcWatchResponse DmcWatchResponse { get; private set; }

        private DispatcherTimer _DmcSessionHeartbeatTimer;

        private static AsyncLock DmcSessionHeartbeatLock = new AsyncLock();

        private int _HeartbeatCount = 0;
        private bool IsFirstHeartbeat => _HeartbeatCount == 0;

        public NicoVideoQuality RequestedQuality { get; }

        public VideoContent VideoContent { get; private set; }

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
            
            if (result == null || !result.Available)
            {
                result = videos.FirstOrDefault(x => x.Available);
            }

            

            return result;
        }

        DmcWatchData _DmcWatchData;
        static DmcSessionResponse _DmcSessionResponse;

        public DmcVideoStreamingSession(DmcWatchData res, NicoVideoQuality requestQuality, NiconicoContext context)
            : base(context)
        {
            RequestedQuality = requestQuality;
            _DmcWatchData = res;
            DmcWatchResponse = res.DmcWatchResponse;

            VideoContent = ResetActualQuality();

            if (VideoContent != null)
            {
                if (VideoContent.Bitrate >= 4000_000)
                {
                    _Quality = NicoVideoQuality.Dmc_SuperHigh;
                }
                else if (VideoContent.Bitrate >= 1400_000)
                {
                    _Quality = NicoVideoQuality.Dmc_High;
                }
                else if (VideoContent.Bitrate >= 1000_000)
                {
                    _Quality = NicoVideoQuality.Dmc_Midium;
                }
                else if (VideoContent.Bitrate >= 600_000)
                {
                    _Quality = NicoVideoQuality.Dmc_Low;
                }
                else
                {
                    _Quality = NicoVideoQuality.Dmc_Mobile;
                }

                Debug.WriteLine($"bitrate={VideoContent.Bitrate}, id={VideoContent.Id}, w={VideoContent.Resolution.Width}, h={VideoContent.Resolution.Height}");
                Debug.WriteLine($"quality={_Quality}");
            }
        }

        protected override async Task<Uri> GetVideoContentUri()
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
                        DmcWatchResponse = await _Context.Video.GetDmcWatchJsonAsync(DmcWatchResponse.Video.Id, _DmcWatchData.DmcWatchEnvironment.PlaylistToken);
                    }
                }

                _DmcSessionResponse = await _Context.Video.GetDmcSessionResponse(DmcWatchResponse, VideoContent);

                if (_DmcSessionResponse == null) { return null; }

                if (clearPreviousSession != null)
                {
                    await _Context.Video.DmcSessionExitHeartbeatAsync(DmcWatchResponse, clearPreviousSession);
                }

                return new Uri(_DmcSessionResponse.Data.Session.ContentUri);
            }
            catch
            {
                return null;
            }
        }

        protected override void OnStartStreaming()
        {
            if (DmcWatchResponse != null && _DmcSessionResponse != null)
            {
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを開始しました");
                _DmcSessionHeartbeatTimer = new DispatcherTimer();
                _DmcSessionHeartbeatTimer.Interval = TimeSpan.FromSeconds(30);
                _DmcSessionHeartbeatTimer.Tick += _DmcSessionHeartbeatTimer_Tick;

                _DmcSessionHeartbeatTimer.Start();
            }
        }

        private async void _DmcSessionHeartbeatTimer_Tick(object sender, object e)
        {
            using (var releaser = await DmcSessionHeartbeatLock.LockAsync())
            {
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート {_HeartbeatCount + 1}回目");

                if (IsFirstHeartbeat)
                {
                    await _Context.Video.DmcSessionFirstHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                    Debug.WriteLine($"{DmcWatchResponse.Video.Title} の初回ハートビート実行");
                    await Task.Delay(2);
                }

                await _Context.Video.DmcSessionHeartbeatAsync(DmcWatchResponse, _DmcSessionResponse);
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビート実行");

                _HeartbeatCount++;
            }
        }

        protected override async void OnStopStreaming()
        {
            if (_DmcSessionHeartbeatTimer != null)
            {
                _DmcSessionHeartbeatTimer.Stop();
                _DmcSessionHeartbeatTimer = null;
                Debug.WriteLine($"{DmcWatchResponse.Video.Title} のハートビートを終了しました");
            }

            if (_DmcSessionResponse != null)
            {
                await _Context.Video.DmcSessionLeaveAsync(DmcWatchResponse, _DmcSessionResponse);
            }

        }
    }
}

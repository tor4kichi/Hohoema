#nullable enable
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Video.Watch;
using NiconicoToolkit.Video.Watch.Dmc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.Models.Player.Video;

public class DmcVideoStreamingSession : VideoStreamingSession, IVideoStreamingDownloadSession
{
    // Note: 再生中のハートビート管理を含めた管理
    // MediaSourceをMediaPlayerに設定する役割

    private DmcWatchApiData _dmcWatchData;
    private readonly bool _forCacheDownload;
    private DmcSessionResponse _dmcSessionResponse;

    private Timer _DmcSessionHeartbeatTimer;

    private int _HeartbeatCount = 0;
    private bool IsFirstHeartbeat => _HeartbeatCount == 0;

    public VideoContent VideoContent { get; private set; }

    public override string QualityId { get; }
    public override NicoVideoQuality Quality { get; }



    public DmcVideoStreamingSession(string qualityId, DmcWatchApiData res, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager.VideoSessionOwnership videoSessionOwnership, bool forCacheDownload = false)
        : base(niconicoSession, videoSessionOwnership)
    {
        _dmcWatchData = res;
        _forCacheDownload = forCacheDownload;
        QualityId = qualityId;
        Quality = res.ToNicoVideoQuality(qualityId);

#if DEBUG
        Debug.WriteLine($"Id/Bitrate/Resolution/Available");
        foreach (VideoContent q in _dmcWatchData.Media.Delivery.Movie.Videos)
        {
            Debug.WriteLine($"{q.Id}/{q.Metadata.Bitrate}/{q.IsAvailable}/{q.Metadata.Resolution}");
        }
#endif

        VideoContent = _dmcWatchData.Media.Delivery.Movie.Videos.FirstOrDefault(x => x.Id == qualityId);

        if (VideoContent != null)
        {
            Debug.WriteLine($"{VideoContent.Id}/{VideoContent.Metadata.Bitrate}/{VideoContent.IsAvailable}/w={VideoContent.Metadata.Resolution.Width} h={VideoContent.Metadata.Resolution.Height}");
        }
    }

    private async Task<DmcSessionResponse> GetDmcSessionAsync()
    {
        if (_dmcWatchData?.Media == null) { return null; }

        if (VideoContent == null)
        {
            return null;
        }

        try
        {
            // 直前に同一動画を見ていた場合には、動画ページに再アクセスする
            DmcSessionResponse clearPreviousSession = null;
            if (_dmcSessionResponse != null)
            {
                if (_dmcSessionResponse.Data.Session.RecipeId.EndsWith(_dmcWatchData.Video.Id))
                {
                    clearPreviousSession = _dmcSessionResponse;
                    _dmcSessionResponse = null;
                    _dmcWatchData = await NiconicoSession.ToolkitContext.Video.VideoWatch.GetDmcWatchJsonAsync(_dmcWatchData.Client.WatchId, NiconicoSession.IsLoggedIn, _dmcWatchData.Client.WatchTrackId);
                }
            }

            _dmcSessionResponse = await NiconicoSession.ToolkitContext.Video.VideoWatch.GetDmcSessionResponseAsync(_dmcWatchData, VideoContent, null, hlsMode: true);

            if (_dmcSessionResponse == null) { return null; }

            if (clearPreviousSession != null)
            {
                await NiconicoSession.ToolkitContext.Video.VideoWatch.DmcSessionExitHeartbeatAsync(_dmcWatchData, clearPreviousSession);
            }
        }
        catch
        {
            return null;
        }

        return _dmcSessionResponse;
    }

    protected override async Task<MediaSource> GetPlyaingVideoMediaSource()
    {
        if (!NiconicoSession.ToolkitContext.HttpClient.DefaultRequestHeaders.ContainsKey("Origin"))
        {
            NiconicoSession.ToolkitContext.HttpClient.DefaultRequestHeaders.Add("Origin", "https://www.nicovideo.jp");
        }

        NiconicoSession.ToolkitContext.HttpClient.DefaultRequestHeaders.Referer = new Uri($"https://www.nicovideo.jp/watch/{_dmcWatchData.Video.Id}");


        DmcSessionResponse session = await GetDmcSessionAsync();

        if (session == null)
        {
            if (_dmcWatchData.Media.DeliveryLegacy != null)
            {
                throw new NotSupportedException("DmcWatchResponse.Media.DeliveryLegacy not supported");
                //return MediaSource.CreateFromUri(new Uri(DmcWatchResponse.Video.SmileInfo.Url));
            }
            else
            {
                throw new Infra.HohoemaException();
            }
        }

        Uri uri = session?.Data.Session.ContentUri;

        if (session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HttpOutputDownloadParameters != null)
        {
            return MediaSource.CreateFromUri(uri);
        }
        else if (session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HlsParameters != null)
        {
            Protocol.HlsParameters hlsParameters = session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HlsParameters;

            if (hlsParameters.Encryption?.HlsEncryptionV1?.KeyUri != null)
            {
                _ = await NiconicoSession.ToolkitContext.HttpClient.GetStringAsync(new Uri(hlsParameters.Encryption.HlsEncryptionV1.KeyUri));
            }

            AdaptiveMediaSourceCreationResult amsResult = await AdaptiveMediaSource.CreateFromUriAsync(uri, NiconicoSession.ToolkitContext.HttpClient);
            if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
            {
                _ = await NiconicoSession.ToolkitContext.Video.VideoWatch.SendOfficialHlsWatchAsync(_dmcWatchData.Video.Id, _dmcWatchData.Media.Delivery.TrackingId);

                return MediaSource.CreateFromAdaptiveMediaSource(amsResult.MediaSource);
            }
        }

        throw new NotSupportedException("");
    }

    public async Task<Uri> GetDownloadUrlAndSetupDownloadSession()
    {
        DmcSessionResponse session = await GetDmcSessionAsync();
        Uri videoUri = session?.Data.Session.ContentUri;

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
        if (_dmcWatchData != null && _dmcSessionResponse != null)
        {
            Debug.WriteLine($"{_dmcWatchData.Video.Title} のハートビートを開始しました");

            _DmcSessionHeartbeatTimer?.Dispose();
            _DmcSessionHeartbeatTimer = new Timer(_DmcSessionHeartbeatTimer_Tick, this, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }
    }

    private async void _DmcSessionHeartbeatTimer_Tick(object state)
    {
        Debug.WriteLine($"{_dmcWatchData.Video.Title} のハートビート {_HeartbeatCount + 1}回目");

        DmcVideoStreamingSession _this = (DmcVideoStreamingSession)state;

        if (_this.IsFirstHeartbeat)
        {
            await _this.NiconicoSession.ToolkitContext.Video.VideoWatch.DmcSessionFirstHeartbeatAsync(_dmcWatchData, _dmcSessionResponse);
            Debug.WriteLine($"{_this._dmcWatchData.Video.Title} の初回ハートビート実行");
            await Task.Delay(2);
        }
        else
        {
            try
            {
                await _this.NiconicoSession.ToolkitContext.Video.VideoWatch.DmcSessionHeartbeatAsync(_this._dmcWatchData, _dmcSessionResponse);
                Debug.WriteLine($"{_this._dmcWatchData.Video.Title} のハートビート実行");
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
            Debug.WriteLine($"{_dmcWatchData.Video.Title} のハートビートを終了しました");
        }

        try
        {
            if (_dmcSessionResponse != null)
            {
                await NiconicoSession.ToolkitContext.Video.VideoWatch.DmcSessionLeaveAsync(_dmcWatchData, _dmcSessionResponse);
            }
        }
        catch { }
    }
}

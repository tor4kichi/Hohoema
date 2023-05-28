#nullable enable
using CommunityToolkit.Diagnostics;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Video.Watch;
using NiconicoToolkit.Video.Watch.Dmc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

    public override string QualityId { get; protected set; }
    public override NicoVideoQuality Quality { get; protected set; }



    public DmcVideoStreamingSession(string qualityId, DmcWatchApiData res, NiconicoSession niconicoSession, NicoVideoSessionOwnershipManager.VideoSessionOwnership videoSessionOwnership, bool forCacheDownload = false)
        : base(niconicoSession, videoSessionOwnership)
    {
        _dmcWatchData = res;
        _forCacheDownload = forCacheDownload;
        
#if DEBUG
        Debug.WriteLine($"Id/Bitrate/Resolution/Available");
        foreach (VideoContent q in _dmcWatchData.Media.Delivery.Movie.Videos)
        {
            Debug.WriteLine($"{q.Id}/{q.Metadata.Bitrate}/{q.IsAvailable}/{q.Metadata.Resolution}");
        }
#endif

        SetQuality(qualityId);
    }

    private string GetQualityId(NicoVideoQuality quality)
    {
        return _dmcWatchData.Media.Delivery.Movie.Videos.Select(x => (VideoContent: x, Quality: _dmcWatchData.ToNicoVideoQuality(x.Id)))
            .First(x => x.Quality == quality).VideoContent.Id;
    }

    public void SetQuality(NicoVideoQuality quality)
    {
        SetQuality(GetQualityId(quality));
    }

    private void SetQuality(string qualityId)
    {
        QualityId = qualityId;
        Quality = _dmcWatchData.ToNicoVideoQuality(qualityId);

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
            if (_dmcSessionResponse != null)
            {
                // 画質変更時
                var clearPreviousSession = _dmcSessionResponse;
                _dmcSessionResponse = null;
                var res = await NiconicoSession.ToolkitContext.Video.VideoWatch.GetDmcWatchJsonAsync(_dmcWatchData.Client.WatchId, NiconicoSession.IsLoggedIn, _dmcWatchData.Client.WatchTrackId);
                Guard.IsTrue(res.IsSuccess);
                _dmcWatchData = res.Data;

                await NiconicoSession.ToolkitContext.Video.VideoWatch.DmcSessionExitHeartbeatAsync(_dmcWatchData, clearPreviousSession);

                bool watchResult = await NiconicoSession.ToolkitContext.Video.VideoWatch.SendOfficialHlsWatchAsync(_dmcWatchData.Client.WatchId, _dmcWatchData.Client.WatchTrackId);
                Debug.WriteLine($"watchresult: {watchResult}");
                _dmcSessionResponse = await NiconicoSession.ToolkitContext.Video.VideoWatch.GetDmcSessionResponseAsync(_dmcWatchData, VideoContent, null, hlsMode: true);
            }
            else
            {
                await NiconicoSession.ToolkitContext.Video.VideoWatch.SendOfficialHlsWatchAsync(_dmcWatchData.Video.Id, _dmcWatchData.Media.Delivery.TrackingId);
                _dmcSessionResponse = await NiconicoSession.ToolkitContext.Video.VideoWatch.GetDmcSessionResponseAsync(_dmcWatchData, VideoContent, null, hlsMode: true);
            }


            if (_dmcSessionResponse == null) { return null; }
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
            }
            else
            {
                throw new Infra.HohoemaException();
            }
        }

        Uri uri = session?.Data.Session.ContentUri;
        Debug.WriteLine(uri.OriginalString);
        if (session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HttpOutputDownloadParameters != null)
        {
            return MediaSource.CreateFromUri(uri);
        }
        else if (session.Data.Session.Protocol.Parameters.HttpParameters.Parameters.HlsParameters != null)
        {
            AdaptiveMediaSourceCreationResult amsResult = await AdaptiveMediaSource.CreateFromUriAsync(uri, NiconicoSession.ToolkitContext.HttpClient);
            if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
            {
                return MediaSource.CreateFromAdaptiveMediaSource(amsResult.MediaSource);
            }
            else
            {
                throw amsResult.ExtendedError;
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

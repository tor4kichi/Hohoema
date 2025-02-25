﻿#nullable enable
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.VideoCache;
using System.Threading.Tasks;
using Windows.Media.Core;
using NiconicoSession = Hohoema.Models.Niconico.NiconicoSession;

namespace Hohoema.Models.Player.Video;

public class CachedVideoStreamingSession : VideoStreamingSession
{
    private readonly VideoCacheItem _videoCacheItem;

    public override string QualityId { get; protected set; }
    public override NicoVideoQuality Quality { get; protected set; }

    public CachedVideoStreamingSession(VideoCacheItem videoCacheItem, NiconicoSession niconicoSession)
        : base(niconicoSession, null)
    {
        Quality = videoCacheItem.DownloadedVideoQuality;
        QualityId = Quality.ToString();
        _videoCacheItem = videoCacheItem;
    }

    protected override Task<MediaSource> GetPlyaingVideoMediaSource()
    {
        return _videoCacheItem.GetMediaSourceAsync();
    }


    protected override void OnStopStreaming()
    {
        base.OnStopStreaming();
    }

}

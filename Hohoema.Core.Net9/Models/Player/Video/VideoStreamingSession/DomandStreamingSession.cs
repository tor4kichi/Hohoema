﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit;
using NiconicoToolkit.Video.Watch;
using NiconicoToolkit.Video.Watch.Domand;
using Windows.Media.Core;
using Windows.Media.Streaming.Adaptive;
using static NiconicoToolkit.Video.Watch.NicoVideoWatchApiResponse;

namespace Hohoema.Models.Player.Video;
public sealed class DomandStreamingSession : VideoStreamingSession
{
    private readonly NiconicoContext _context;
    private readonly WatchResponse _watchApiData;
    private readonly WatchDomand _domand;

    public DomandStreamingSession(
        WatchResponse watchApiData,
        WatchDomand domand,
        NiconicoSession niconicoSession,
        NicoVideoSessionOwnershipManager.VideoSessionOwnership videoSessionOwnership)
        : base(niconicoSession, videoSessionOwnership)
    {
        _context = niconicoSession.ToolkitContext;
        _watchApiData = watchApiData;
        _domand = domand;
        VideoQuality = _domand.Videos.Where(x => x.IsAvailable).Last();
        AudioQuality = _domand.Audios.Where(x => x.IsAvailable).Last();

        QualityId = VideoQuality.Id;
        Quality = _watchApiData.ToNicoVideoQuality(QualityId);
    }

    public override string QualityId { get; protected set; }
    public override NicoVideoQuality Quality { get; protected set; }

    public AudioContent AudioQuality { get; set; }
    public VideoContent VideoQuality { get; set; }

    public List<VideoContent> GetVideoQualities() => _watchApiData.Media.Domand.Videos;
    public List<AudioContent> GetAudioQualities() => _watchApiData.Media.Domand.Audios;

    public void SetQuality(NicoVideoQuality quality)
    {
        int requireQualityLevel = quality switch
        {
            NicoVideoQuality.SuperHigh => 4,
            NicoVideoQuality.High => 3,
            NicoVideoQuality.Midium => 2,
            NicoVideoQuality.Low => 1,
            NicoVideoQuality.Mobile => 0,
            _ => 0,
        };

        if (_watchApiData.Media.Domand.Videos.Where(x => (x.IsAvailable) && ((x.QualityLevel) == requireQualityLevel)).FirstOrDefault() is { } requireQuality)
        {
            VideoQuality = requireQuality;
        }
        else
        {
            VideoQuality = _watchApiData.Media.Domand.Videos.Where(x => x.IsAvailable).First();
        }

        Debug.WriteLine($"audio qualities: {string.Join(',', _watchApiData.Media.Domand.Audios.Select(x => x.Id))}");

        var recommendedAudioQuality = _watchApiData.Media.Domand.Audios.First(x => (x.QualityLevel) == VideoQuality.RecommendedHighestAudioQualityLevel);
        if (recommendedAudioQuality.IsAvailable)
        {
            AudioQuality = recommendedAudioQuality;
        }
        else
        {
            AudioQuality = _watchApiData.Media.Domand.Audios.First(x => x.IsAvailable);
        }

        Debug.WriteLine($"Set Quality Video: {VideoQuality.Id}, Audio: {AudioQuality.Id}");

        QualityId = VideoQuality.Id;
        Quality = _watchApiData.ToNicoVideoQuality(QualityId);
    }

    protected override async Task<MediaSource> GetPlyaingVideoMediaSource()
    {
        var res = await _context.Video.VideoWatch.GetDomandHlsAccessRightAsync(
            _watchApiData.Video.Id,
            _domand,
            VideoQuality,
            AudioQuality,
            _watchApiData.VideoAds?.AdditionalParams?.WatchTrackId
            );

        if (res.IsSuccess is false)
        {
            var lowAudio = _domand.Audios.First(x => x.IsAvailable);
            Debug.WriteLine($"can't use Audio Level {AudioQuality.Id}, so fallback to {lowAudio.Id}");
            res = await _context.Video.VideoWatch.GetDomandHlsAccessRightAsync(
            _watchApiData.Video.Id,
            _domand,
            VideoQuality,
            lowAudio,
            _watchApiData.VideoAds?.AdditionalParams?.WatchTrackId
            );
        }

        var amsResult = await AdaptiveMediaSource.CreateFromUriAsync(new Uri(res.Data.ContentUrl), _context.HttpClient);
        if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
        {
            return MediaSource.CreateFromAdaptiveMediaSource(amsResult.MediaSource);
        }
        else
        {
            throw amsResult.ExtendedError;
        }
    }    
}

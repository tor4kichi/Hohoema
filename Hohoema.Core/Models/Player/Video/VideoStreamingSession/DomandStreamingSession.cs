using CommunityToolkit.Diagnostics;
using DryIoc;
using Hohoema.Models.Niconico;
using Hohoema.Models.Niconico.Video;
using Hohoema.Models.VideoCache;
using NiconicoToolkit;
using NiconicoToolkit.Follow;
using NiconicoToolkit.Live.WatchPageProp;
using NiconicoToolkit.Video.Watch;
using NiconicoToolkit.Video.Watch.Domand;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Media.Streaming.Adaptive;
using static NiconicoToolkit.Video.Watch.NicoVideoWatchApiResponse;

namespace Hohoema.Models.Player.Video;
public sealed class DomandStreamingSession : VideoStreamingSession
{
    public static bool HttpClientPathToPlayingSession = true;

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
        if (_watchApiData.Media.Hls == null
            && _watchApiData.Media.Domand is { } delively)
        {
            _watchApiData.Media.Hls = new Hls()
            {
                outputs = delively.Videos.Where(x => x.IsAvailable).SelectMany(x => delively.Audios.Where(x => x.IsAvailable).Select(audio => new HlsOutput { assetUnitNames = new List<string> { x.Id, audio.Id } })).ToList()
            };
        }
        _watchApiData.Media.Hls.outputs.Sort((x, y) =>
        {
            var qx = GetAssetUnitNumericValue(x.assetUnitNames[0]);
            var qy = GetAssetUnitNumericValue(y.assetUnitNames[0]);
            var diffVideo = qx.CompareTo(qy);
            if (diffVideo == 0)
            {
                var ax = x.assetUnitNames[0].Length;
                var ay = y.assetUnitNames[1].Length;
                var diffAudio = ax.CompareTo(ay);
                if (diffVideo == 0)
                {
                    return x.assetUnitNames[1].Length.CompareTo(y.assetUnitNames[1].Length);
                }
                else
                {
                    return diffAudio;
                }
            }
            else { return diffVideo; }
        });
    }

    public static int GetAssetUnitNumericValue(string assetUnitName)
    {
        if (assetUnitName is null)
        {
            throw new ArgumentNullException(nameof(assetUnitName));
        }

        var lastPartStart = 0;
        for (var i = 0; i < assetUnitName.Length; i++)
        {
            if (assetUnitName[i] == '-')
            {
                lastPartStart = i + 1;
            }
        }

        var value = 0;
        var hasDigit = false;

        for (var i = lastPartStart; i < assetUnitName.Length; i++)
        {
            var character = assetUnitName[i];
            if (character < '0' || character > '9')
            {
                break;
            }

            hasDigit = true;
            var digit = character - '0';

            if (value > (int.MaxValue - digit) / 10)
            {
                return 0;
            }

            value = value * 10 + digit;
        }

        return hasDigit ? value : 0;
    }

    public override string QualityId { get; protected set; }
    public override NicoVideoQuality Quality { get; protected set; }

    public AudioContent AudioQuality { get; set; }
    public VideoContent VideoQuality { get; set; }

    public List<VideoContent> GetVideoQualities() => _watchApiData.Media.Domand.Videos;
    public List<AudioContent> GetAudioQualities() => _watchApiData.Media.Domand.Audios;

    private HlsOutput? _currentQualityHlsOutput;

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
        _currentQualityHlsOutput = FindHlsOutput(VideoQuality, AudioQuality);
    }

    HlsOutput? FindHlsOutput(VideoContent video, AudioContent audio)
    {
        var list = _watchApiData.Media.Hls.outputs;
        foreach (var item in list)
        {            
            if (item.assetUnitNames[0] == video.Id
                && item.assetUnitNames[1] == audio.Id)
            {
                return item;
            }
        }

        foreach (var item in list)
        {
            if (item.assetUnitNames[0] == video.Id)
            {
                return item;
            }
        }

        return null;
    }

    protected override async Task<IMediaPlaybackSource> GetPlyaingVideoMediaSource()
    {
        var res = await _context.Video.VideoWatch.GetVariantsDomandHlsAccessRightAsync(
            _watchApiData.Video.Id,
            _domand,
            _watchApiData.VideoAds?.AdditionalParams?.WatchTrackId
            );

        bool isVariantsHls = true;
        if (res.IsSuccess is false)
        {
            res = await _context.Video.VideoWatch.GetDomandHlsAccessRightAsync(
                _watchApiData.Video.Id,
                _domand,
                VideoQuality,
                AudioQuality,
                _watchApiData.VideoAds?.AdditionalParams?.WatchTrackId
                );
            isVariantsHls = false;
        }

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
            isVariantsHls = false;
        }

        //Uri hlsUri = new(_watchApiData.Media.Hls.url);
        Uri hlsUri = new(res.Data.ContentUrl);
        var amsResult = HttpClientPathToPlayingSession
            ? await AdaptiveMediaSource.CreateFromUriAsync(hlsUri, _context.HttpClient)
            : await AdaptiveMediaSource.CreateFromUriAsync(hlsUri);
                    
        if (amsResult.Status == AdaptiveMediaSourceCreationStatus.Success)
        {
            if (isVariantsHls)
            {
                var hlsOutput = FindHlsOutput(VideoQuality, AudioQuality);
                int qualityIindex = _watchApiData.Media.Hls.outputs.IndexOf(hlsOutput);
                uint currentQualityBitrate = amsResult.MediaSource.AvailableBitrates[qualityIindex];
                Debug.WriteLine($"currentQualityBitrate : {currentQualityBitrate}");
                Debug.WriteLine($"bitrates: {string.Join(',', amsResult.MediaSource.AvailableBitrates)}");
                // bitrates: 321769,493479,1073911,1245621,3037318,3209028,3653827,3825537
                amsResult.MediaSource.InitialBitrate = currentQualityBitrate;
                Debug.WriteLine($"outputs: {string.Join(',', _watchApiData.Media.Hls.outputs.Select(x => string.Join('+', x.assetUnitNames)))}");
                // outputs: video-h264-144p+audio-aac-64kbps,video-h264-144p+audio-aac-192kbps,video-h264-360p+audio-aac-64kbps,video-h264-360p+audio-aac-192kbps,video-h264-480p+audio-aac-64kbps,video-h264-480p+audio-aac-192kbps,video-h264-720p+audio-aac-64kbps,video-h264-720p+audio-aac-192kbps
                amsResult.MediaSource.DesiredMaxBitrate = currentQualityBitrate;
            }

            //Debug.WriteLine($"RequestBitrate: {requestBitrate / 1000f:F2}kbps");            
            //amsResult.MediaSource.DownloadBitrateChanged += MediaSource_DownloadBitrateChanged;            
            return MediaSource.CreateFromAdaptiveMediaSource(amsResult.MediaSource);            
        }

        throw amsResult.ExtendedError;
    }

    private void MediaSource_DownloadBitrateChanged(AdaptiveMediaSource sender, AdaptiveMediaSourceDownloadBitrateChangedEventArgs args)
    {        
        Debug.WriteLine($"DownloadBitrate: {args.NewValue / 1000f:F2}kbps");
    }

    private void Item_VideoTracksChanged(MediaPlaybackItem sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        Debug.WriteLine($"VideoTrackChanged: {sender.VideoTracks[(int)args.Index].Id}");
    }

    private Uri BuildHlsMediaUri(string assetUnitName)
    {
        Uri sourceUri = new(_watchApiData.Media.Hls.url);

        string path = sourceUri.AbsolutePath;
        int variantsIndex = path.IndexOf(
            "/playlists/variants/",
            StringComparison.OrdinalIgnoreCase);

        if (variantsIndex < 0)
        {
            throw new InvalidOperationException(
                $"Unexpected HLS URL format: {sourceUri}");
        }

        string mediaPath =
            path.Substring(0, variantsIndex)
            + "/playlists/media/"
            + assetUnitName
            + ".m3u8";

        var query = ParseQuery(sourceUri.Query);

        if (!query.TryGetValue("session", out string? session)
            || !query.TryGetValue("Signature", out string? signature)
            || !query.TryGetValue("Key-Pair-Id", out string? keyPairId))
        {
            throw new InvalidOperationException(
                $"HLS URL does not contain the required authentication parameters: {sourceUri}");
        }

        long expires = DateTimeOffset.UtcNow
            .AddDays(1)
            .ToUnixTimeSeconds();

        var builder = new UriBuilder(sourceUri)
        {
            Path = mediaPath,
            Query = string.Join(
                "&",
                $"session={Uri.EscapeDataString(session)}",
                $"Expires={expires}",
                $"Signature={Uri.EscapeDataString(signature)}",
                $"Key-Pair-Id={Uri.EscapeDataString(keyPairId)}")
        };

        return builder.Uri;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (string parameter in query.TrimStart('?').Split('&'))
        {
            if (string.IsNullOrEmpty(parameter))
            {
                continue;
            }

            string[] pair = parameter.Split('=', 2);

            string key = Uri.UnescapeDataString(pair[0]);
            string value = pair.Length == 2
                ? Uri.UnescapeDataString(pair[1])
                : string.Empty;

            result[key] = value;
        }

        return result;
    }
}


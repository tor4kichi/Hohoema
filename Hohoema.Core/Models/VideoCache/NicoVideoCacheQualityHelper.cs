#nullable enable
using Hohoema.Models.Niconico.Video;
using System;

namespace Hohoema.Models.VideoCache;

public static class NicoVideoCacheQualityHelper
{
    public static NicoVideoQuality QualityIdToCacheQuality(string qualityId)
    {        
        return qualityId switch
        {
            "video-h264-1080p" => NicoVideoQuality.SuperHigh,
            "video-h264-720p" => NicoVideoQuality.High,
            "video-h264-480p" => NicoVideoQuality.Midium,
            "video-h264-360p" => NicoVideoQuality.Low,
            "video-h264-360p-lowest" => NicoVideoQuality.Mobile,
            _ => NicoVideoQuality.Unknown,
        };
    }

    public static string CacheQualityToQualityId(NicoVideoQuality quality)
    {
        return quality switch
        {
            NicoVideoQuality.SuperHigh => "video-h264-1080p",
            NicoVideoQuality.High => "video-h264-720p",
            NicoVideoQuality.Midium => "video-h264-480p",
            NicoVideoQuality.Low => "video-h264-360p",
            NicoVideoQuality.Mobile => "video-h264-360p-lowest",
            _ => throw new NotSupportedException()
        };
    }

    public static bool TryGetOneLowerQuality(NicoVideoQuality quality, out NicoVideoQuality outQuality)
    {
        outQuality = GetOneLowerQuality(quality);

        return outQuality != NicoVideoQuality.Unknown;
    }

    public static NicoVideoQuality GetOneLowerQuality(NicoVideoQuality quality)
    {
        return quality switch
        {
            NicoVideoQuality.SuperHigh => NicoVideoQuality.High,
            NicoVideoQuality.High => NicoVideoQuality.Midium,
            NicoVideoQuality.Midium => NicoVideoQuality.Low,
            NicoVideoQuality.Low => NicoVideoQuality.Mobile,
            NicoVideoQuality.Mobile => NicoVideoQuality.Unknown,
            _ => NicoVideoQuality.Unknown,
        };
    }
}

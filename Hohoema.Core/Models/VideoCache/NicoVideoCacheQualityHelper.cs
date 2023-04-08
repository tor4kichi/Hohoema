using Hohoema.Models.Niconico.Video;
using System;

namespace Hohoema.Models.VideoCache;

public static class NicoVideoCacheQualityHelper
{
    public static NicoVideoQuality QualityIdToCacheQuality(string qualityId)
    {
        return qualityId switch
        {
            "archive_h264_1080p" => NicoVideoQuality.SuperHigh,
            "archive_h264_720p" => NicoVideoQuality.High,
            "archive_h264_480p" => NicoVideoQuality.Midium,
            "archive_h264_360p" => NicoVideoQuality.Low,
            "archive_h264_360p_low" => NicoVideoQuality.Mobile,
            _ => NicoVideoQuality.Unknown,
        };
    }

    public static string CacheQualityToQualityId(NicoVideoQuality quality)
    {
        return quality switch
        {
            NicoVideoQuality.SuperHigh => "archive_h264_1080p",
            NicoVideoQuality.High => "archive_h264_720p",
            NicoVideoQuality.Midium => "archive_h264_480p",
            NicoVideoQuality.Low => "archive_h264_360p",
            NicoVideoQuality.Mobile => "archive_h264_360p_low",
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

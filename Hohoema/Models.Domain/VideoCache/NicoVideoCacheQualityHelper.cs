using System;

namespace Hohoema.Models.Domain.VideoCache
{
    public static class NicoVideoCacheQualityHelper
    {
        public static NicoVideoCacheQuality QualityIdToCacheQuality(string qualityId)
        {
            return qualityId switch
            {
                "archive_h264_1080p" => NicoVideoCacheQuality.SuperHigh,
                "archive_h264_720p" => NicoVideoCacheQuality.High,
                "archive_h264_480p" => NicoVideoCacheQuality.Midium,
                "archive_h264_360p" => NicoVideoCacheQuality.Low,
                "archive_h264_360p_low" => NicoVideoCacheQuality.SuperLow,
                _ => NicoVideoCacheQuality.Unknown,
            };
        }

        public static string CacheQualityToQualityId(NicoVideoCacheQuality quality)
        {
            return quality switch
            {
                NicoVideoCacheQuality.SuperHigh => "archive_h264_1080p",
                NicoVideoCacheQuality.High => "archive_h264_720p",
                NicoVideoCacheQuality.Midium => "archive_h264_480p",
                NicoVideoCacheQuality.Low => "archive_h264_360p",
                NicoVideoCacheQuality.SuperLow=> "archive_h264_360p_low",
                _ => throw new NotSupportedException()
            };
        }

        public static bool TryGetOneLowerQuality(NicoVideoCacheQuality quality, out NicoVideoCacheQuality outQuality)
        {
            outQuality = GetOneLowerQuality(quality);

            return outQuality != NicoVideoCacheQuality.Unknown;
        }

        public static NicoVideoCacheQuality GetOneLowerQuality(NicoVideoCacheQuality quality)
        {
            return quality switch
            {
                NicoVideoCacheQuality.SuperHigh => NicoVideoCacheQuality.High,
                NicoVideoCacheQuality.High => NicoVideoCacheQuality.Midium,
                NicoVideoCacheQuality.Midium => NicoVideoCacheQuality.Low,
                NicoVideoCacheQuality.Low => NicoVideoCacheQuality.SuperLow,
                NicoVideoCacheQuality.SuperLow => NicoVideoCacheQuality.Unknown,
                _ => NicoVideoCacheQuality.Unknown,
            };
        }
    }
}

using System;

namespace Hohoema.Models.Domain.VideoCache
{
    public enum NicoVideoCacheQuality
    {
        Unknown,

        SuperLow,
        Low,
        Midium,
        High,
        SuperHigh,
    }

    public static class NicoVideoCacheQualityExtension
    {
        public static NicoVideoQuality ToPlayVideoQuality(this NicoVideoCacheQuality quality)
        {
            return quality switch
            {
                NicoVideoCacheQuality.Unknown => NicoVideoQuality.Unknown,
                NicoVideoCacheQuality.SuperLow => NicoVideoQuality.Mobile,
                NicoVideoCacheQuality.Low => NicoVideoQuality.Low,
                NicoVideoCacheQuality.Midium => NicoVideoQuality.Midium,
                NicoVideoCacheQuality.High => NicoVideoQuality.High,
                NicoVideoCacheQuality.SuperHigh => NicoVideoQuality.SuperHigh,
                _ => throw new NotSupportedException(),
            };
        }
    }
}

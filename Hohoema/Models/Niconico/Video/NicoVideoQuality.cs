using Mntone.Nico2.Videos.Dmc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models
{
    
	public enum NicoVideoQuality
	{
        Unknown,

        Smile_Original,
		Smile_Low,

        Dmc_SuperHigh,
        Dmc_High,
        Dmc_Midium,
        Dmc_Low,
        Dmc_Mobile,
    }


    public static class NicoVideoQualityFileNameHelper
    {

        public static bool IsLegacy(this NicoVideoQuality quality)
        {
            return quality == NicoVideoQuality.Smile_Low || quality == NicoVideoQuality.Smile_Original;
        }
        public static bool IsDmc(this NicoVideoQuality quality)
        {
            return !IsLegacy(quality);
        }
    }


    public static class NicoVideoVideoContentHelper
    {
        public static NicoVideoQuality VideoContentToQuality(VideoContent content)
        {
            if (content.Bitrate >= 4000_000)
            {
                return NicoVideoQuality.Dmc_SuperHigh;
            }
            else if (content.Bitrate >= 1400_000)
            {
                return NicoVideoQuality.Dmc_High;
            }
            else if (content.Bitrate >= 1000_000)
            {
                return NicoVideoQuality.Dmc_Midium;
            }
            else if (content.Bitrate >= 600_000)
            {
                return NicoVideoQuality.Dmc_Low;
            }
            else
            {
                return NicoVideoQuality.Dmc_Mobile;
            }
        }
    }


}

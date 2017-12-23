using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
    
	public enum NicoVideoQuality
	{
        Smile_Original,
		Smile_Low,

        Dmc_SuperHigh,
        Dmc_High,
        Dmc_Midium,
        Dmc_Low,
        Dmc_Mobile,

        Unknown,

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



}

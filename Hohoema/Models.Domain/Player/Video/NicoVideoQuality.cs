using Mntone.Nico2.Videos.Dmc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain
{
    
	public enum NicoVideoQuality
	{
        Unknown,

        SuperHigh,
        High,
        Midium,
        Low,
        Mobile,
    }

    public static class NicoVideoVideoContentHelper
    {
        public static NicoVideoQuality VideoContentToQuality(VideoContent content)
        {
            if (content.Metadata.Bitrate >= 4000_000)
            {
                return NicoVideoQuality.SuperHigh;
            }
            else if (content.Metadata.Bitrate >= 1400_000)
            {
                return NicoVideoQuality.High;
            }
            else if (content.Metadata.Bitrate >= 1000_000)
            {
                return NicoVideoQuality.Midium;
            }
            else if (content.Metadata.Bitrate >= 600_000)
            {
                return NicoVideoQuality.Low;
            }
            else
            {
                return NicoVideoQuality.Mobile;
            }
        }
    }


}

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

        public static string FileNameWithQualityNameExtention(this NicoVideoQuality quality, string filename)
        {
            var toQualityNameExtention = filename;
            switch (quality)
            {
                case NicoVideoQuality.Smile_Original:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".mp4");
                    break;
                case NicoVideoQuality.Smile_Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".low.mp4");
                    break;
                case NicoVideoQuality.Dmc_Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".xlow.mp4");
                    break;
                case NicoVideoQuality.Dmc_Midium:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".xmid.mp4");
                    break;
                case NicoVideoQuality.Dmc_High:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".xhigh.mp4");
                    break;
                default:
                    throw new NotSupportedException(quality.ToString());
            }

            return toQualityNameExtention;
        }

        public static NicoVideoQuality NicoVideoQualityFromFileNameExtention(string filename)
        {
            var firstDotPosition = filename.IndexOf('.');
            var ext = new string (filename.Skip(firstDotPosition).ToArray());

            if (ext.EndsWith(".low.mp4"))
            {
                return NicoVideoQuality.Smile_Low;
            } 
            else if (ext.EndsWith(".xlow.mp4"))
            {
                return NicoVideoQuality.Dmc_Low;
            }
            else if (ext.EndsWith(".xmid.mp4"))
            {
                return NicoVideoQuality.Dmc_Midium;
            }
            else if (ext.EndsWith(".xhigh.mp4"))
            {
                return NicoVideoQuality.Dmc_High;
            }
            else if (ext.EndsWith(".mp4"))
            {
                return NicoVideoQuality.Smile_Original;
            }
            else
            {
                throw new NotSupportedException(filename + "does not have Nico video quality in file extention. ex) *.low.mp4");
            }
        }
    }



}

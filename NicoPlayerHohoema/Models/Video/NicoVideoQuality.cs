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
		Original,
		Low,

        v2_Low,
        v2_Middle,
        v2_High,
	}


    public static class NicoVideoQualityFileNameHelper
    {
        public static string FileNameWithQualityNameExtention(this NicoVideoQuality quality, string filename)
        {
            var toQualityNameExtention = filename;
            switch (quality)
            {
                case NicoVideoQuality.Original:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".mp4");
                    break;
                case NicoVideoQuality.Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".low.mp4");
                    break;
                case NicoVideoQuality.v2_Low:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".xlow.mp4");
                    break;
                case NicoVideoQuality.v2_Middle:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".xmid.mp4");
                    break;
                case NicoVideoQuality.v2_High:
                    toQualityNameExtention = Path.ChangeExtension(filename, ".xhigh.mp4");
                    break;
                default:
                    throw new NotSupportedException(quality.ToString());
            }

            return toQualityNameExtention;
        }

        public static NicoVideoQuality NicoVideoQualityFromFileNameExtention(string filename)
        {
            var nameLastPosition = Path.GetFileNameWithoutExtension(filename).Length;
            var ext = filename.Substring(nameLastPosition);

            if (ext.EndsWith(".low.mp4"))
            {
                return NicoVideoQuality.Low;
            } 
            else if (ext.EndsWith(".xlow.mp4"))
            {
                return NicoVideoQuality.v2_Low;
            }
            else if (ext.EndsWith(".xmid.mp4"))
            {
                return NicoVideoQuality.v2_Middle;
            }
            else if (ext.EndsWith(".xhigh.mp4"))
            {
                return NicoVideoQuality.v2_High;
            }
            else if (ext.EndsWith(".mp4"))
            {
                return NicoVideoQuality.Original;
            }
            else
            {
                throw new NotSupportedException(filename + "does not have Nico video quality in file extention. ex) *.low.mp4");
            }
        }
    }



}

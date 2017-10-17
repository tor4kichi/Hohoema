using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public class NicoVideoQualityToCultualizedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            NicoVideoQuality? quality = null;
            if (value is NicoVideoQuality?)
            {
                quality = (NicoVideoQuality?)value;
            }
            if (value is NicoVideoQuality)
            {
                quality = (NicoVideoQuality)value;
            }

            switch (quality)
            {
                case NicoVideoQuality.Smile_Original:
                    return "旧 オリジナル";
                case NicoVideoQuality.Smile_Low:
                    return "旧 低";
                case NicoVideoQuality.Dmc_High:
                    return "高";
                case NicoVideoQuality.Dmc_Midium:
                    return "中";
                case NicoVideoQuality.Dmc_Low:
                    return "低";
                case NicoVideoQuality.Dmc_Mobile:
                    return "モバイル";
                default:
                    return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}

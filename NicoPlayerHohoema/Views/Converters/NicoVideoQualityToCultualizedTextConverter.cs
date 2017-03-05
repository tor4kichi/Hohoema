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
            if (value is NicoVideoQuality)
            {
                var quality = (NicoVideoQuality)value;
                switch (quality)
                {
                    case NicoVideoQuality.Original:
                        return "オリジナル";
                    case NicoVideoQuality.Low:
                        return "低";
                    case NicoVideoQuality.v2_Low:
                        return value.ToString();
                    case NicoVideoQuality.v2_Middle:
                        return value.ToString();
                    case NicoVideoQuality.v2_High:
                        return value.ToString();
                    default:
                        return value.ToString();
                }
            }
            else
            {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}

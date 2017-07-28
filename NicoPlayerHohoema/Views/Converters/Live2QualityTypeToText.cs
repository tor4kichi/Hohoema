using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public class Live2QualityTypeToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                var quality = (string)value;
                switch (quality)
                {
                    case "super_low":
                        return "モバイル画質";
                    case "low":
                        return "低画質";
                    case "normal":
                        return "通常画質";
                    case "high":
                        return "高画質";
                    case "":
                        return "画質指定なし";
                    default:
                        break;
                }
            }
            else if (value == null)
            {
                return "画質指定なし";
            }

            return value?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

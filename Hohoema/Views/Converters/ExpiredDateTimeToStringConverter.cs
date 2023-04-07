using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters
{
    public sealed class ExpiredDateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTimeOffset t)
            {
                value = t.LocalDateTime;
            }

            if (value is DateTime time)
            {
                if (time == DateTime.MinValue || time == default)
                {
                    return "";
                }
                else if (time == DateTime.MaxValue)
                {
                    return "Expired_Unlimited".Translate();
                }
                else if (time.Hour == 23 && time.Minute == 59 && time.Second == 59)
                {
                    var allDayText = "Expired_AllDayLong".Translate() ?? time.ToString("hh:mm");
                    return $"{time.ToString("D")} {allDayText}";
                }
                else
                {
                    return $"{time.ToString("f")}";
                }
            }

            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

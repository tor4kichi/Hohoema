using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
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
                if (time == DateTime.MaxValue)
                {
                    return Services.Helpers.CulturelizeHelper.ToCulturelizeString("Expired_Unlimited") ?? "Expired_Unlimited";
                }
                else if (time.Hour == 23 && time.Minute == 59 && time.Second == 59)
                {
                    var allDayText = Services.Helpers.CulturelizeHelper.ToCulturelizeString("Expired_AllDayLong") ?? time.ToString("hh:mm");
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

#nullable enable
using I18NPortable;
using System;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public sealed class TiemSpanToLocalizedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan timeSpan)
        {
            int hours = (int)Math.Floor(timeSpan.TotalHours);
            if (hours > 0)
            {
                return "TimeSpanHoursAndMinites".Translate(hours, (int)timeSpan.Minutes);
            }
            else
            {
                return "TimeSpanMinutes".Translate((int)timeSpan.Minutes);
            }
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

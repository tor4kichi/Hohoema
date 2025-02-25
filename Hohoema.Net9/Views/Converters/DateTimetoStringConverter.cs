#nullable enable
using System;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public partial class DateTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset t)
        {
            value = t.LocalDateTime;
        }

        if (value is DateTime time)
        {
            return $"{time.ToString((parameter as string) ?? "f")}";
        }

        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

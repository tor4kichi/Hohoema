#nullable enable
using System;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public sealed class TimeSpanToSecondsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan time)
        {
            return time.TotalSeconds;
        }
        else
        {
            throw new ArgumentException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double val)
        {
            return TimeSpan.FromSeconds(val);
        }
        else
        {
            throw new ArgumentException();
        }
    }
}

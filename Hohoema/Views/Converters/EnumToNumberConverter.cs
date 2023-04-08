#nullable enable
using System;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public sealed class EnumToNumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (int)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return Enum.ToObject(targetType, (int)value);
    }
}

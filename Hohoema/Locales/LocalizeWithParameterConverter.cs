using I18NPortable;
using System;
using Windows.UI.Xaml.Data;

namespace Hohoema.Locales;

public sealed class LocalizeWithParameterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string stringParameter)
        {
            return stringParameter.Translate(value);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

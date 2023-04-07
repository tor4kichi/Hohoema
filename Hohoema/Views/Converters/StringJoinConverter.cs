using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public sealed class StringJoinConverter : IValueConverter
{
    public string Separator { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is null) { return ""; }
        else if (value is IReadOnlyList<string> str)
        {
            return string.Join(Separator, str);
        }
        else if (value is string s)
        {
            return s;
        }
        else
        {
            throw new NotSupportedException(value.ToString());
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

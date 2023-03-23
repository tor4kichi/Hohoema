using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Presentation.Views.Converters;

public sealed class StringJoinConverter : IValueConverter
{
    public string Separator { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IEnumerable<string> str)
        {
            return string.Join(Separator, str);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

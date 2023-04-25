using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;
public sealed class SizeToRectConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Size size)
        {
            return new Rect(new Point(), size);
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Rect rect)
        {
            return new Size(rect.Width, rect.Height);
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}

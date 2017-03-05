using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    // this code copy from
    // http://stackoverflow.com/questions/35134824/uwp-transparent-lineargradientbrush


    public class TransparentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, string language)
        {
            Color convert = (Color)value; // Color is a struct, so we cast
            return Color.FromArgb(0, convert.R, convert.G, convert.B);
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

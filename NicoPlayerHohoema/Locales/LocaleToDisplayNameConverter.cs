using I18NPortable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Locales
{
    public sealed class LocaleToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string locale)
            {
                return I18NPortable.I18N.Current.TranslateOrNull(locale) ?? new CultureInfo(locale).NativeName.CapitalizeFirstCharacter();
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
}

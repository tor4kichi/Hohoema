using I18NPortable;
using NicoPlayerHohoema.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public sealed class MylistSortLocalizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MylistSortViewModel sortVM)
            {
                return $"MylistSort.{sortVM.Key}{sortVM.Order}".TranslateOrNull() ?? $"{sortVM.Key} - {sortVM.Order}";
            }

            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

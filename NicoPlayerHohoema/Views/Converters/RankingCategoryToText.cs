using Mntone.Nico2.Videos.Ranking;
using NicoPlayerHohoema.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public class RankingCategoryToText : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) { return null; }

            try
            {
                var category = (RankingCategory)value;

                return category.ToCultulizedText();
            }
            catch { return value?.ToString(); }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

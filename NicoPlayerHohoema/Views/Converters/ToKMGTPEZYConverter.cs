using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public sealed class ToKMGTPEZYConverter : IValueConverter
    {

        const string KMGTPEZY = "KMGTPEZY";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var number = (double)value;
            long longNumber = (long)number;
            if (number == 0) { return "0"; }

            var digit = (int)Math.Log10(number) + 1;


            if (digit <= 3) { return number.ToString(); }

            var KMGTPEZY_pos = ((digit - 1 - 3) / 3);
            var KMGTPEZY_amari = digit - (KMGTPEZY_pos+1) * 3;

            if (KMGTPEZY.Length > KMGTPEZY_pos)
            {
                var displayNumber = new string(longNumber.ToString().Take(KMGTPEZY_amari).ToArray());
                return $"{displayNumber}{KMGTPEZY.ElementAt(KMGTPEZY_pos)}";
            }
            else
            {
                throw new NotSupportedException("over digit amount, can not convert, digit: " + digit);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

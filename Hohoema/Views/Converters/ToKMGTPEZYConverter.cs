using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters
{
    public sealed class ToKMGTPEZYConverter : IValueConverter
    {

        const string KMGTPEZY = "KMGTPEZY";

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            double number = 0.0;
            if (value is not null && Decimal.TryParse(value.ToString(), out var num))
            {
                number = Decimal.ToDouble(num);
            }



            int divCount = -1;
            while (number >= 1000.0d)
            {
                number /= 1000.0d;
                divCount++;
            }

            if (divCount >= KMGTPEZY.Length)
            {
                throw new NotSupportedException("ヨタより大きい桁数は対応してない");
            }
            else if (divCount >= 2 /* G 以上なら */)
            {
                return number.ToString("F2") + KMGTPEZY[divCount];
            }
            else if (divCount >= 0 /* K 以上なら */)
            {
                return number.ToString("F0") + KMGTPEZY[divCount];
            }
            else
            {
                return number.ToString("F0");
            }

            /*
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
            */
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

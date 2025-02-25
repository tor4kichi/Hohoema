#nullable enable
using Hohoema.Helpers;
using System;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public sealed partial class ToKMGTPEZYConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        double number = 0.0;
        if (value is not null && Decimal.TryParse(value.ToString(), out var num))
        {
            number = Decimal.ToDouble(num);
        }

        return NumberToKMGTPEZYStringHelper.ToKMGTPEZY(number);

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

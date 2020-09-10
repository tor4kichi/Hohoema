using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Presentation.Views.Converters
{
    public sealed class StringToUriConverter : IValueConverter
    {
        static readonly Regex UrlRegex = new Regex("^http(s)?://([\\w-]+.)+[\\w-]+(/[\\w- ./?%&=])?$");

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string str)
            {
                if (Uri.IsWellFormedUriString(str, UriKind.Absolute))
                {
                    return new Uri(str);
                }

                if (UrlRegex.IsMatch(str))
                {
                    var match = UrlRegex.Match(str);
                    return new Uri(match.Groups.Cast<Match>().First().Value);
                }
            }
            else if (value is Uri uri)
            {
                return uri;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

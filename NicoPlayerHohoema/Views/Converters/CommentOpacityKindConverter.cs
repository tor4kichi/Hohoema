using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public class CommentOpacityKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Models.CommentOpacityKind)
            {
                var kind = (Models.CommentOpacityKind)value;
                switch (kind)
                {
                    case Models.CommentOpacityKind.NoSukesuke:
                        return "透過しない";
                    case Models.CommentOpacityKind.BitSukesuke:
                        return "少し透過";
                    case Models.CommentOpacityKind.MoreSukesuke:
                        return "かなり透過";
                    default:
                        return "透過しない";
                }
            }
            else
            {
                return value?.ToString() ?? "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public class PlayerEndActionToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Models.PlaylistEndAction)
            {
                var endAction = (Models.PlaylistEndAction)value;
                switch (endAction)
                {
                    case Models.PlaylistEndAction.None:
                        return "何もしない";
                    case Models.PlaylistEndAction.ChangeIntoSplit:
                        return "プレイヤーを小さく表示";
                    case Models.PlaylistEndAction.CloseIfPlayWithCurrentWindow:
                        return "プレイヤーを閉じる";
                    default:
                        break;
                }
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

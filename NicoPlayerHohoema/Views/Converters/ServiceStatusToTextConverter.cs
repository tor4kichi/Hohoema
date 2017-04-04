using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
    public class ServiceStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Models.HohoemaAppServiceLevel && targetType == typeof(string))
            {
                var serviceLevel = (Models.HohoemaAppServiceLevel)value;
                switch (serviceLevel)
                {
                    case Models.HohoemaAppServiceLevel.Offline:
                        return "オフライン";
                    case Models.HohoemaAppServiceLevel.OnlineButServiceUnavailable:
                        return "ニコニコ動画サービス利用不可";
                    case Models.HohoemaAppServiceLevel.OnlineWithoutLoggedIn:
                        return "ログイン無し";
                    case Models.HohoemaAppServiceLevel.LoggedIn:
                        return "ログイン（通常会員）";
                    case Models.HohoemaAppServiceLevel.LoggedInWithPremium:
                        return "ログイン（プレミアム会員）";
                    default:
                        break;
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

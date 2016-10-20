using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
	public class TimeSpanToMovieLengthStringValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var seconds = value is TimeSpan ? (TimeSpan)value : TimeSpan.FromSeconds(System.Convert.ToInt32(value));

			bool isNegative = seconds.TotalSeconds < 0;
			string timeText;
			if (seconds.Hours > 0)
			{
				timeText = seconds.ToString(@"hh\:mm\:ss");
			}
			else
			{
				timeText = seconds.ToString(@"mm\:ss");
			}

			return isNegative ? "-" + timeText : timeText;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}


}

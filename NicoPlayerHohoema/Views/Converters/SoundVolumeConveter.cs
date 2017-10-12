using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
	public class SoundVolumeConveter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			try
			{
				int casted = (int) Math.Round(((double)value * 100));
				return casted;
			}
			catch
			{
				return value.ToString();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is double)
            {
                return (double)(value) * 0.01;
            }
            else
            {
                return value;
            }
		}
	}
}

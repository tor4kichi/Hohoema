using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
	public class CursorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			CoreCursorType temp;
			if (value is string && Enum.TryParse<CoreCursorType>((string)value, out temp))
			{
				return new CoreCursor(temp, 101);
			}
			else
			{
				return Window.Current.CoreWindow.PointerCursor;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}

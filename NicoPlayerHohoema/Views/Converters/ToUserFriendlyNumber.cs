using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
	public class ToUserFriendlyNumber : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is short)		{ return ((short)value).ToString("N0"); }
			if (value is int)		{ return ((int)value).ToString("N0"); }
			if (value is long)		{ return ((long)value).ToString("N0"); }
			if (value is decimal)	{ return ((decimal)value).ToString("N0"); }
			if (value is ushort)	{ return ((ushort)value).ToString("N0"); }
			if (value is uint)		{ return ((uint)value).ToString("N0"); }
			if (value is ulong)		{ return ((ulong)value).ToString("N0"); }
			if (value is float)		{ return ((float)value).ToString("N2"); }
			if (value is double)	{ return ((double)value).ToString("N2"); }

			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}

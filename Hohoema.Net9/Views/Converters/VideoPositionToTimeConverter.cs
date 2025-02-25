#nullable enable
using System;
using Windows.UI.Xaml.Data;

namespace Hohoema.Views.Converters;

public partial class VideoPositionToTimeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		var timespan = TimeSpan.FromMilliseconds((uint)value * 10);

		if (timespan.Hours > 0)
		{
			return timespan.ToString(@"hh\:mm\:ss");
		}
		else
		{
			return timespan.ToString(@"mm\:ss");
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotImplementedException();
	}
}

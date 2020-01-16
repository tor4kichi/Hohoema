﻿using System;
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
				double casted = Math.Round(((double)value * 100));
				if (targetType == typeof(string))
				{
					return casted.ToString("F0");
				}
				else if (targetType == typeof(double))
				{
					return casted;
				}
				else if (targetType == typeof(int))
				{
					return (int)casted;
				}
				else
				{
					return casted;
				}
			}
			catch
			{
				return value?.ToString();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value is double)
            {
                return (double)(value) * 0.01;
            }
			else if (value is int val)
			{
				return val / 100;
			}
			else if (value is string str)
			{
				if (targetType == typeof(double))
				{
					return double.Parse(str);
				}
				else if (targetType == typeof(int))
				{
					return int.Parse(str);
				}
				else
				{
					throw new NotSupportedException();
				}
			}
            else
            {
                return value;
            }
		}
	}
}

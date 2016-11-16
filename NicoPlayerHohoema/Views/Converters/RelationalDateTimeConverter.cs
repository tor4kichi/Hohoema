using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace NicoPlayerHohoema.Views.Converters
{
	public class RelationalDateTimeConverter : IValueConverter
	{
		class DurationToTextConvertInfo{
			public TimeSpan Duration { get; set; }
			public Func<TimeSpan, string> GetText { get; set; }
		}
		private static DurationToTextConvertInfo[] DurationToText = new DurationToTextConvertInfo[]
			{
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromMinutes(1), GetText = (t) => "今さっき"},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromHours(1), GetText = (t) => $"{(int)Math.Floor(t.TotalMinutes)}分前"},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromDays(1), GetText = (t) => $"{(int)Math.Floor(t.TotalHours)}時間前"},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromDays(7), GetText = (t) => $"{(int)Math.Floor(t.TotalDays)}日前"},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromDays(31), GetText = (t) => $"{(int)Math.Floor(t.TotalDays/7)}週間前"},
			};


		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is DateTime)
			{
				var duration = DateTime.Now - (DateTime)value;

				var f = DurationToText.FirstOrDefault(x => duration < x.Duration);
				if (f != null)
				{
					return f.GetText(duration);
				}
				else
				{
					return $"一ヶ月以上昔";
				}
			}
			else
			{
				return value;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return !(bool)value;
		}
	}
}

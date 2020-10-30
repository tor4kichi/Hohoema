using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Hohoema.Presentation.Views.Converters
{
	public class RelationalDateTimeConverter : IValueConverter
	{
		class DurationToTextConvertInfo{
			public TimeSpan Duration { get; set; }
			public Func<TimeSpan, string> GetText { get; set; }
		}
		private static DurationToTextConvertInfo[] DurationToText = new DurationToTextConvertInfo[]
			{
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromMinutes(2), GetText = (t) => "RelationalDateTime_JustNow".Translate()},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromMinutes(60), GetText = (t) => "RelationalDateTime_SomeMinutesAgo".Translate((int)Math.Floor(t.TotalMinutes))},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromHours(24), GetText = (t) => "RelationalDateTime_SomeHoursAgo".Translate((int)Math.Floor(t.TotalHours))},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromDays(7), GetText = (t) => "RelationalDateTime_SomeDaysAgo".Translate((int)Math.Floor(t.TotalDays))},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromDays(30), GetText = (t) => "RelationalDateTime_SomeWeeksAgo".Translate((int)Math.Floor(t.TotalDays/7))},
				new DurationToTextConvertInfo{ Duration = TimeSpan.FromDays(365), GetText = (t) => "RelationalDateTime_SomeMonthAgo".Translate((int)Math.Floor(t.TotalDays/30))},
				new DurationToTextConvertInfo{ Duration = TimeSpan.MaxValue, GetText = (t) => "RelationalDateTime_SomeYearsAgo".Translate((int)Math.Floor(t.TotalDays/365))},
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
					return value;
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

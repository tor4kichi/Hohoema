using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if WINDOWS_UWP
using Windows.Networking;
#endif

namespace NiconicoToolkit
{
    internal static class DeserializationExtensions
    {
		public static bool ToBooleanFrom1(this string value)
		{
			return value != null && value.Length == 1 && value[0] == '1' ? true : false;
		}

		public static bool ToBooleanFromString(this string value)
		{
			return value == "true" ? true : false;
		}

		public static short ToShort(this string value)
		{
			return short.Parse(value, System.Globalization.NumberStyles.AllowThousands);
		}

		public static ushort ToUShort(this string value)
		{
			return ushort.Parse(value, System.Globalization.NumberStyles.AllowThousands);
		}

		public static int ToInt(this string value)
		{
			return int.Parse(value, System.Globalization.NumberStyles.AllowThousands);
		}

		public static uint ToUInt(this string value)
		{
			return uint.Parse(value, System.Globalization.NumberStyles.AllowThousands);
		}

		public static long ToLong(this string value)
		{
			return long.Parse(value, System.Globalization.NumberStyles.AllowThousands);
		}

		public static ulong ToULong(this string value)
		{
			return ulong.Parse(value, System.Globalization.NumberStyles.AllowThousands);
		}

		public static float ToSingle(this string value)
		{
			return float.Parse(value);
		}

		public static double ToDouble(this string value)
		{
			return double.Parse(value);
		}

		public static long ToLongFromDateTimeOffset(this DateTimeOffset value)
		{
			return value.Ticks / 10000000 - 116444736000000000;
		}

		public static DateTimeOffset ToDateTimeOffsetFromUnixTime(this string value)
		{
			return ToDateTimeOffsetFromUnixTime(long.Parse(value));
		}

		public static DateTimeOffset ToDateTimeOffsetFromUnixTime(this long value)
		{
			return DateTimeOffset.FromFileTime(10000000 * value + 116444736000000000);
		}

		public static DateTimeOffset ToDateTimeOffsetFromIso8601(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return DateTimeOffset.MinValue;
			}
			return DateTimeOffset.Parse(value);
		}

		public static TimeSpan ToTimeSpan(this string value)
		{
			var buf = value.Split(':');
			if (buf.Length == 3)
			{
				return new TimeSpan(int.Parse(buf[0]), int.Parse(buf[1]), int.Parse(buf[2]));
			}
			else if (buf.Length == 2)
			{
				return new TimeSpan(0, int.Parse(buf[0]), int.Parse(buf[1]));
			}
			else if (buf.Length == 1)
			{
				return new TimeSpan(0, 0, int.Parse(buf[1]));
			}
			throw new ArgumentException();
		}

		public static TimeSpan ToTimeSpanFromSecondsString(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return TimeSpan.MinValue;
			}
			return new TimeSpan(0, 0, int.Parse(value));
		}

		public static Uri ToUri(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return null;
			}
			return new Uri(value);
		}

#if WINDOWS_UWP
		public static HostName ToHostName( this string value )
		{
			return new HostName( value );
		}
#endif

		public static string ToString1Or0(this bool value)
		{
			return value ? "1" : "0";
		}



		public static int ToInt(this IEnumerable<char> chars)
		{
			int num = 0;
			foreach (var c in chars)
            {
#if DEBUG
				System.Diagnostics.Debug.Assert(char.IsDigit(c));
#endif
				num *= 10;
				num += (int)(c - '0');
            }
			return num;
		}

		public static uint ToUInt(this IEnumerable<char> chars)
		{
			uint num = 0;
			foreach (var c in chars)
			{
#if DEBUG
				System.Diagnostics.Debug.Assert(char.IsDigit(c));
#endif
				num *= 10;
				num += (uint)(c - '0');
			}
			return num;
		}
	}
}

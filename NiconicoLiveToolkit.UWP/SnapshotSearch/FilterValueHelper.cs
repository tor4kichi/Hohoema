using System;

namespace NiconicoToolkit.SnapshotSearch
{
    public static class FilterValueHelper
    {
		public static string ToStringFilterValue<T>(T value)
        {
			if (typeof(T) == typeof(DateTime))
            {
				var dateTime = Convert.ToDateTime(value);
				return dateTime.ToString("o");
            }
			else
            {
				return value.ToString();
            }
        }
    }

}

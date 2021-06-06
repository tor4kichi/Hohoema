using System;

namespace NiconicoToolkit.SearchWithPage.Video
{
    /// <summary>
    /// 整列方向
    /// </summary>
    public enum Order
	{
		/// <summary>
		/// 降順
		/// </summary>
		Descending,

		/// <summary>
		/// 昇順
		/// </summary>
		Ascending,
	}

	internal static class SortDirectionExtensions
	{
		public static char ToChar(this Order direction)
		{
			switch (direction)
			{
				case Order.Ascending:
					return 'a';
				case Order.Descending:
					return 'd';
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static string ToShortString(this Order direction)
		{
			switch (direction)
			{
				case Order.Ascending:
					return "asc";
				case Order.Descending:
					return "desc";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}

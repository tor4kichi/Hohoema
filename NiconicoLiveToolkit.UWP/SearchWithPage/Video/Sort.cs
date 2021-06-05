using System;

namespace NiconicoToolkit.SearchWithPage.Video
{
    public enum Sort
	{
		Popurarity,    // h
		FirstRetrieve, // f
		ViewCount,     // v
		MylistCount,   // m
		NewComment,    // n
		CommentCount,  // r
		Length,        // l
	}


	public static class SortMethodExtention
	{
		public static char ToChar(this Sort method)
		{
			switch (method)
			{
				case Sort.NewComment:
					return 'n';
				case Sort.ViewCount:
					return 'v';
				case Sort.MylistCount:
					return 'm';
				case Sort.CommentCount:
					return 'r';
				case Sort.FirstRetrieve:
					return 'f';
				case Sort.Length:
					return 'l';
				case Sort.Popurarity:
					return 'h';
				default:
					throw new NotSupportedException($"not support {nameof(Sort)}.{method.ToString()}");
			}
		}

		public static string ToShortString(this Sort method)
		{
			return method.ToChar().ToString();
		}
	}
}

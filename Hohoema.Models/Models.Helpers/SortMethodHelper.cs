using I18NPortable;
using NiconicoToolkit.SearchWithCeApi.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Helpers
{
	public static class SortHelper
	{
		public static string ToCulturizedText(VideoSortKey sort, VideoSortOrder order)
		{
			var isAscending = order == VideoSortOrder.Asc;
			string text;
			switch (sort)
			{
				case VideoSortKey.NewComment:
					text = isAscending ? "Sort.NewComment_Ascending" : "Sort.NewComment_Descending";  break;
				case VideoSortKey.ViewCount:
					text = isAscending ? "Sort.ViewCount_Ascending" : "Sort.ViewCount_Descending"; break;
				case VideoSortKey.MylistCount:
					text = isAscending ? "Sort.MylistCount_Ascending" : "Sort.MylistCount_Descending"; break;
				case VideoSortKey.CommentCount:
					text = isAscending ? "Sort.CommentCount_Ascending" : "Sort.CommentCount_Descending"; break;
				case VideoSortKey.FirstRetrieve:
					text = isAscending ? "Sort.FirstRetrieve_Ascending" : "Sort.FirstRetrieve_Descending"; break;
				case VideoSortKey.Length:
					text = isAscending ? "Sort.Length_Ascending" : "Sort.Length_Descending"; break;
				case VideoSortKey.Popurarity:
					text = isAscending ? "Sort.Popurarity_Ascending" : "Sort.Popurarity_Descending"; break;
				case VideoSortKey.MylistPopurarity:
					text = isAscending ? "Sort.MylistPopurarity_Ascending" : "Sort.MylistPopurarity_Descending"; break;
				case VideoSortKey.VideoCount:
					text = isAscending ? "Sort.VideoCount_Ascending" : "Sort.VideoCount_Descending"; break;
				case VideoSortKey.Relation:
					text = isAscending ? "Sort.Relation_Ascending" : "Sort.Relation_Descending"; break;
				default:
					throw new NotSupportedException();
			}

			return text.Translate();
		}

	}
}

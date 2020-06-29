using Hohoema.Models.Repository.Niconico;
using I18NPortable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.ViewModels
{
	public static class SortHelper
	{
		public static string ToCulturizedText(Sort sort, Order order)
		{
			var isAscending = order == Order.Ascending;
			string text;
			switch (sort)
			{
				case Sort.NewComment:
					text = isAscending ? "Sort.NewComment_Ascending" : "Sort.NewComment_Descending";  break;
				case Sort.ViewCount:
					text = isAscending ? "Sort.ViewCount_Ascending" : "Sort.ViewCount_Descending"; break;
				case Sort.MylistCount:
					text = isAscending ? "Sort.MylistCount_Ascending" : "Sort.MylistCount_Descending"; break;
				case Sort.CommentCount:
					text = isAscending ? "Sort.CommentCount_Ascending" : "Sort.CommentCount_Descending"; break;
				case Sort.FirstRetrieve:
					text = isAscending ? "Sort.FirstRetrieve_Ascending" : "Sort.FirstRetrieve_Descending"; break;
				case Sort.Length:
					text = isAscending ? "Sort.Length_Ascending" : "Sort.Length_Descending"; break;
				case Sort.Popurarity:
					text = isAscending ? "Sort.Popurarity_Ascending" : "Sort.Popurarity_Descending"; break;
				case Sort.MylistPopurarity:
					text = isAscending ? "Sort.MylistPopurarity_Ascending" : "Sort.MylistPopurarity_Descending"; break;
				case Sort.VideoCount:
					text = isAscending ? "Sort.VideoCount_Ascending" : "Sort.VideoCount_Descending"; break;
				case Sort.UpdateTime:
					text = isAscending ? "Sort.UpdateTime_Ascending" : "Sort.UpdateTime_Descending"; break;
				case Sort.Relation:
					text = isAscending ? "Sort.Relation_Ascending" : "Sort.Relation_Descending"; break;
				default:
					throw new NotSupportedException();
			}

			return text.Translate();
		}

		public static string ToCulturizedText(CommunitySearchSort sort, Order order)
		{
			var isAscending = order == Order.Ascending;
			string text;
			switch (sort)
			{
				case CommunitySearchSort.CreatedAt:
					text = isAscending ? "CommunitySearchSort.CreatedAt_Ascending" : "CommunitySearchSort.CreatedAt_Descending"; break;
				case CommunitySearchSort.UpdateAt:
					text = isAscending ? "CommunitySearchSort.UpdateAt_Ascending" : "CommunitySearchSort.UpdateAt_Descending"; break;
				case CommunitySearchSort.CommunityLevel:
					text = isAscending ? "CommunitySearchSort.CommunityLevel_Ascending" : "CommunitySearchSort.CommunityLevel_Descending"; break;
				case CommunitySearchSort.MemberCount:
					text = isAscending ? "CommunitySearchSort.MemberCount_Ascending" : "CommunitySearchSort.MemberCount_Descending"; break;
				case CommunitySearchSort.VideoCount:
					text = isAscending ? "CommunitySearchSort.VideoCount_Ascending" : "CommunitySearchSort.VideoCount_Descending"; break;
				default:
					throw new NotSupportedException();
			}
			return text.Translate();
		}

		public static string ToCulturizedText(LiveSearchSortType sort)
		{
			var isAscending = sort.HasFlag(LiveSearchSortType.SortAcsending);

			sort = isAscending ? (LiveSearchSortType)(sort - LiveSearchSortType.SortAcsending) : sort;
			string text;
			switch (sort)
			{
				case LiveSearchSortType.StartTime:
					text = isAscending ? "NicoliveSearchSort.Recent_Ascending" : "NicoliveSearchSort.Recent_Descending"; break;
				case LiveSearchSortType.CommentCounter:
					text = isAscending ? "NicoliveSearchSort.Comment_Ascending" : "NicoliveSearchSort.Comment_Descending"; break;
				default:
					throw new NotSupportedException();
			}

			return text.Translate();
		}
	}
}

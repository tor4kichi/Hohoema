using Mntone.Nico2;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Helpers
{
	public static class SortHelper
	{
		public static string ToCulturizedText(Sort sort, Order order)
		{
			var isAscending = order == Order.Ascending;
			switch (sort)
			{
				case Sort.NewComment:
					return isAscending ? "コメントが古い順" : "コメントが新しい順";
				case Sort.ViewCount:
					return isAscending ? "再生数が少ない順" : "再生数が多い順";
				case Sort.MylistCount:
					return isAscending ? "マイリスト数が少ない順" : "マイリスト数が多い順";
				case Sort.CommentCount:
					return isAscending ? "コメント数が少ない順" : "コメント数が多い順";
				case Sort.FirstRetrieve:
					return isAscending ? "投稿日時が古い順" : "投稿日時が新しい順";
				case Sort.Length:
					return isAscending ? "動画時間が短い順" : "動画時間が長い順";
				case Sort.Popurarity:
					return isAscending ? "人気が低い順" : "人気が高い順";
				case Sort.MylistPopurarity:
					return isAscending ? "人気が低い順" : "人気が高い順";
				case Sort.VideoCount:
					return isAscending ? "動画数が少ない順" : "動画数が多い順";
				case Sort.UpdateTime:
					return isAscending ? "更新が古い順" : "更新が新しい順";
				case Sort.Relation:
					return isAscending ? "適合率が低い順" : "適合率が高い順";
				default:
					throw new NotSupportedException();
			}
		}

		public static string ToCulturizedText(CommunitySearchSort sort, Order order)
		{
			var isAscending = order == Order.Ascending;
			switch (sort)
			{
				case CommunitySearchSort.CreatedAt:
					return isAscending ? "作成が古い順" : "作成が新しい順";
				case CommunitySearchSort.UpdateAt:
					return isAscending ? "更新が古い順" : "更新が新しい順";
				case CommunitySearchSort.CommunityLevel:
					return isAscending ? "レベルが小さい順" : "レベルが大きい順";
				case CommunitySearchSort.MemberCount:
					return isAscending ? "登録メンバーが少ない順" : "登録メンバーが多い順";
				case CommunitySearchSort.VideoCount:
					return isAscending ? "投稿動画数が少ない順" : "投稿動画数が多い順";
				default:
					throw new NotSupportedException();
			}
		}

		public static string ToCulturizedText(NicoliveSearchSort sort, Order order)
		{
			var isAscending = order == Order.Ascending;
			switch (sort)
			{
				case NicoliveSearchSort.Recent:
					return isAscending ? "放送日時が近い順" : "放送日時が遠い順";
				case NicoliveSearchSort.Comment:
					return isAscending ? "コメントが多い順" : "コメントが少ない順";
				default:
					throw new NotSupportedException();
			}
		}
	}
}

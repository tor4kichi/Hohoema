using Mntone.Nico2.Videos.Ranking;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Util
{
	public static class RankingTargetExtention
	{
		public static string ToCultulizedText(this RankingTarget target)
		{
			switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
			{
				case "ja":
					return GetJaText(target);

				case "en":
				default:
					var s = target.ToString();
					var first = s.Substring(0, 1);
					return first.ToUpper() + s.Substring(1, s.Length - 1);
			}
		}


		private static string GetJaText(RankingTarget target)
		{
			switch (target)
			{
				case RankingTarget.view:
					return "再生数";
				case RankingTarget.res:
					return "コメント数";
				case RankingTarget.mylist:
					return "マイリスト数";
				default:
					break;
			}

			return "";
		}
	}




	public static class RankingTimeSpanExtention
	{
		public static string ToCultulizedText(this RankingTimeSpan timeSpan)
		{
			switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
			{
				case "ja":
					return GetJaText(timeSpan);

				case "en":
				default:
					var s = timeSpan.ToString();
					var first = s.Substring(0, 1);
					return first.ToUpper() + s.Substring(1, s.Length - 1);
			}
		}


		private static string GetJaText(RankingTimeSpan timeSpan)
		{
			switch (timeSpan)
			{
				case RankingTimeSpan.hourly:
					return "毎時";
				case RankingTimeSpan.daily:
					return "日";
				case RankingTimeSpan.weekly:
					return "週";
				case RankingTimeSpan.monthly:
					return "月";
				case RankingTimeSpan.total:
					return "トータル";
				default:
					break;
			}

			return "";
		}
	}

	public static class RankingCategoryExtention
	{
		public static string ToCultulizedText(this RankingCategory category)
		{
			switch (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
			{
				case "ja":
					return GetJaText(category);

				case "en":
				default:
					var s = category.ToString();
					var first = s.Substring(0, 1);
					return first.ToUpper() + s.Substring(1, s.Length - 1);
			}
		}

		private static string GetJaText(RankingCategory category)
		{
			switch (category)
			{
				case RankingCategory.all:
					return "カテゴリ合算";
				case RankingCategory.g_ent2:
					return "エンタメ・音楽";
				case RankingCategory.ent:
					return "エンターテイメント";
				case RankingCategory.music:
					return "音楽";
				case RankingCategory.sing:
					return "歌ってみた";
				case RankingCategory.dance:
					return "踊ってみた";
				case RankingCategory.play:
					return "演奏してみた";
				case RankingCategory.vocaloid:
					return "VOCALOID";
				case RankingCategory.nicoindies:
					return "ニコニコインディーズ";
				case RankingCategory.g_life2:
					return "生活・一般・スポ";
				case RankingCategory.animal:
					return "動物";
				case RankingCategory.cooking:
					return "料理";
				case RankingCategory.nature:
					return "自然";
				case RankingCategory.travel:
					return "旅行";
				case RankingCategory.sport:
					return "スポーツ";
				case RankingCategory.lecture:
					return "ニコニコ動画講座";
				case RankingCategory.drive:
					return "車載動画";
				case RankingCategory.history:
					return "歴史";
				case RankingCategory.g_politics:
					return "政治";
				case RankingCategory.g_tech:
					return "科学・技術";
				case RankingCategory.science:
					return "科学";
				case RankingCategory.tech:
					return "ニコニコ技術部";
				case RankingCategory.handcraft:
					return "ニコニコ手芸部";
				case RankingCategory.make:
					return "作ってみた";
				case RankingCategory.g_culture2:
					return "アニメ・ゲーム・絵";
				case RankingCategory.anime:
					return "アニメ";
				case RankingCategory.game:
					return "ゲーム";
				case RankingCategory.jikkyo:
					return "実況プレイ動画";
				case RankingCategory.toho:
					return "東方";
				case RankingCategory.imas:
					return "アイドルマスター";
				case RankingCategory.radio:
					return "ラジオ";
				case RankingCategory.draw:
					return "描いてみた";
				case RankingCategory.g_other:
					return "その他（合算）";
				case RankingCategory.are:
					return "例のアレ";
				case RankingCategory.diary:
					return "日記";
				case RankingCategory.other:
					return "その他";
				default:
					return "？";
			}
		}
	}
}

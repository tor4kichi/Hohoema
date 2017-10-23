using Mntone.Nico2.Videos.Ranking;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace NicoPlayerHohoema.Helpers
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
        static ResourceLoader _resourceLoader = ResourceLoader.GetForViewIndependentUse();


        public static string ToCultulizedText(this RankingCategory category)
		{
            return _resourceLoader.GetString(category.ToString());
		}
	}
}

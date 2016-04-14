using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NicoPlayerHohoema.Mntone.Nico2.Ranking
{
	public sealed class NiconicoRanking
	{
		public const string NiconicoRankingDomain = "http://www.nicovideo.jp/ranking/";

		internal static string MakeRankingUrlParameters(RankingTarget target, RankingTimeSpan timeSpan, RankingCategory category)
		{
			var _target = target.ToString();
			var _timeSpan = timeSpan.ToString();
			var _category = category.ToString();

			return $"{_target}/{_timeSpan}/{_category}?rss=2.0";
        }

		public static async Task<NiconicoRankingRss> GetRankingData(RankingTarget target, RankingTimeSpan timeSpan, RankingCategory category)
		{
			var rssUrl = NiconicoRankingDomain + MakeRankingUrlParameters(target, timeSpan, category);

			//			var rssParameters = Uri.EscapeUriString();

			try
			{
				using (HttpClient client = new HttpClient())
				{
					HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, rssUrl);

					request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ja", 0.5));
					request.Headers.UserAgent.Add(new ProductInfoHeaderValue("NicoPlayerHohoema_UWP", "1.0"));

					var result = await client.SendAsync(request);
					using (var contentStream = await result.Content.ReadAsStreamAsync())
					{
						var serializer = new XmlSerializer(typeof(NiconicoRankingRss));

						return (NiconicoRankingRss)serializer.Deserialize(contentStream);
					}
				}
			}
			catch (HttpRequestException reqException)
			{

			}
			catch (Exception e)
			{

			}

			return null;
		}
	}

	// see@ http://nicowiki.com/?RSS%E3%83%95%E3%82%A3%E3%83%BC%E3%83%89%E4%B8%80%E8%A6%A7

	public enum RankingTarget
	{
		view,
		res,
		mylist,
		
	}

	public enum RankingTimeSpan
	{
		hourly,
		daily,
		weekly,
		monthly,
		total,
	}

	public enum RankingCategory
	{
		all,
		music,
		ent,
		anime,
		game,
		animal,
		que,
		radio,
		sport,
		politics,
		chat,
		science,
		history,
		cooking,
		nature,
		diary,
		dance,
		sing,
		play,
		lecture,
		owner,
		tw,
		other,
		test,
		r18
	}




	public static class RankingTargetExtention
	{
		public static string ToCultulizedText(this RankingTarget target)
		{
			return target.ToString();
		}
	}

	public static class RankingTimeSpanExtention
	{
		public static string ToCultulizedText(this RankingTimeSpan timeSpan)
		{
			return timeSpan.ToString();
		}
	}

	public static class RankingCategoryExtention
	{
		public static string ToCultulizedText(this RankingCategory category)
		{
			return category.ToString();
		}
	}
}

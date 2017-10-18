using Mntone.Nico2.Videos.Ranking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Helpers
{
	public static class NicoVideoExtention
	{
		public static string GetVideoId(this NiconicoVideoRssItem rssItem)
		{
			return UrlToVideoId(rssItem.VideoUrl);
		}
		public static string UrlToVideoId(string url)
		{
			return url.Split('/').Last();
		}
		public static string VideoIdToWatchPageUrl(string id)
		{
			return Mntone.Nico2.NiconicoUrls.VideoWatchPageUrl + id;
		}
	}
}

﻿using NiconicoToolkit.Ranking.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Helpers
{
	public static class NicoVideoIdHelper
    {
		public static string UrlToVideoId(string url)
		{
			return url.Split('/').Last();
		}
		public static string VideoIdToWatchPageUrl(string id)
		{
			return $"https://www.nicovideo.jp/watch/{id}";
		}
	}
}

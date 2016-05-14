using Mntone.Nico2;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Search;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// 検索やランキングなどコンテンツを見つける機能をサポートします
	/// </summary>
	public class NiconicoContentFinder : BindableBase
	{
		public NiconicoContentFinder(HohoemaApp app)
		{
			_HohoemaApp = app;
		}


		public async Task<NiconicoRankingRss> GetCategoryRanking(RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await NiconicoRanking.GetRankingData(target, timeSpan, category);
			});
		}


		public async Task<SearchResponse> GetKeywordSearch(string keyword, uint pageCount, SearchSortMethod sortMethod, SortDirection sortDir = SortDirection.Descending)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Video.GetKeywordSearchAsync(keyword, pageCount, sortMethod, sortDir);
			});
		}

		public async Task<SearchResponse> GetTagSearch(string keyword, uint pageCount, SearchSortMethod sortMethod, SortDirection sortDir = SortDirection.Descending)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Video.GetKeywordSearchAsync(keyword, pageCount, sortMethod, sortDir);
			});
		}

		HohoemaApp _HohoemaApp;
	}
}

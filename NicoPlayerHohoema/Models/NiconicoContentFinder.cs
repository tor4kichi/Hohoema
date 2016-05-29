using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Users.Fav;
using Mntone.Nico2.Users.Video;
using Mntone.Nico2.Videos.Histories;
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


		public async Task Initialize()
		{
			// お気に入りデータの読み込み
		}


		public async Task<NiconicoRankingRss> GetCategoryRanking(RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await NiconicoRanking.GetRankingData(target, timeSpan, category);
			});
		}


		public async Task<SearchResponse> GetKeywordSearch(string keyword, uint pageCount, SortMethod sortMethod, SortDirection sortDir = SortDirection.Descending)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Video.GetKeywordSearchAsync(keyword, pageCount, sortMethod, sortDir);
			});
		}

		public async Task<SearchResponse> GetTagSearch(string keyword, uint pageCount, SortMethod sortMethod, SortDirection sortDir = SortDirection.Descending)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Video.GetKeywordSearchAsync(keyword, pageCount, sortMethod, sortDir)
					.ContinueWith(prevTask =>
					{
						if (!prevTask.Result.IsStatusOK)
						{
							throw new Exception();
						}
						else
						{
							return prevTask.Result;
						}
					});
			});
		}

		public async Task<List<MylistGroupData>> GetLoginUserMylistGroups()
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetMylistGroupListAsync();
			});
		}


		public async Task<List<MylistGroupData>> GetUserMylistGroups(string userId)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetUserMylistGroupAsync(userId);
			})
			.ContinueWith(prevTask => 
			{
				if (prevTask.IsCompleted && prevTask.Result != null)
				{
					_CachedUserMylistGroupDatum = prevTask.Result;
					return prevTask.Result;
				}
				else
				{
					return _CachedUserMylistGroupDatum;
				}
			});
		}

		private List<MylistGroupData> _CachedUserMylistGroupDatum = null;


		public async Task<MylistGroupDetail> GetMylist(string mylistGroupid)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetMylistGroupDetailAsync(mylistGroupid);
			});
		}

		public async Task<MylistListResponse> GetMylistItems(string mylistGroupid, uint from = 0, uint limit = 50)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetMylistListAsync(mylistGroupid, from, limit);
			});
		}




		public async Task<HistoriesResponse> GetHistory()
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Video.GetHistoriesAsync();
			});	
		}

		
		public async Task<List<FavData>> GetFavUsers()
		{
			return await _HohoemaApp.NiconicoContext.User.GetFavUsersAsync();
		}


		public async Task<List<string>> GetFavTags()
		{
			return await _HohoemaApp.NiconicoContext.User.GetFavTagsAsync();
		}

		public async Task<List<FavData>> GetFavMylists()
		{
			return await _HohoemaApp.NiconicoContext.User.GetFavMylistsAsync();
		}



		public async Task<UserVideoResponse> GetUserVideos(uint userId, uint page, SortMethod sortMethod = SortMethod.FirstRetrieve, SortDirection sortDir = SortDirection.Descending)
		{
			return await _HohoemaApp.NiconicoContext.User.GetUserVideos(userId, page, sortMethod, sortDir);
		}




		HohoemaApp _HohoemaApp;
	}
}

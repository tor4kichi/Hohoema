using Mntone.Nico2;
using Mntone.Nico2.Communities.Detail;
using Mntone.Nico2.Communities.Info;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Users.Fav;
using Mntone.Nico2.Users.FavCommunity;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Users.Video;
using Mntone.Nico2.Videos.Histories;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// 検索やランキングなどコンテンツを見つける機能をサポートします
	/// </summary>
	public class NiconicoContentFinder : BindableBase
	{
		AsyncLock _NicoPageAccessLock = new AsyncLock();

		SemaphoreSlim _ThumbnailAccessLock = new SemaphoreSlim(1, 3);

		public NiconicoContentFinder(HohoemaApp app)
		{
			_HohoemaApp = app;
		}


		public Task Initialize()
		{
			return Task.CompletedTask;
		}

		public async Task<ThumbnailResponse> GetThumbnailResponse(string rawVideoId)
		{
			try
			{
				await _ThumbnailAccessLock.WaitAsync();
				ThumbnailResponse res = null;

				res = await Util.ConnectionRetryUtil.TaskWithRetry(async () =>
				{
					return await _HohoemaApp.NiconicoContext.Video.GetThumbnailAsync(rawVideoId);
				});

				if (res != null)
				{
					await UserInfoDb.AddOrReplaceAsync(res.UserId.ToString(), res.UserName, res.UserIconUrl.AbsoluteUri);
				}

				return res;
			}
			finally
			{
				_ThumbnailAccessLock.Release();
			}
		}

		public async Task<WatchApiResponse> GetWatchApiResponse(string rawVideoId, bool forceLowQuality = false, HarmfulContentReactionType harmfulContentReaction = HarmfulContentReactionType.None)
		{
			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				var res = await Util.ConnectionRetryUtil.TaskWithRetry(() =>
				{
					return _HohoemaApp.NiconicoContext.Video.GetWatchApiAsync(
						rawVideoId
						, forceLowQuality: forceLowQuality
						, harmfulReactType: harmfulContentReaction
						);
				});

				if (res != null && res.UploaderInfo != null)
				{
					var uploaderInfo = res.UploaderInfo;
					await UserInfoDb.AddOrReplaceAsync(uploaderInfo.id, uploaderInfo.nickname, uploaderInfo.icon_url);
				}

				return res;
			}
			
		}


		public async Task<User> GetUserInfo(string userId)
		{
			var user = await ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return _HohoemaApp.NiconicoContext.User.GetUserAsync(userId);
			});

			if (user != null)
			{
				await UserInfoDb.AddOrReplaceAsync(userId, user.Nickname, user.ThumbnailUrl);
			}

			return user;
		}

		public async Task<UserDetail> GetUserDetail(string userId)
		{
			var userDetail = await ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return _HohoemaApp.NiconicoContext.User.GetUserDetail(userId);
			});

			if (userDetail != null)
			{
				await UserInfoDb.AddOrReplaceAsync(userId, userDetail.Nickname, userDetail.ThumbnailUri);
			}

			return userDetail;
		}


		public async Task<NiconicoRankingRss> GetCategoryRanking(RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await NiconicoRanking.GetRankingData(target, timeSpan, category);
			});
		}


		public async Task<VideoListingResponse> GetKeywordSearch(string keyword, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Search.VideoSearchWithKeywordAsync(keyword, from, limit, sort, order);
			});
		}

		public async Task<VideoListingResponse> GetTagSearch(string tag, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Search.VideoSearchWithTagAsync(tag, from, limit, sort, order)
					.ContinueWith(prevTask =>
					{
						if (!prevTask.Result.IsOK)
						{
							throw new WebException();
						}
						else
						{
							return prevTask.Result;
						}
					});
			}, retryInterval:2000);
		}


		public async Task<Mntone.Nico2.Searches.Live.NicoliveVideoResponse> LiveSearchAsync(
			string word,
			bool isTagSearch,
			Mntone.Nico2.Live.CommunityType? provider = null,
			uint from = 0,
			uint length = 30,
			Order? order = null,
			Mntone.Nico2.Searches.Live.NicoliveSearchSort? sort = null,
			Mntone.Nico2.Searches.Live.NicoliveSearchMode? mode = null
			)
		{
			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.Search.LiveSearchAsync(
					word,
					isTagSearch,
					provider,
					from,
					length,
					order,
					sort,
					mode
					);
			}
		}


		public Task<List<LoginUserMylistGroup>> GetLoginUserMylistGroups()
		{
			return ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return _HohoemaApp.NiconicoContext.User.GetMylistGroupListAsync();
			})
			.ContinueWith(prevResult => 
			{
				return prevResult.Result.Cast<LoginUserMylistGroup>().ToList();
			});
		}


		public Task<List<MylistGroupData>> GetUserMylistGroups(string userId)
		{
			return ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				try
				{
					return await _HohoemaApp.NiconicoContext.Mylist.GetUserMylistGroupAsync(userId);
				}
				catch (Exception e) when (e.Message.Contains("Forbidden"))
				{
					return new List<MylistGroupData>();
				}
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


		public async Task<MylistGroupDetailResponse> GetMylistGroupDetail(string mylistGroupid)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetMylistGroupDetailAsync(mylistGroupid);
			});
		}

		public async Task<MylistGroupVideoResponse> GetMylistGroupVideo(string mylistGroupid, uint from = 0, uint limit = 50)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetMylistGroupVideoAsync(mylistGroupid, from, limit);
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
			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFavUsersAsync();
			}
		}


		public async Task<List<string>> GetFavTags()
		{
			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFavTagsAsync();
			}
		}

		public async Task<List<FavData>> GetFavMylists()
		{
			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFavMylistsAsync();
			}
		}


		public async Task<FavCommunityResponse> GetFavCommunities()
		{
			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFavCommunityAsync();
			}
		}


		public async Task<UserVideoResponse> GetUserVideos(uint userId, uint page, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetUserVideos(userId, page, sort, order);
			}
		}

		public async Task<NicoVideoResponse> GetRelatedVideos(string videoId, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
			return await _HohoemaApp.NiconicoContext.Video.GetRelatedVideoAsync(videoId, from, limit, sort, order);
		}




		public async Task<CommunitySearchResponse> SearchCommunity(
			string keyword
			, uint page
			, CommunitySearchSort sort = CommunitySearchSort.CreatedAt
			, Order order = Order.Descending
			, CommunitySearchMode mode = CommunitySearchMode.Keyword
			)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				using (var releaser = await _NicoPageAccessLock.LockAsync())
				{
					return await _HohoemaApp.NiconicoContext.Search.CommunitySearchAsync(keyword, page, sort, order, mode);
				}
			});
		}


		public Task<NicovideoCommunityResponse> GetCommunityInfo(
			string communityId
			)
		{
			return _HohoemaApp.NiconicoContext.Community.GetCommunifyInfoAsync(communityId);
		}


		public Task<CommunityDetailResponse> GetCommunityDetail(
			string communityId
			)
		{
			return ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				using (var releaser = await _NicoPageAccessLock.LockAsync())
				{
					return await _HohoemaApp.NiconicoContext.Community.GetCommunityDetailAsync(communityId);
				}
			});
		}


		HohoemaApp _HohoemaApp;
	}
}

using Mntone.Nico2;
using Mntone.Nico2.Communities.Detail;
using Mntone.Nico2.Communities.Info;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Mylist.MylistGroup;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Video;
using Mntone.Nico2.Users.Follow;
using Mntone.Nico2.Users.FollowCommunity;
using Mntone.Nico2.Users.User;
using Mntone.Nico2.Users.Video;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.Histories;
using Mntone.Nico2.Videos.Ranking;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Models.Db;
using NicoPlayerHohoema.Util;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		DateTime LastPageApiAccessTime = DateTime.MinValue;
		static TimeSpan PageAccessMinimumInterval = TimeSpan.FromSeconds(0.5);


        AsyncLock _ThumbnailAccessLock = new AsyncLock();


        public NiconicoContentFinder(HohoemaApp app)
		{
			_HohoemaApp = app;
		}

		private async Task WaitNicoPageAccess()
		{
			var duration = DateTime.Now - LastPageApiAccessTime;
			if (duration < PageAccessMinimumInterval)
			{
				await Task.Delay(PageAccessMinimumInterval - duration);
			}

			LastPageApiAccessTime = DateTime.Now;
		}


		public Task Initialize()
		{
			return Task.CompletedTask;
		}

		public async Task<ThumbnailResponse> GetThumbnailResponse(string rawVideoId)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            using (var releaser = await _ThumbnailAccessLock.LockAsync())
            {
                ThumbnailResponse res = null;

                res = await Util.ConnectionRetryUtil.TaskWithRetry(() =>
                {
                    return _HohoemaApp.NiconicoContext.Video.GetThumbnailAsync(rawVideoId);
                }, 
                retryCount:5,
                retryInterval:1000
                );
                
                if (res != null)
                {
                    await UserInfoDb.AddOrReplaceAsync(res.UserId.ToString(), res.UserName, res.UserIconUrl.AbsoluteUri);
                    Debug.WriteLine("サムネ取得:" + rawVideoId);
                }
                else
                {
                    Debug.WriteLine("サムネ取得失敗:" + rawVideoId);
                }

                return res;
            }
		}

		public async Task<DmcWatchResponse> GetDmcWatchResponse(string rawVideoId, HarmfulContentReactionType harmfulContentReaction = HarmfulContentReactionType.None)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();


			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				var res = await Util.ConnectionRetryUtil.TaskWithRetry(() =>
				{
					return _HohoemaApp.NiconicoContext.Video.GetDmcWatchResponseAsync(
						rawVideoId
						, harmfulReactType: harmfulContentReaction
						);
				});

				if (res != null && res.Owner != null)
				{
					var uploaderInfo = res.Owner;
					await UserInfoDb.AddOrReplaceAsync(uploaderInfo.Id, uploaderInfo.Nickname, uploaderInfo.IconURL);
				}

                if (res != null)
                {
                    var data = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(rawVideoId);
                    if (data != null)
                    {
                        await VideoInfoDb.UpdateNicoVideoInfo(data, res);
                    }
                }

				return res;
			}
			
		}

        public async Task<WatchApiResponse> GetWatchApiResponse(string rawVideoId, bool forceLowQuality = false, HarmfulContentReactionType harmfulContentReaction = HarmfulContentReactionType.None)
        {
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();


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

                if (res != null)
                {
                    var data = await VideoInfoDb.GetEnsureNicoVideoInfoAsync(rawVideoId);
                    if (data != null)
                    {
                        await VideoInfoDb.UpdateNicoVideoInfo(data, res);
                    }
                }

                return res;
            }

        }


        public async Task<UserDetail> GetUserInfo(string userId)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            await WaitNicoPageAccess();



			var user = await ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return _HohoemaApp.NiconicoContext.User.GetUserDetail(userId);
			});

			if (user != null)
			{
				await UserInfoDb.AddOrReplaceAsync(userId, user.Nickname, user.ThumbnailUri);
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


		public async Task<NiconicoVideoRss> GetCategoryRanking(RankingCategory category, RankingTarget target, RankingTimeSpan timeSpan)
		{
			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await NiconicoRanking.GetRankingData(target, timeSpan, category);
			});
		}


		public async Task<VideoListingResponse> GetKeywordSearch(string keyword, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Search.VideoSearchWithKeywordAsync(keyword, from, limit, sort, order);
			}
			, retryInterval:1000);
		}

		public async Task<VideoListingResponse> GetTagSearch(string tag, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

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
			}, retryInterval: 1000);
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
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

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


		public async Task<List<LoginUserMylistGroup>> GetLoginUserMylistGroups()
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();


			return await ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return _HohoemaApp.NiconicoContext.User.GetMylistGroupListAsync();
			})
			.ContinueWith(prevResult => 
			{
				return prevResult.Result.Cast<LoginUserMylistGroup>().ToList();
			});
		}


		public async Task<List<MylistGroupData>> GetUserMylistGroups(string userId)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            await WaitNicoPageAccess();


			return await ConnectionRetryUtil.TaskWithRetry(async () =>
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
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetMylistGroupDetailAsync(mylistGroupid);
			});
		}

		public async Task<MylistGroupVideoResponse> GetMylistGroupVideo(string mylistGroupid, uint from = 0, uint limit = 50)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Mylist.GetMylistGroupVideoAsync(mylistGroupid, from, limit);
			});
		}




		public async Task<HistoriesResponse> GetHistory()
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }

            await WaitNicoPageAccess();

			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Video.GetHistoriesAsync();
			});	
		}

		
		public async Task<List<FollowData>> GetFollowUsers()
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFollowUsersAsync();
			}
		}


		public async Task<List<string>> GetFavTags()
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFollowTagsAsync();
			}
		}

		public async Task<List<FollowData>> GetFavMylists()
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFollowMylistsAsync();
			}
		}


		public async Task<FollowCommunityResponse> GetFavCommunities()
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetFollowCommunityAsync();
			}
		}


		public async Task<UserVideoResponse> GetUserVideos(uint userId, uint page, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await _HohoemaApp.NiconicoContext.User.GetUserVideos(userId, page, sort, order);
			}
		}

		public async Task<NicoVideoResponse> GetRelatedVideos(string videoId, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

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
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


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
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            return _HohoemaApp.NiconicoContext.Community.GetCommunifyInfoAsync(communityId);
		}


		public async Task<CommunityDetailResponse> GetCommunityDetail(
			string communityId
			)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();


			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				using (var releaser = await _NicoPageAccessLock.LockAsync())
				{
					return await _HohoemaApp.NiconicoContext.Community.GetCommunityDetailAsync(communityId);
				}
			});
		}


		public Task<NiconicoVideoRss> GetCommunityVideo(
			string communityId,
			uint page
			)
		{
            if (_HohoemaApp.NiconicoContext == null)
            {
                return null;
            }

            if (!_HohoemaApp.IsLoggedIn)
            {
                return null;
            }


            return ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await _HohoemaApp.NiconicoContext.Community.GetCommunityVideoAsync(communityId, page);
			});
		}

		HohoemaApp _HohoemaApp;
	}
}

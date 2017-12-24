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
using NicoPlayerHohoema.Helpers;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NicoPlayerHohoema.Database;
using Microsoft.Practices.Unity;

namespace NicoPlayerHohoema.Models
{
	/// <summary>
	/// 検索やランキングなどコンテンツを見つける機能をサポートします
	/// </summary>
	public class NiconicoContentProvider 
	{
		AsyncLock _NicoPageAccessLock = new AsyncLock();
		DateTime LastPageApiAccessTime = DateTime.MinValue;
		static TimeSpan PageAccessMinimumInterval = TimeSpan.FromSeconds(0.5);


        static TimeSpan ThumbnailExpirationSpan { get; set; } = TimeSpan.FromMinutes(30);

        AsyncLock _ThumbnailAccessLock = new AsyncLock();

        public NiconicoContext Context { get; set; }

        public NiconicoContentProvider()
		{
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


        /// <summary>
        /// ニコニコ動画コンテンツの情報を取得します。
        /// 内部DB、サムネイル、Watchページのアクセス情報から更新されたデータを提供します。
        /// 
        /// </summary>
        /// <param name="rawVideoId"></param>
        /// <returns></returns>
        public async Task<Database.NicoVideo> GetNicoVideoInfo(string rawVideoId, bool requireLatest = false)
		{
            if (Context == null)
            {
                return null;
            }

            using (var releaser = await _ThumbnailAccessLock.LockAsync())
            {
                var info = NicoVideoDb.Get(rawVideoId);

                // 最新情報が不要な場合は内部DBのキャッシュをそのまま返す
                if (info != null && !requireLatest)
                {
                    if (info.LastUpdated > DateTime.Now - ThumbnailExpirationSpan)
                    {
                        return info;
                    }
                }

                if (info == null)
                {
                    info = new Database.NicoVideo()
                    {
                        RawVideoId = rawVideoId
                    };
                }

                try
                {
                    var res = await Helpers.ConnectionRetryUtil.TaskWithRetry(() =>
                    {
                        return Context.Search.GetVideoInfoAsync(rawVideoId);
                    },
                    retryCount: 5,
                    retryInterval: 1000
                    );

                    if (res.Status == "ok")
                    {
                        var video = res.Video;

                        info.Title = video.Title;
                        info.VideoId = video.Id;
                        info.Length = video.Length;
                        info.PostedAt = video.FirstRetrieve;
                        info.ThumbnailUrl = video.ThumbnailUrl.OriginalString;

                        info.ViewCount = (int)video.ViewCount;
                        info.MylistCount = (int)video.MylistCount;
                        info.CommentCount = (int)res.Thread.GetCommentCount();
                        info.Tags = res.Tags.TagInfo.Select(x => new NicoVideoTag()
                        {
                            Id = x.Tag,
                        }
                        ).ToList();

                        info.Owner = new NicoVideoOwner()
                        {
                            OwnerId = res.Video.UserId,
                            UserType = res.Video.ProviderType == "regular" ? UserType.User : UserType.Channel
                        };

                        info.IsDeleted = res.Video.IsDeleted;
                        if (info.IsDeleted && int.TryParse(res.Video.__deleted, out int deleteType))
                        {
                            try
                            {
                                info.PrivateReasonType = (PrivateReasonType)deleteType;
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        info.IsDeleted = true;
                    }
                }

                catch (Exception ex) when (ex.Message.Contains("DELETE") || ex.Message.Contains("NOT_FOUND"))
                {
                    info.IsDeleted = true;
                    
                    var cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
                    await cacheManager.VideoDeletedFromNiconicoServer(info.RawVideoId).ConfigureAwait(false);
                }
                finally
                {
                    NicoVideoDb.AddOrUpdate(info);
                    if (info.Owner != null)
                    {
                        NicoVideoOwnerDb.AddOrUpdate(info.Owner);
                    }
                }

                return info;
            }
		}

		public async Task<DmcWatchData> GetDmcWatchResponse(string rawVideoId)
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            await WaitNicoPageAccess();

            // TODO: 有害動画に指定されたページにアクセスした場合の対応
            // 有害動画ページにアクセスしたら一度だけ確認ページをダイアログ表示する
            // （ユーザーのアクションによらず）再度ページを読み込んで、もう一度HurmfulContentが返ってきた場合はnullを返す

            HarmfulContentReactionType harmfulContentReactionType = HarmfulContentReactionType.None;

			using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
                try
                {
                    var data = await Helpers.ConnectionRetryUtil.TaskWithRetry(() =>
                    {
                        return Context.Video.GetDmcWatchResponseAsync(
                            rawVideoId
                            , harmfulReactType: harmfulContentReactionType
                            );
                    });

                    var res = data?.DmcWatchResponse;

                    if (res != null)
                    {
                        var info = NicoVideoDb.Get(rawVideoId);
                        if (info == null)
                        {
                            info = new Database.NicoVideo()
                            {
                                RawVideoId = rawVideoId
                            };
                        }

                        info.VideoId = res.Video.Id;
                        info.Title = res.Video.Title;
                        info.Length = TimeSpan.FromSeconds(res.Video.Duration);
                        info.PostedAt = DateTime.Parse(res.Video.PostedDateTime);
                        info.ThumbnailUrl = res.Video.ThumbnailURL;

                        info.ViewCount = res.Video.ViewCount;
                        info.MylistCount = res.Video.MylistCount;
                        info.CommentCount = res.Thread.CommentCount;

                        switch (res.Video.MovieType)
                        {
                            case @"mp4":
                                info.MovieType = MovieType.Mp4;
                                break;
                            case @"flv":
                                info.MovieType = MovieType.Flv;
                                break;
                            case @"swf":
                                info.MovieType = MovieType.Swf;
                                break;
                        }

                        info.Tags = res.Tags.Select(x => new NicoVideoTag()
                        {
                            Name = x.Name,
                            IsCategory = x.IsCategory,
                            IsLocked = x.IsLocked,
                            Id = x.Id,
                            IsDictionaryExists = x.IsDictionaryExists
                        }).ToList();


                        info.Owner = new NicoVideoOwner()
                        {
                            ScreenName = res.Owner?.Nickname ?? res.Channel?.Name,
                            IconUrl = res.Owner?.IconURL ?? res.Channel?.IconURL,
                            OwnerId = res.Owner?.Id ?? res.Channel?.GlobalId,
                            UserType = res.Channel != null ? UserType.Channel : UserType.User
                        };

                        if (data.DmcWatchResponse?.Video != null)
                        {
                            info.IsDeleted = data.DmcWatchResponse.Video.IsDeleted;
                        }

                        NicoVideoDb.AddOrUpdate(info);
                        NicoVideoOwnerDb.AddOrUpdate(info.Owner);
                    }


                    if (data.DmcWatchResponse.Video.IsDeleted)
                    {
                        var cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
                        await cacheManager.VideoDeletedFromNiconicoServer(rawVideoId).ConfigureAwait(false);
                    }

                    return data;
                }
                catch (AggregateException ea) when (ea.Flatten().InnerExceptions.Any(e => e is ContentZoningException))
                {
                    throw new NotImplementedException("not implement hurmful video content.");
                }
                catch (ContentZoningException)
                {
                    throw new NotImplementedException("not implement hurmful video content.");
                }


                
			}
			
		}

        public async Task<WatchApiResponse> GetWatchApiResponse(string rawVideoId, bool forceLowQuality = false)
        {
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            await WaitNicoPageAccess();


            // TODO: 有害動画に指定されたページにアクセスした場合の対応
            HarmfulContentReactionType harmfulContentReactionType = HarmfulContentReactionType.None;

            using (var releaser = await _NicoPageAccessLock.LockAsync())
            {
                try
                {
                    var res = await Helpers.ConnectionRetryUtil.TaskWithRetry(() =>
                    {
                        return Context.Video.GetWatchApiAsync(
                            rawVideoId
                            , forceLowQuality: forceLowQuality
                            , harmfulReactType: harmfulContentReactionType
                            );
                    });

                    var info = NicoVideoDb.Get(rawVideoId);
                    if (info == null)
                    {
                        info = new Database.NicoVideo()
                        {
                            RawVideoId = rawVideoId
                        };
                    }

                    info.VideoId = res.videoDetail.id;
                    info.Title = res.videoDetail.title;
                    info.Length = res.videoDetail.length.HasValue ? TimeSpan.FromSeconds(res.videoDetail.length.Value) : TimeSpan.Zero;
                    info.PostedAt = DateTime.Parse(res.videoDetail.postedAt);
                    info.ThumbnailUrl = res.videoDetail.thumbnail;

                    info.ViewCount = res.videoDetail.viewCount ?? 0;
                    info.MylistCount = res.videoDetail.mylistCount ?? 0;
                    info.CommentCount = res.videoDetail.commentCount ?? 0;
                    switch (res.flashvars.movie_type)
                    {
                        case @"mp4":
                            info.MovieType = MovieType.Mp4;
                            break;
                        case @"flv":
                            info.MovieType = MovieType.Flv;
                            break;
                        case @"swf":
                            info.MovieType = MovieType.Swf;
                            break;
                    }
                    info.Tags = res.videoDetail.tagList.Select(x => new NicoVideoTag()
                    {
                        Name = x.tag,
                        IsCategory = x.cat ?? false,
                        IsLocked = x.lck == "1",  /* TODO: lck 値が不明です */
                        Id = x.id,
                        IsDictionaryExists = x.dic ?? false
                    }).ToList();

                    info.Owner = new NicoVideoOwner()
                    {
                        ScreenName = res.UploaderInfo?.nickname ?? res.channelInfo?.name,
                        IconUrl = res.UploaderInfo?.icon_url ?? res.channelInfo?.icon_url,
                        OwnerId = res.UploaderInfo?.id ?? res.channelInfo?.id,
                        UserType = res.channelInfo != null ? UserType.Channel : UserType.User
                    };

                    info.IsDeleted = res.IsDeleted;
                    info.PrivateReasonType = res.PrivateReason;

                    NicoVideoDb.AddOrUpdate(info);
                    //                    NicoVideoOwnerDb.AddOrUpdate(info.Owner);

                    if (res.IsDeleted)
                    {
                        var cacheManager = App.Current.Container.Resolve<VideoCacheManager>();
                        await cacheManager.VideoDeletedFromNiconicoServer(rawVideoId).ConfigureAwait(false);
                    }

                    return res;
                }
                catch (AggregateException ea) when (ea.Flatten().InnerExceptions.Any(e => e is ContentZoningException))
                {
                    throw new NotImplementedException("not implement hurmful video content.");
                }
                catch (ContentZoningException)
                {
                    throw new NotImplementedException("not implement hurmful video content.");
                }
            }
        }


        public async Task<UserDetail> GetUserInfo(string userId)
		{
            if (Context == null)
            {
                return null;
            }

            await WaitNicoPageAccess();



			var user = await ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return Context.User.GetUserDetail(userId);
			});

			if (user != null)
			{
                var owner = NicoVideoOwnerDb.Get(userId);
                if (owner == null)
                {
                    owner = new NicoVideoOwner()
                    {
                        OwnerId = userId,
                        UserType = UserType.User
                    };
                }
                owner.ScreenName = user.Nickname;
                owner.IconUrl = user.ThumbnailUri;

                NicoVideoOwnerDb.AddOrUpdate(owner);
			}

			return user;
		}

		public async Task<UserDetail> GetUserDetail(string userId)
		{
			var userDetail = await ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return Context.User.GetUserDetail(userId);
			});

			if (userDetail != null)
			{
                var owner = NicoVideoOwnerDb.Get(userId);
                if (owner == null)
                {
                    owner = new NicoVideoOwner()
                    {
                        OwnerId = userId,
                        UserType = UserType.User
                    };
                }
                owner.ScreenName = userDetail.Nickname;
                owner.IconUrl = userDetail.ThumbnailUri;

                NicoVideoOwnerDb.AddOrUpdate(owner);
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
            if (Context == null)
            {
                return null;
            }

            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await Context.Search.VideoSearchWithKeywordAsync(keyword, from, limit, sort, order);
			}
			, retryInterval:1000);
		}

		public async Task<VideoListingResponse> GetTagSearch(string tag, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
            if (Context == null)
            {
                return null;
            }

            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await Context.Search.VideoSearchWithTagAsync(tag, from, limit, sort, order)
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
            if (Context == null)
            {
                return null;
            }

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await Context.Search.LiveSearchAsync(
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


        public Task<Mntone.Nico2.Searches.Suggestion.SuggestionResponse> GetSearchSuggestKeyword(string keyword)
        {
            return Context.Search.GetSuggestionAsync(keyword);
        }


		public async Task<List<LoginUserMylistGroup>> GetLoginUserMylistGroups()
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            await WaitNicoPageAccess();


			return await ConnectionRetryUtil.TaskWithRetry(() =>
			{
				return Context.User.GetMylistGroupListAsync();
			})
			.ContinueWith(prevResult => 
			{
				return prevResult.Result.Cast<LoginUserMylistGroup>().ToList();
			});
		}


		public async Task<List<MylistGroupData>> GetUserMylistGroups(string userId)
		{
            if (Context == null)
            {
                return null;
            }

            await WaitNicoPageAccess();


			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				try
				{
					return await Context.Mylist.GetUserMylistGroupAsync(userId);
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
            if (Context == null)
            {
                return null;
            }

            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await Context.Mylist.GetMylistGroupDetailAsync(mylistGroupid);
			});
		}

		public async Task<MylistGroupVideoResponse> GetMylistGroupVideo(string mylistGroupid, uint from = 0, uint limit = 50)
		{
            if (Context == null)
            {
                return null;
            }

            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await Context.Mylist.GetMylistGroupVideoAsync(mylistGroupid, from, limit);
			});
		}




		public async Task<HistoriesResponse> GetHistory()
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }

            await WaitNicoPageAccess();

			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await Context.Video.GetHistoriesAsync();
			});	
		}

		
		public async Task<List<FollowData>> GetFollowUsers()
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await Context.User.GetFollowUsersAsync();
			}
		}


		public async Task<List<string>> GetFavTags()
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }

            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await Context.User.GetFollowTagsAsync();
			}
		}

		public async Task<List<FollowData>> GetFavMylists()
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await Context.User.GetFollowMylistsAsync();
			}
		}


		public async Task<FollowCommunityResponse> GetFavCommunities(int page)
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            await WaitNicoPageAccess();

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await Context.User.GetFollowCommunityAsync(page);
			}
		}


		public async Task<UserVideoResponse> GetUserVideos(uint userId, uint page, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
            if (Context == null)
            {
                return null;
            }

            using (var releaser = await _NicoPageAccessLock.LockAsync())
			{
				return await Context.User.GetUserVideos(userId, page, sort, order);
			}
		}

		public async Task<NicoVideoResponse> GetRelatedVideos(string videoId, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
		{
            if (Context == null)
            {
                return null;
            }

            return await Context.Video.GetRelatedVideoAsync(videoId, from, limit, sort, order);
		}




		public async Task<CommunitySearchResponse> SearchCommunity(
			string keyword
			, uint page
			, CommunitySearchSort sort = CommunitySearchSort.CreatedAt
			, Order order = Order.Descending
			, CommunitySearchMode mode = CommunitySearchMode.Keyword
			)
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				using (var releaser = await _NicoPageAccessLock.LockAsync())
				{
					return await Context.Search.CommunitySearchAsync(keyword, page, sort, order, mode);
				}
			});
		}


		public Task<NicovideoCommunityResponse> GetCommunityInfo(
			string communityId
			)
		{
            if (Context == null)
            {
                return null;
            }

            return Context.Community.GetCommunifyInfoAsync(communityId);
		}


		public async Task<CommunityDetailResponse> GetCommunityDetail(
			string communityId
			)
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            await WaitNicoPageAccess();


			return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				using (var releaser = await _NicoPageAccessLock.LockAsync())
				{
					return await Context.Community.GetCommunityDetailAsync(communityId);
				}
			});
		}


		public async Task<NiconicoVideoRss> GetCommunityVideo(
			string communityId,
			uint page
			)
		{
            if (Context == null)
            {
                return null;
            }

            if (await Context.GetIsSignedInAsync() != NiconicoSignInStatus.Success)
            {
                return null;
            }


            return await ConnectionRetryUtil.TaskWithRetry(async () =>
			{
				return await Context.Community.GetCommunityVideoAsync(communityId, page);
			});
		}
   	}
}

using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.Recommend;
using Mntone.Nico2.Videos.Thumbnail;
using Mntone.Nico2.Videos.WatchAPI;
using NicoPlayerHohoema.Database;
using NicoPlayerHohoema.Models.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class NicoVideoProvider : ProviderBase
    {
        public NicoVideoProvider(
            NiconicoSession niconicoSession
            )
            : base(niconicoSession)
        {
        }

        static TimeSpan ThumbnailExpirationSpan { get; set; } = TimeSpan.FromMinutes(30);

        AsyncLock _ThumbnailAccessLock = new AsyncLock();


        /// <summary>
        /// ニコニコ動画コンテンツの情報を取得します。
        /// 内部DB、サムネイル、Watchページのアクセス情報から更新されたデータを提供します。
        /// 
        /// </summary>
        /// <param name="rawVideoId"></param>
        /// <returns></returns>
        public async Task<Database.NicoVideo> GetNicoVideoInfo(string rawVideoId, bool requireLatest = false)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return null;
            }

            using (await _ThumbnailAccessLock.LockAsync())
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
                        return NiconicoSession.Context.Search.GetVideoInfoAsync(rawVideoId);
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

                        if (res.Video.ProviderType == "channel")
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                OwnerId = res.Video.CommunityId,
                                UserType = UserType.Channel
                            };
                        }
                        else
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                OwnerId = res.Video.UserId,
                                UserType = res.Video.ProviderType == "regular" ? UserType.User : UserType.Channel
                            };
                        }

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
                }
                finally
                {
                    NicoVideoDb.AddOrUpdate(info);
                    if (info.Owner != null)
                    {
                        NicoVideoOwnerDb.AddOrUpdate(info.Owner);
                    }
                }

                /*
                if (info.IsDeleted)
                {
                    await VideoCacheManager.VideoDeletedFromNiconicoServer(info.RawVideoId).ConfigureAwait(false);
                }
                */

                return info;
            }
        }

        public async Task<DmcWatchData> GetDmcWatchResponse(string rawVideoId)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return null;
            }

            await WaitNicoPageAccess();

            // TODO: 有害動画に指定されたページにアクセスした場合の対応
            // 有害動画ページにアクセスしたら一度だけ確認ページをダイアログ表示する
            // （ユーザーのアクションによらず）再度ページを読み込んで、もう一度HurmfulContentが返ってきた場合はnullを返す

            HarmfulContentReactionType harmfulContentReactionType = HarmfulContentReactionType.None;

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
                        info.DescriptionWithHtml = res.Video.Description;
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

                        if (res.Owner != null)
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                ScreenName = res.Owner.Nickname,
                                IconUrl = res.Owner.IconURL,
                                OwnerId = res.Owner.Id,
                                UserType = UserType.User
                            };

                            NicoVideoOwnerDb.AddOrUpdate(info.Owner);
                        }
                        else if (res.Channel != null)
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                ScreenName = res.Channel.Name,
                                IconUrl = res.Channel.IconURL,
                                OwnerId = res.Channel.GlobalId,
                                UserType = UserType.Channel
                            };
                        }

                        if (data.DmcWatchResponse?.Video != null)
                        {
                            info.IsDeleted = data.DmcWatchResponse.Video.IsDeleted;
                        }

                        NicoVideoDb.AddOrUpdate(info);
                    }

                    /*
                    if (data.DmcWatchResponse.Video.IsDeleted)
                    {
                        await VideoCacheManager.VideoDeletedFromNiconicoServer(rawVideoId).ConfigureAwait(false);
                    }
                    */

                    return data;
                }
                catch (AggregateException ea) when (ea.Flatten().InnerExceptions.Any(e => e is ContentZoningException))
                {
                    throw new NotImplementedException("not implement hurmful video content.");
                }
                catch (Mntone.Nico2.ContentZoningException)
                {
                    throw new NotImplementedException("not implement hurmful video content.");
                }



            }

        }

        public async Task<WatchApiResponse> GetWatchApiResponse(string rawVideoId, bool forceLowQuality = false)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return null;
            }

            if (!NiconicoSession.IsLoggedIn)
            {
                return null;
            }


            await WaitNicoPageAccess();


            // TODO: 有害動画に指定されたページにアクセスした場合の対応
            HarmfulContentReactionType harmfulContentReactionType = HarmfulContentReactionType.None;

            
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
                    info.DescriptionWithHtml = res.videoDetail.description;
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

                    /*
                    if (res.IsDeleted)
                    {
                        await VideoCacheManager.VideoDeletedFromNiconicoServer(rawVideoId).ConfigureAwait(false);
                    }
                    */

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

        public async Task<NicoVideoResponse> GetRelatedVideos(string videoId, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            if (Context == null)
            {
                return null;
            }

            return await Context.Video.GetRelatedVideoAsync(videoId, from, limit, sort, order);
        }


        public Task<RecommendResponse> GetRecommendFirstAsync()
        {
            return Context.Video.GetRecommendFirstAsync();
        }

        public Task<RecommendContent> GetRecommendAsync(RecommendResponse res, RecommendContent prevInfo = null)
        {
            var user_tags = res.UserTagParam;
            var seed = res.Seed;
            var page = prevInfo?.RecommendInfo.Page ?? res.Page;
            return Context.Video.GetRecommendAsync(user_tags, seed, page);
        }



    }
}

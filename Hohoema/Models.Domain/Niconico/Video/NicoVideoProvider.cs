using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.Recommend;
using Mntone.Nico2.Videos.WatchAPI;
using Hohoema.Database;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Infrastructure;
using Prism.Events;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Application;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public class VideoDeletedEvent : PubSubEvent<NicoVideo>
    { }



    public sealed class NicoVideoProvider : ProviderBase
    {
        
        public NicoVideoProvider(
            IEventAggregator eventAggregator,
            NiconicoSession niconicoSession,
            NicoVideoCacheRepository nicoVideoRepository,
            NicoVideoOwnerCacheRepository nicoVideoOwnerRepository
            )
            : base(niconicoSession)
        {
            EventAggregator = eventAggregator;
            _nicoVideoRepository = nicoVideoRepository;
            _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
        }


        NiconicoLiveToolkit.Video.VideoClient VideoClient => NiconicoSession.LiveContext.Video;


        static TimeSpan ThumbnailExpirationSpan { get; set; } = TimeSpan.FromMinutes(5);
        private IEventAggregator EventAggregator { get;  }

        AsyncLock _ThumbnailAccessLock = new AsyncLock();
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

        private void PublishVideoDeletedEvent(NicoVideo deletedVideo)
        {
            if (deletedVideo.IsDeleted)
            {
                EventAggregator.GetEvent<VideoDeletedEvent>().Publish(deletedVideo);
            }
        }



        /// <summary>
        /// ニコニコ動画コンテンツの情報を取得します。
        /// 内部DB、サムネイル、Watchページのアクセス情報から更新されたデータを提供します。
        /// 
        /// </summary>
        /// <param name="rawVideoId"></param>
        /// <returns></returns>
        public async Task<NicoVideo> GetNicoVideoInfo(string rawVideoId, bool requireLatest = false)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return null;
            }

            using (await _ThumbnailAccessLock.LockAsync())
            {
                var info = _nicoVideoRepository.Get(rawVideoId);

                // 最新情報が不要な場合は内部DBのキャッシュをそのまま返す
                if (info != null && !requireLatest)
                {
                    if (info.ViewCount != 0 
                        && info.LastUpdated > DateTime.Now - ThumbnailExpirationSpan)
                    {
                        return info;
                    }
                }

                if (info == null)
                {
                    info = new NicoVideo()
                    {
                        RawVideoId = rawVideoId
                    };
                }

                try
                {
                    var res = await ContextActionAsync(async context =>
                    {
                        return await VideoClient.GetVideoInfoAsync(rawVideoId);
                    });

                    if (res.IsOK)
                    {
                        var video = res.Video;

                        info.Title = video.Title;
                        info.VideoId = video.Id;
                        info.Length = TimeSpan.FromSeconds(video.LengthInSeconds);
                        info.PostedAt = video.FirstRetrieve.DateTime;
                        info.ThumbnailUrl = video.ThumbnailUrl.OriginalString;

                        info.ViewCount = (int)video.ViewCounter;
                        info.MylistCount = (int)video.MylistCounter;
                        info.CommentCount = (int)res.Thread.NumRes;
                        info.Tags = res.Tags.TagInfo.Select(x => new NicoVideoTag(x.Tag)).ToList();

                        if (res.Video.ProviderType == "channel")
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                OwnerId = res.Video.CommunityId,
                                UserType = NicoVideoUserType.Channel
                            };
                        }
                        else
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                OwnerId = res.Video.UserId.ToString(),
                                UserType = res.Video.ProviderType == "regular" ? NicoVideoUserType.User : NicoVideoUserType.Channel
                            };
                        }

                        info.IsDeleted = res.Video.Deleted != 0;
                        if (info.IsDeleted)
                        {
                            try
                            {
                                info.PrivateReasonType = (PrivateReasonType)res.Video.Deleted;
                            }
                            catch { }
                        }
                    }
                    else
                    { 
                        info.IsDeleted = true;
                    }

                    _nicoVideoRepository.UpdateItem(info);
                }
                catch (Exception ex) when (ex.Message.Contains("DELETE") || ex.Message.Contains("NOT_FOUND"))
                {
                    info.IsDeleted = true;
                    
                }
                finally
                {
                    _nicoVideoRepository.AddOrUpdate(info);
                }

                if (info.IsDeleted)
                {
                    PublishVideoDeletedEvent(info);
                }

                return info;
            }
        }

        public async Task<DmcWatchData> GetDmcWatchResponse(string rawVideoId)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return null;
            }

            // TODO: 有害動画に指定されたページにアクセスした場合の対応
            // 有害動画ページにアクセスしたら一度だけ確認ページをダイアログ表示する
            // （ユーザーのアクションによらず）再度ページを読み込んで、もう一度HurmfulContentが返ってきた場合はnullを返す

            HarmfulContentReactionType harmfulContentReactionType = HarmfulContentReactionType.None;

            {
                try
                {
                    var data = await Helpers.ConnectionRetryUtil.TaskWithRetry(async () =>
                    {
                        return await ContextActionWithPageAccessWaitAsync(async context =>
                        {
                            return await context.Video.GetDmcWatchResponseAsync(
                            rawVideoId
                            , harmfulReactType: harmfulContentReactionType
                            );
                        });
                        
                    });

                    var res = data?.DmcWatchResponse;

                    var info = _nicoVideoRepository.Get(rawVideoId);
                    if (res != null)
                    {
                        if (info == null)
                        {
                            info = new NicoVideo()
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

                        if (res.Video.DmcInfo?.Quality.Audios != null)
                        {
                            info.LoudnessCollectionValue = res.Video.DmcInfo.Quality.Audios[0].VideoLoudnessCorrectionValue;
                        }
                        else if (res.Video.SmileInfo != null)
                        {
                            info.LoudnessCollectionValue = res.Video.SmileInfo.VideoLoudnessCorrectionValue;
                        }


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

                        info.Tags = res.Tags.Select(x => new NicoVideoTag(x.Id)
                        {
                            Tag = x.Name,
                            IsCategoryTag = x.IsCategory,
                            IsLocked = x.IsLocked,
                            IsDictionaryExists = x.IsDictionaryExists
                        }).ToList();

                        if (res.Owner != null)
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                ScreenName = res.Owner.Nickname,
                                IconUrl = res.Owner.IconURL,
                                OwnerId = res.Owner.Id,
                                UserType = NicoVideoUserType.User
                            };

                            _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                        }
                        else if (res.Channel != null)
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                ScreenName = res.Channel.Name,
                                IconUrl = res.Channel.IconURL,
                                OwnerId = res.Channel.GlobalId,
                                UserType = NicoVideoUserType.Channel
                            };
                        }

                        if (data.DmcWatchResponse?.Video != null)
                        {
                            info.IsDeleted = data.DmcWatchResponse.Video.IsDeleted;
                        }

                        _nicoVideoRepository.AddOrUpdate(info);
                    }

                    
                    if (info.IsDeleted)
                    {
                        PublishVideoDeletedEvent(info);
                    }
                    

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

            // TODO: 有害動画に指定されたページにアクセスした場合の対応
            HarmfulContentReactionType harmfulContentReactionType = HarmfulContentReactionType.None;

            
            {
                try
                {
                    var res = await Helpers.ConnectionRetryUtil.TaskWithRetry(async () =>
                    {
                        return await ContextActionWithPageAccessWaitAsync(async context =>
                        {
                            return await context.Video.GetWatchApiAsync(
                            rawVideoId
                            , forceLowQuality: forceLowQuality
                            , harmfulReactType: harmfulContentReactionType
                            );
                        });
                    });

                    var info = _nicoVideoRepository.Get(rawVideoId);
                    if (info == null)
                    {
                        info = new NicoVideo()
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
                    info.Tags = res.videoDetail.tagList.Select(x => new NicoVideoTag(x.tag)
                    {
                        IsCategoryTag = x.cat ?? false,
                        IsLocked = x.lck == "1",  /* TODO: lck 値が不明です */
                        IsDictionaryExists = x.dic ?? false
                    }).ToList();

                    info.Owner = new NicoVideoOwner()
                    {
                        ScreenName = res.UploaderInfo?.nickname ?? res.channelInfo?.name,
                        IconUrl = res.UploaderInfo?.icon_url ?? res.channelInfo?.icon_url,
                        OwnerId = res.UploaderInfo?.id ?? res.channelInfo?.id,
                        UserType = res.channelInfo != null ? NicoVideoUserType.Channel : NicoVideoUserType.User
                    };

                    info.IsDeleted = res.IsDeleted;
                    info.PrivateReasonType = res.PrivateReason;

                    _nicoVideoRepository.AddOrUpdate(info);
                    //                    NicoVideoOwnerDb.AddOrUpdate(info.Owner);

                    if (info.IsDeleted)
                    {
                        PublishVideoDeletedEvent(info);
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

        public async Task<NicoVideoResponse> GetRelatedVideos(string videoId, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Video.GetRelatedVideoAsync(videoId, from, limit, sort, order);
            });
        }


        public async Task<RecommendResponse> GetRecommendFirstAsync()
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Video.GetRecommendFirstAsync();
            });
        }

        public async Task<RecommendContent> GetRecommendAsync(RecommendResponse res, RecommendContent prevInfo = null)
        {
            var user_tags = res.UserTagParam;
            var seed = res.Seed;
            var page = prevInfo?.RecommendInfo.Page ?? res.Page;
            return await ContextActionAsync(async context =>
            {
                return await context.Video.GetRecommendAsync(user_tags, seed, page);
            });
            
        }



    }
}

using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Videos.Dmc;
using Mntone.Nico2.Videos.Recommend;
using Mntone.Nico2.Videos.WatchAPI;
using Hohoema.Database;
using Hohoema.Models.Domain.Helpers;
using Hohoema.Models.Infrastructure;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Application;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public class VideoDeletedEvent : ValueChangedMessage<NicoVideo>
    {
        public VideoDeletedEvent(NicoVideo value) : base(value)
        {
        }
    }



    public sealed class NicoVideoProvider : ProviderBase
    {
        
        public NicoVideoProvider(
            NiconicoSession niconicoSession,
            NicoVideoCacheRepository nicoVideoRepository,
            NicoVideoOwnerCacheRepository nicoVideoOwnerRepository
            )
            : base(niconicoSession)
        {
            _nicoVideoRepository = nicoVideoRepository;
            _nicoVideoOwnerRepository = nicoVideoOwnerRepository;
        }


        NiconicoLiveToolkit.Video.VideoClient VideoClient => NiconicoSession.LiveContext.Video;


        static TimeSpan ThumbnailExpirationSpan { get; set; } = TimeSpan.FromMinutes(5);
        
        AsyncLock _ThumbnailAccessLock = new AsyncLock();
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

        private void PublishVideoDeletedEvent(NicoVideo deletedVideo)
        {
            if (deletedVideo.IsDeleted)
            {
                StrongReferenceMessenger.Default.Send(new VideoDeletedEvent(deletedVideo));
            }
        }



        /// <summary>
        /// ニコニコ動画コンテンツの情報を取得します。
        /// 内部DB、サムネイル、Watchページのアクセス情報から更新されたデータを提供します。
        /// 
        /// </summary>
        /// <param name="rawVideoId"></param>
        /// <returns></returns>
        public async ValueTask<NicoVideo> GetNicoVideoInfo(string rawVideoId, bool requireLatest = false)
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
                        info.Description = video.Description;
                        info.ViewCount = (int)video.ViewCounter;
                        info.MylistCount = (int)video.MylistCounter;
                        info.CommentCount = (int)res.Thread.NumRes;
                        info.Permission = res.Video.VideoPermission;
#if DEBUG
                        if (info.Permission is 
                            NiconicoLiveToolkit.Video.VideoPermission.Unknown or
                            NiconicoLiveToolkit.Video.VideoPermission.FreeForChannelMember or
                            NiconicoLiveToolkit.Video.VideoPermission.VideoPermission_3
                            )
                        {
                            if (Debugger.IsAttached)
                            {
                                Debugger.Break();
                            }
                        }
#endif
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


        public async IAsyncEnumerable<NicoVideo> GetVideoInfoManyAsync(IEnumerable<string> idItems, bool isLatestRequired = true)
        {
            using (await _ThumbnailAccessLock.LockAsync())
            {
                if (isLatestRequired is false)
                {
                    if (idItems.All(x => _nicoVideoRepository.Exists(y => y.VideoId == x)))
                    {
                        foreach (var id in idItems)
                        {
                            yield return _nicoVideoRepository.Get(id);
                        }

                        yield break;
                    }
                }

                var res = await Task.Run(async () => await VideoClient.GetVideoInfoManyAsync(idItems));

                if (res.IsOK && res.Count == 0)
                {
                    yield break;
                }

                foreach (var data in idItems)
                {
                    var item = res.Videos.FirstOrDefault(x => x.Video.Id == data) ?? await VideoClient.GetVideoInfoAsync(data);

                    var info = _nicoVideoRepository.Get(item.Video.Id);
                    var video = item.Video;

                    info.Title = video.Title;
                    info.VideoId = video.Id;
                    info.Length = TimeSpan.FromSeconds(video.LengthInSeconds);
                    info.PostedAt = video.FirstRetrieve.DateTime;
                    info.ThumbnailUrl = video.ThumbnailUrl.OriginalString;
                    info.Description = video.Description;
                    info.ViewCount = (int)video.ViewCounter;
                    info.MylistCount = (int)video.MylistCounter;
                    info.CommentCount = (int)item.Thread.NumRes;
                    info.Permission = video.VideoPermission;
#if DEBUG
                    if (info.Permission is
                        NiconicoLiveToolkit.Video.VideoPermission.Unknown or
                        NiconicoLiveToolkit.Video.VideoPermission.FreeForChannelMember or
                        NiconicoLiveToolkit.Video.VideoPermission.VideoPermission_3
                        )
                    {
                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }
                    }
#endif
                    info.Tags = item.Tags?.TagInfo.Select(x => new NicoVideoTag(x.Tag)).ToList() ?? info.Tags;

                    if (item.Video.ProviderType == "channel")
                    {
                        info.Owner = new NicoVideoOwner()
                        {
                            OwnerId = item.Video.CommunityId,
                            UserType = NicoVideoUserType.Channel
                        };
                    }
                    else
                    {
                        info.Owner = new NicoVideoOwner()
                        {
                            OwnerId = item.Video.UserId.ToString(),
                            UserType = item.Video.ProviderType == "regular" ? NicoVideoUserType.User : NicoVideoUserType.Channel
                        };
                    }

                    info.IsDeleted = item.Video.Deleted != 0;
                    if (info.IsDeleted)
                    {
                        try
                        {
                            info.PrivateReasonType = (PrivateReasonType)item.Video.Deleted;
                        }
                        catch { }
                    }

                    _nicoVideoRepository.UpdateItem(info);

                    yield return info;
                }
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
                        info.PostedAt = res.Video.RegisteredAt.DateTime;
                        info.ThumbnailUrl = res.Video.Thumbnail.Url.OriginalString;
                        info.DescriptionWithHtml = res.Video.Description;
                        info.ViewCount = res.Video.Count.View;
                        info.MylistCount = res.Video.Count.Mylist;
                        info.CommentCount = res.Video.Count.Comment;

                        if (res.Media?.Delivery?.Movie.Audios is not null and var audios)
                        {
                            info.LoudnessCollectionValue = audios[0].Metadata.VideoLoudnessCollection;
                        }

                        info.MovieType = MovieType.Mp4;

                        info.Tags = res.Tag.Items.Select(x => new NicoVideoTag(x.Name)
                        {
                            Tag = x.Name,
                            IsCategoryTag = x.IsCategory,
                            IsLocked = x.IsLocked,
                            IsDictionaryExists = x.IsNicodicArticleExists
                        }).ToList();

                        if (res.Owner != null)
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                ScreenName = res.Owner.Nickname,
                                IconUrl = res.Owner.IconUrl.OriginalString,
                                OwnerId = res.Owner.Id.ToString(),
                                UserType = NicoVideoUserType.User
                            };

                            _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                        }
                        else if (res.Channel != null)
                        {
                            info.Owner = new NicoVideoOwner()
                            {
                                ScreenName = res.Channel.Name,
                                IconUrl = res.Channel.Thumbnail.Url.OriginalString,
                                OwnerId = res.Channel.Id,
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

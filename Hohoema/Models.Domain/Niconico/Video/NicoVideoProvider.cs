using Mntone.Nico2;
using Mntone.Nico2.Mylist;
using Mntone.Nico2.Videos.Recommend;
using Hohoema.Database;
using Hohoema.Models.Helpers;
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
using Uno.Threading;
using System.Threading;
using System.Runtime.CompilerServices;
using NiconicoToolkit.Video.Watch;

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


        NiconicoToolkit.Video.VideoClient VideoClient => NiconicoSession.ToolkitContext.Video;


        static TimeSpan ThumbnailExpirationSpan { get; set; } = TimeSpan.FromMinutes(5);
        
        FastAsyncLock _ThumbnailAccessLock = new FastAsyncLock();
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
        public async ValueTask<NicoVideo> GetNicoVideoInfo(string rawVideoId, bool requireLatest = false, CancellationToken ct = default)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return _nicoVideoRepository.Get(rawVideoId);
            }

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

            if (!Helpers.InternetConnection.IsInternet())
            {
                return info;
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
                    using (await _ThumbnailAccessLock.LockAsync(ct))
                    {
                        return await VideoClient.GetVideoInfoAsync(rawVideoId);
                    }
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
//                    info.Permission = res.Video.VideoPermission;
/*
#if DEBUG
                    if (info.Permission is 
                        NiconicoLiveToolkit.Video.VideoPermission.Unknown or
                        NiconicoLiveToolkit.Video.VideoPermission.VideoPermission_3
                        )
                    {
                        if (Debugger.IsAttached)
                        {
                            Debugger.Break();
                        }
                    }
#endif
*/
                    info.Tags = res.Tags.TagInfo.Select(x => new NicoVideoTag(x.Tag)).ToList();

                    if (res.Video.ProviderType == NiconicoToolkit.Video.VideoProviderType.Channel)
                    {
                        info.Owner = new NicoVideoOwner()
                        {
                            OwnerId = res.Video.CommunityId,
                            UserType = NicoVideoUserType.Channel,
                            IconUrl = info.Owner?.IconUrl,
                            ScreenName = info.Owner?.ScreenName,
                        };

                        _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                    }
                    else
                    {
                        info.Owner = new NicoVideoOwner()
                        {
                            OwnerId = res.Video.UserId.ToString(),
                            UserType = res.Video.ProviderType == NiconicoToolkit.Video.VideoProviderType.Regular ? NicoVideoUserType.User : NicoVideoUserType.Channel,
                            IconUrl = info.Owner?.IconUrl,
                            ScreenName = info.Owner?.ScreenName,
                        };

                        _nicoVideoOwnerRepository.UpdateItem(info.Owner);
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
            }
            catch (Exception ex) when (ex.Message.Contains("DELETE") || ex.Message.Contains("NOT_FOUND"))
            {
                info.IsDeleted = true;
                    
            }
            finally
            {
                info.LastUpdated = DateTime.Now;
                _nicoVideoRepository.AddOrUpdate(info);
            }

            if (info.IsDeleted)
            {
                PublishVideoDeletedEvent(info);
            }

            return info;
            
        }

        public async IAsyncEnumerable<NicoVideo> GetVideoInfoManyAsync(IEnumerable<string> idItems, bool isLatestRequired = true, [EnumeratorCancellation] CancellationToken ct = default)
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

            var res = await Task.Run(async () =>
            {
                using (await _ThumbnailAccessLock.LockAsync(ct))
                {
                    return await VideoClient.GetVideoInfoManyAsync(idItems);
                }
            });

            if (res.IsOK && !res.Videos.Any())
            {
                yield break;
            }

            foreach (var data in idItems)
            {
                var item = res.Videos.FirstOrDefault(x => x.Video.Id == data);
                var video = item?.Video;

                if (video is null && isLatestRequired)
                {
                    var singleRes = await VideoClient.GetVideoInfoAsync(data);
                    video = singleRes?.Video;
                }

                var info = _nicoVideoRepository.Get(item.Video.Id);

                if (video is null)
                {
                    yield return info;
                    continue;
                }


                info.Title = video.Title;
                info.VideoId = video.Id;
                info.Length = TimeSpan.FromSeconds(video.LengthInSeconds);
                info.PostedAt = video.FirstRetrieve.DateTime;
                info.ThumbnailUrl = video.ThumbnailUrl.OriginalString;
                info.Description = video.Description;
                info.ViewCount = (int)video.ViewCounter;
                info.MylistCount = (int)video.MylistCounter;
                info.CommentCount = (int)item.Thread.NumRes;
#if DEBUG
                if (info.Permission is
                    NiconicoToolkit.Video.VideoPermission.Unknown or
                    NiconicoToolkit.Video.VideoPermission.VideoPermission_3
                    )
                {
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }
                }
#endif

                if (item.Video.ProviderType == NiconicoToolkit.Video.VideoProviderType.Channel)
                {
                    info.Owner = new NicoVideoOwner()
                    {
                        OwnerId = item.Video.CommunityId,
                        UserType = NicoVideoUserType.Channel,
                        IconUrl = info.Owner?.IconUrl,
                        ScreenName = info.Owner?.ScreenName,
                    };

                    _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                }
                else
                {
                    info.Owner = new NicoVideoOwner()
                    {
                        OwnerId = item.Video.UserId.ToString(),
                        UserType = item.Video.ProviderType ==  NiconicoToolkit.Video.VideoProviderType.Regular ? NicoVideoUserType.User : NicoVideoUserType.Channel,
                        IconUrl = info.Owner?.IconUrl,
                        ScreenName = info.Owner?.ScreenName,
                    };

                    _nicoVideoOwnerRepository.UpdateItem(info.Owner);
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

                info.LastUpdated = DateTime.Now;
                _nicoVideoRepository.UpdateItem(info);

                yield return info;
            }

        }


        public async Task<WatchPageResponse> GetDmcWatchResponse(string rawVideoId, bool noHisotry = false)
        {
            if (NiconicoSession.ServiceStatus.IsOutOfService())
            {
                return null;
            }

            var data = await NiconicoSession.ToolkitContext.Video.VideoWatch.GetInitialWatchDataAsync(rawVideoId, !noHisotry, !noHisotry);

            var res = data?.WatchApiResponse.WatchApiData;

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
                    info.LoudnessCollectionValue = audios[0].Metadata.LoudnessCollection[0].Value;
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

                    _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                }

                if (res?.Video != null)
                {
                    info.IsDeleted = res.Video.IsDeleted;
                }

                info.LastUpdated = DateTime.Now;
                _nicoVideoRepository.AddOrUpdate(info);
            }


            if (info.IsDeleted)
            {
                PublishVideoDeletedEvent(info);
            }


            return data;
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

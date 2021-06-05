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
using NiconicoToolkit.Video;
using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NiconicoToolkit.SearchWithCeApi.Video;

namespace Hohoema.Models.Domain.Niconico.Video
{
    public class VideoDeleteMessageData
    {
        public VideoDeleteMessageData(string videoId, long? deleteReason, string title)
        {
            VideoId = videoId;
            DeleteReason = deleteReason;
            Title = title;
        }

        public string VideoId { get; set; }
        public long? DeleteReason { get; }
        public string Title { get; }
    }


    public class VideoDeletedMessage : ValueChangedMessage<VideoDeleteMessageData>
    {
        public VideoDeletedMessage(VideoDeleteMessageData value) : base(value)
        {
        }
    }



    sealed class NicoVideoCacheRepository : LiteDBServiceBase<NicoVideo>
    {
        internal NicoVideoCacheRepository(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public NicoVideo Get(string videoId)
        {
            return _collection
                .Include(x => x.Owner)
                .FindById(videoId)
                ?? new NicoVideo() { RawVideoId = videoId };
        }

        public string GetVideoId(string rawVideoId)
        {
            return _collection.FindById(rawVideoId)
                ?.VideoId;
        }

        public List<NicoVideo> Get(IEnumerable<string> videoIds)
        {
            return videoIds.Select(id => _collection.FindById(id) ?? new NicoVideo() { RawVideoId = id }).ToList();
        }

        public bool AddOrUpdate(NicoVideo video)
        {
            video.LastUpdated = DateTime.Now;
            return _collection.Upsert(video);
        }

        public List<NicoVideo> SearchFromTitle(string keyword)
        {
            return _collection.Find(x => x.Title.Contains(keyword))
                    .ToList();
        }


        public int Delete(Expression<Func<NicoVideo, bool>> expression)
        {
            return _collection.DeleteMany(expression);
        }
    }

    public sealed class NicoVideoOwnerCacheRepository : LiteDBServiceBase<NicoVideoOwner>
    {
        public NicoVideoOwnerCacheRepository(LiteDB.LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public NicoVideoOwner Get(string ownerId)
        {
            return _collection.FindById(ownerId);
        }

        public List<NicoVideoOwner> SearchFromTitle(string name)
        {
            return _collection.Find(x => x.ScreenName.Contains(name)).ToList();
        }
    }
    

    public sealed class NicoVideoProvider : ProviderBase
    {
        public NicoVideoProvider(
            NiconicoSession niconicoSession,
            LiteDB.LiteDatabase liteDatabase
            )
            : base(niconicoSession)
        {
            _nicoVideoRepository = new NicoVideoCacheRepository(liteDatabase);
            _nicoVideoOwnerRepository = new NicoVideoOwnerCacheRepository(liteDatabase);
            _cache = new MemoryCache(new MemoryCacheOptions());
            _entryOptions = new MemoryCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(5),
                Size = 500
            };
        }

        MemoryCache _cache;
        MemoryCacheEntryOptions _entryOptions;
        NiconicoToolkit.SearchWithCeApi.Video.VideoSearchSubClient SearchClient => _niconicoSession.ToolkitContext.SearchWithCeApi.Video;


        static TimeSpan ThumbnailExpirationSpan { get; set; } = TimeSpan.FromMinutes(5);
        
        FastAsyncLock _ThumbnailAccessLock = new FastAsyncLock();
        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

        private void PublishVideoDeletedEvent(string videoId, long? deleteReason, string title)
        {
            StrongReferenceMessenger.Default.Send(new VideoDeletedMessage(new VideoDeleteMessageData(videoId, deleteReason, title)));
        }

        public NicoVideo UpdateCache(string videoId, Func<NicoVideo, (bool IsDeleted, long? deleteReason)> updateDelegate)
        {
            var video = GetCachedVideoInfo(videoId);
            if (video == null)
            {
                video = new NicoVideo() { RawVideoId = videoId };
                _nicoVideoRepository.CreateItem(video);
                
                Debug.WriteLine("set video to memory cache" + videoId);
                _cache.Set(videoId, video, _entryOptions);
            }

            return UpdateCache(video, updateDelegate);
        }

        public NicoVideo UpdateCache(NicoVideo video, Func<NicoVideo, (bool IsDeleted, long? deleteReason)> updateDelegate)
        {
            if (video == null)
            {
                throw new ArgumentNullException(nameof(video));
            }

            var result = updateDelegate(video);
            if (result.IsDeleted)
            {
                PublishVideoDeletedEvent(video.RawVideoId, result.deleteReason, video.Title);
            }


            video.LastUpdated = DateTime.Now;
            _nicoVideoRepository.UpdateItem(video);

            return video;
        }



        public NicoVideo GetCachedVideoInfo(string videoId)
        {
            if (_cache.TryGetValue<NicoVideo>(videoId, out var video)) 
            {
                Debug.WriteLine("get video from local: " + videoId);
                return video; 
            }
            
            video = _nicoVideoRepository.Get(videoId);
            if (video != null)
            {
                Debug.WriteLine("set video to memory cache: " + videoId);
                _cache.Set(videoId, video, _entryOptions);
            }

            return video;
        }

        public List<NicoVideo> GetCachedVideoInfoItems(IEnumerable<string> videoIds)
        {
            return videoIds.Select(x => GetCachedVideoInfo(x)).ToList();
        }


        public async ValueTask<List<NicoVideo>> GetCachedVideoInfoItemsAsync(IEnumerable<string> videoIds, CancellationToken ct = default)
        {
            var cachedVideos = GetCachedVideoInfoItems(videoIds);
            var ids = cachedVideos.Where(x => x.Owner is null).Select(x => x.RawVideoId);
            if (ids.Any())
            {
                await GetVideoInfoManyAsync(ids).ToArrayAsync(ct);
            }

            return cachedVideos;
        }


        public async ValueTask<NicoVideo> GetCachedVideoInfoAsync(string videoId, CancellationToken ct = default)
        {
            var video = GetCachedVideoInfo(videoId);
            if (video == null)
            {
                (_, video) = await GetVideoInfoAsync(videoId, ct);
            }

            return video;
        }

        public async ValueTask<string> ResolveVideoTitleAsync(string videoId, CancellationToken ct = default)
        {
            var video = await GetCachedVideoInfoAsync(videoId, ct);
            return video.Title;
        }

        public async ValueTask<string> ResolveThumbnailUrlAsync(string videoId, CancellationToken ct = default)
        {
            var video = await GetCachedVideoInfoAsync(videoId, ct);
            return video.ThumbnailUrl;
        }


        public async ValueTask<IDictionary<string, NicoVideoOwner>> ResolveVideoOwnersAsync(IEnumerable<string> videoIds, CancellationToken ct = default)
        {
            var cachedVideos = await GetCachedVideoInfoItemsAsync(videoIds, ct);
            return cachedVideos.ToDictionary(x => x.RawVideoId, x => x.Owner);
        }

        public async ValueTask<NicoVideoOwner> ResolveVideoOwnerAsync(string videoId, CancellationToken ct = default)
        {
            var video = GetCachedVideoInfo(videoId);
            if (video?.Owner == null)
            {
                (_, video) = await GetVideoInfoAsync(videoId, ct);
            }

            return video?.Owner;
        }



        /// <summary>
        /// ニコニコ動画コンテンツの情報を取得します。
        /// 内部DB、サムネイル、Watchページのアクセス情報から更新されたデータを提供します。
        /// 
        /// </summary>
        /// <param name="rawVideoId"></param>
        /// <returns></returns>
        public async Task<(VideoIdSearchSingleResponse Response, NicoVideo NicoVideo)> GetVideoInfoAsync(string rawVideoId, CancellationToken ct = default)
        {
            if (_niconicoSession.ServiceStatus.IsOutOfService())
            {
                throw new InvalidOperationException();
            }

            if (!Helpers.InternetConnection.IsInternet())
            {
                throw new InvalidOperationException();
            }

            Debug.WriteLine("get video from online " + rawVideoId);

            try
            {
                var res = await SearchClient.IdSearchAsync(rawVideoId);

                if (!res.IsOK)
                {
                    throw new ArgumentException(rawVideoId);
                }

                var nicoVideo = UpdateVideo(rawVideoId, res.Video);
                var video = res.Video;
                
                if (res.Video.Deleted != 0)
                {
                    PublishVideoDeletedEvent(rawVideoId, res.Video.Deleted, res.Video.Title);
                }

                return (res, nicoVideo);
            }
            catch (Exception ex) when (ex.Message.Contains("DELETE") || ex.Message.Contains("NOT_FOUND"))
            {
                PublishVideoDeletedEvent(rawVideoId, null, null);
                throw;
            }            
        }

        private NicoVideo UpdateVideo(string videoId, VideoItem video)
        {
            return UpdateCache(videoId, info =>
            {
                info.Title = video.Title;
                info.VideoId = video.Id;
                info.Length = TimeSpan.FromSeconds(video.LengthInSeconds);
                info.PostedAt = video.FirstRetrieve.DateTime;
                info.ThumbnailUrl = video.ThumbnailUrl.OriginalString;
                info.Description = video.Description;

                if (video.ProviderType == VideoProviderType.Channel)
                {
                    info.Owner = new NicoVideoOwner()
                    {
                        OwnerId = video.CommunityId,
                        UserType = OwnerType.Channel,
                        IconUrl = info.Owner?.IconUrl,
                        ScreenName = info.Owner?.ScreenName,
                    };

                    _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                }
                else
                {
                    info.Owner = new NicoVideoOwner()
                    {
                        OwnerId = video.UserId.ToString(),
                        UserType = video.ProviderType == VideoProviderType.Regular ? OwnerType.User : OwnerType.Channel,
                        IconUrl = info.Owner?.IconUrl,
                        ScreenName = info.Owner?.ScreenName,
                    };

                    _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                }

                return (video.Deleted != 0, video.Deleted);
            });
        }

        public async IAsyncEnumerable<VideoItem> GetVideoInfoManyAsync(IEnumerable<string> idItems, bool isLatestRequired = true, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var res = await Task.Run(async () =>
            {
                using (await _ThumbnailAccessLock.LockAsync(ct))
                {
                    return await SearchClient.IdSearchAsync(idItems);
                }
            });

            if (res.IsOK && !res.Videos.Any())
            {
                yield break;
            }

            foreach (var videoId in idItems)
            {
                var item = res.Videos.FirstOrDefault(x => x.Video.Id == videoId);
                var video = item?.Video;

                if (video is null && isLatestRequired)
                {
                    var singleRes = await SearchClient.IdSearchAsync(videoId);
                    video = singleRes?.Video;
                }

                if (video is null)
                {
                    Debug.WriteLine("動画情報の取得に失敗 VideoId: " + videoId);
                    continue;
                }

                UpdateVideo(videoId, video);

                yield return video;
            }

        }


        public async Task<WatchPageResponse> GetWatchPageResponseAsync(string rawVideoId, bool noHisotry = false)
        {
            if (_niconicoSession.ServiceStatus.IsOutOfService())
            {
                return null;
            }

            var data = await _niconicoSession.ToolkitContext.Video.VideoWatch.GetInitialWatchDataAsync(rawVideoId, !noHisotry, !noHisotry);
            if (data.WatchApiResponse?.WatchApiData is not null and var watchData)
            {
                UpdateCache(rawVideoId, info =>
                {
                    var video = watchData.Video;
                    info.VideoId = rawVideoId;
                    info.Title = video.Title;
                    info.Length = TimeSpan.FromSeconds(video.Duration);
                    info.PostedAt = video.RegisteredAt.DateTime;
                    info.ThumbnailUrl = video.Thumbnail.Url.OriginalString;
                    info.Description = video.Description;

                    if (watchData.Owner is not null and var userOwner)
                    {
                        info.Owner = new NicoVideoOwner()
                        {
                            ScreenName = userOwner.Nickname,
                            IconUrl = userOwner.IconUrl.OriginalString,
                            OwnerId = userOwner.Id.ToString(),
                            UserType = OwnerType.User
                        };

                        _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                    }
                    else if (watchData.Channel is not null and var channelOwner)
                    {
                        info.Owner = new NicoVideoOwner()
                        {
                            ScreenName = channelOwner.Name,
                            IconUrl = channelOwner.Thumbnail.Url.OriginalString,
                            OwnerId = channelOwner.Id,
                            UserType = OwnerType.Channel
                        };

                        _nicoVideoOwnerRepository.UpdateItem(info.Owner);
                    }

                    return (watchData.Video.IsDeleted, default);
                });
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

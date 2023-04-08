using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Hohoema.Infra;
using Hohoema.Models.Application;
using LiteDB;
using Microsoft.Extensions.Caching.Memory;
using NiconicoToolkit.SearchWithCeApi.Video;
using NiconicoToolkit.Video;
using NiconicoToolkit.Video.Watch;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Video;

public class VideoDeleteMessageData
{
    public VideoDeleteMessageData(VideoId videoId, long? deleteReason, string title)
    {
        VideoId = videoId;
        DeleteReason = deleteReason;
        Title = title;
    }

    public VideoId VideoId { get; set; }
    public long? DeleteReason { get; }
    public string Title { get; }
}


public class VideoDeletedMessage : ValueChangedMessage<VideoDeleteMessageData>
{
    public VideoDeletedMessage(VideoDeleteMessageData value) : base(value)
    {
    }
}



public sealed class NicoVideoCacheRepository : LiteDBServiceBase<NicoVideo>
{
    public NicoVideoCacheRepository(LiteDatabase liteDatabase) : base(liteDatabase)
    {

    }

    public NicoVideo Get(VideoId videoId)
    {
        return _collection
            .Include(x => x.Owner)
            .FindById(videoId.ToString())
            ?? new NicoVideo() { Id = videoId };
    }

    public string GetVideoId(string rawVideoId)
    {
        return _collection.FindById(rawVideoId)
            ?.VideoAliasId;
    }

    public List<NicoVideo> Get(IEnumerable<VideoId> videoIds)
    {
        return videoIds.Select(id => _collection.FindById(id.ToString()) ?? new NicoVideo() { Id = id }).ToList();
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
        LiteDB.LiteDatabase liteDatabase,
        NicoVideoCacheRepository nicoVideoCacheRepository
        )
        : base(niconicoSession)
    {
        _nicoVideoRepository = nicoVideoCacheRepository;
        _nicoVideoOwnerRepository = new NicoVideoOwnerCacheRepository(liteDatabase);
        _cache ??= new MemoryCache(new MemoryCacheOptions());
        _entryOptions = new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 500
        };
    }

    private static MemoryCache _cache;
    private readonly MemoryCacheEntryOptions _entryOptions;

    private NiconicoToolkit.SearchWithCeApi.Video.VideoSearchSubClient SearchClient => _niconicoSession.ToolkitContext.SearchWithCeApi.Video;

    private static TimeSpan ThumbnailExpirationSpan { get; set; } = TimeSpan.FromMinutes(5);

    private readonly Helpers.AsyncLock _ThumbnailAccessLock = new();
    private readonly NicoVideoCacheRepository _nicoVideoRepository;
    private readonly NicoVideoOwnerCacheRepository _nicoVideoOwnerRepository;

    private void PublishVideoDeletedEvent(VideoId videoId, long? deleteReason, string title)
    {
        _ = StrongReferenceMessenger.Default.Send(new VideoDeletedMessage(new VideoDeleteMessageData(videoId, deleteReason, title)));
    }

    public NicoVideo UpdateCache(VideoId videoId, NvapiVideoItem nvapiVideoItem, bool isDeleted = false, long deleted = 0)
    {
        NicoVideo video = GetCachedVideoInfo(videoId);
        if (video == null)
        {
            video = new NicoVideo() { Id = videoId };
            _ = _nicoVideoRepository.CreateItem(video);

            Debug.WriteLine("set video to memory cache" + videoId);
            _ = _cache.Set(videoId, video, _entryOptions);
        }

        return UpdateCache(video, video =>
        {
            video.VideoAliasId = nvapiVideoItem.Id;
            video.Title = nvapiVideoItem.Title;
            video.ThumbnailUrl = nvapiVideoItem.Thumbnail.ListingUrl.OriginalString;
            video.PostedAt = nvapiVideoItem.RegisteredAt.DateTime;
            video.Length = TimeSpan.FromSeconds(nvapiVideoItem.Duration);
            video.Description ??= nvapiVideoItem.ShortDescription;
            if (nvapiVideoItem.Owner is not null and var owner)
            {
                video.Owner ??= new NicoVideoOwner()
                {
                    OwnerId = owner.Id,
                    UserType = owner.OwnerType,
                };

                video.Owner.ScreenName = owner.Name;
                video.Owner.IconUrl = owner.IconUrl?.OriginalString;
            }

            return (false, default);
        });
    }

    public NicoVideo UpdateCache(VideoId videoId, Func<NicoVideo, (bool IsDeleted, long? deleteReason)> updateDelegate)
    {
        NicoVideo video = GetCachedVideoInfo(videoId);
        if (video == null)
        {
            video = new NicoVideo() { Id = videoId };
            _ = _nicoVideoRepository.CreateItem(video);

            Debug.WriteLine("set video to memory cache" + videoId);
            _ = _cache.Set(videoId, video, _entryOptions);
        }

        return UpdateCache(video, updateDelegate);
    }

    public NicoVideo UpdateCache(NicoVideo video, Func<NicoVideo, (bool IsDeleted, long? deleteReason)> updateDelegate)
    {
        if (video == null)
        {
            throw new ArgumentNullException(nameof(video));
        }

        (bool IsDeleted, long? deleteReason) = updateDelegate(video);
        if (IsDeleted)
        {
            PublishVideoDeletedEvent(video.VideoId, deleteReason, video.Title);
        }

        if (video.Owner?.UserType == OwnerType.Hidden)
        {
            video.Owner = null;
        }

        video.LastUpdated = DateTime.Now;
        _ = _nicoVideoRepository.UpdateItem(video);

        if (video.Owner != null)
        {
            _ = _nicoVideoOwnerRepository.UpdateItem(video.Owner);
        }

        return video;
    }



    public NicoVideo GetCachedVideoInfo(VideoId videoId)
    {
        if (_cache.TryGetValue<NicoVideo>(videoId, out NicoVideo video))
        {
            Debug.WriteLine("get video from local: " + videoId);
            return video;
        }

        video = _nicoVideoRepository.Get(videoId);
        if (video != null)
        {
            Debug.WriteLine("set video to memory cache: " + videoId);
            _ = _cache.Set(videoId, video, _entryOptions);
        }

        return video;
    }

    public List<NicoVideo> GetCachedVideoInfoItems(IEnumerable<VideoId> videoIds)
    {
        return videoIds.Select(GetCachedVideoInfo).ToList();
    }


    public async ValueTask<List<NicoVideo>> GetCachedVideoInfoItemsAsync(IEnumerable<VideoId> videoIds, CancellationToken ct = default)
    {
        List<NicoVideo> cachedVideos = GetCachedVideoInfoItems(videoIds);
        IEnumerable<VideoId> ids = cachedVideos.Where(x => x.Owner is null).Select(x => x.VideoId);
        if (ids.Any())
        {
            _ = await GetVideoInfoManyAsync(ids).ToArrayAsync(ct);
        }

        return cachedVideos;
    }


    public async ValueTask<NicoVideo> GetCachedVideoInfoAsync(VideoId videoId, CancellationToken ct = default)
    {
        NicoVideo video = GetCachedVideoInfo(videoId);
        if (video == null || string.IsNullOrEmpty(video.Title))
        {
            (_, video) = await GetVideoInfoAsync(videoId, ct);
        }

        return video;
    }

    public async ValueTask<string> ResolveVideoTitleAsync(VideoId videoId, CancellationToken ct = default)
    {
        NicoVideo video = await GetCachedVideoInfoAsync(videoId, ct);
        if (video.Title == null)
        {
            (_, video) = await GetVideoInfoAsync(videoId, ct);
        }

        return video.Title;
    }

    public async ValueTask<string> ResolveThumbnailUrlAsync(VideoId videoId, CancellationToken ct = default)
    {
        NicoVideo video = await GetCachedVideoInfoAsync(videoId, ct);
        return video.ThumbnailUrl;
    }


    public async ValueTask<IDictionary<VideoId, NicoVideoOwner>> ResolveVideoOwnersAsync(IEnumerable<VideoId> videoIds, CancellationToken ct = default)
    {
        List<NicoVideo> cachedVideos = await GetCachedVideoInfoItemsAsync(videoIds, ct);
        return cachedVideos.ToDictionary(x => x.VideoId, x => x.Owner);
    }

    public async ValueTask<NicoVideoOwner> ResolveVideoOwnerAsync(VideoId videoId, CancellationToken ct = default)
    {
        NicoVideo video = GetCachedVideoInfo(videoId);
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
    public async Task<(VideoIdSearchSingleResponse Response, NicoVideo NicoVideo)> GetVideoInfoAsync(VideoId rawVideoId, CancellationToken ct = default)
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
            VideoIdSearchSingleResponse res = await SearchClient.IdSearchAsync(rawVideoId);

            if (!res.IsOK)
            {
                throw new ArgumentException(rawVideoId);
            }

            NicoVideo nicoVideo = UpdateVideo(rawVideoId, res.Video);
            VideoItem video = res.Video;

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

    private NicoVideo UpdateVideo(VideoId videoId, VideoItem video)
    {
        return UpdateCache(videoId, info =>
        {
            info.Title = video.Title;
            info.VideoAliasId = video.Id;
            info.Length = TimeSpan.FromSeconds(video.LengthInSeconds);
            info.PostedAt = video.FirstRetrieve.DateTime;
            info.ThumbnailUrl = video.ThumbnailUrl.OriginalString;
            info.Description = video.Description;

            info.Owner = video.ProviderType == VideoProviderType.Channel
                ? new NicoVideoOwner()
                {
                    OwnerId = video.CommunityId,
                    UserType = OwnerType.Channel,
                    IconUrl = info.Owner?.IconUrl,
                    ScreenName = info.Owner?.ScreenName,
                }
                : new NicoVideoOwner()
                {
                    OwnerId = video.UserId.ToString(),
                    UserType = video.ProviderType == VideoProviderType.Regular ? OwnerType.User : OwnerType.Channel,
                    IconUrl = info.Owner?.IconUrl,
                    ScreenName = info.Owner?.ScreenName,
                };

            return (video.Deleted != 0, video.Deleted);
        });
    }

    public async IAsyncEnumerable<VideoItem> GetVideoInfoManyAsync(IEnumerable<VideoId> idItems, bool isLatestRequired = true, [EnumeratorCancellation] CancellationToken ct = default)
    {
        VideoIdSearchResponse res = await Task.Run(async () =>
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

        foreach (VideoId videoId in idItems)
        {
            VideoInfo item = res.Videos.FirstOrDefault(x => x.Video.Id == videoId);
            VideoItem video = item?.Video;

            if (video is null && isLatestRequired)
            {
                VideoIdSearchSingleResponse singleRes = await SearchClient.IdSearchAsync(videoId);
                video = singleRes?.Video;
            }

            if (video is null)
            {
                Debug.WriteLine("動画情報の取得に失敗 VideoId: " + videoId);
                continue;
            }

            _ = UpdateVideo(videoId, video);

            yield return video;
        }

    }


    public async Task<WatchPageResponse> GetWatchPageResponseAsync(VideoId videoId, bool noHisotry = false)
    {
        if (_niconicoSession.ServiceStatus.IsOutOfService())
        {
            return null;
        }

        WatchPageResponse data = await _niconicoSession.ToolkitContext.Video.VideoWatch.GetInitialWatchDataAsync(videoId, !noHisotry, !noHisotry);
        if (data.WatchApiResponse?.WatchApiData is not null and var watchData)
        {
            _ = UpdateCache(videoId, info =>
            {
                WatchApiVideo video = watchData.Video;
                info.VideoAliasId = videoId;
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
                }

                return (watchData.Video.IsDeleted, default);
            });
        }

        return data;
    }



}

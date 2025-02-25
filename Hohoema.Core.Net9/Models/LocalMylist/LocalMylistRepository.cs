#nullable enable
using Hohoema.Infra;
using Hohoema.Models.Playlist;
using LiteDB;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.LocalMylist;


public class PlaylistEntity
{
    [BsonId]
    public string Id { get; set; }

    [BsonField]
    public PlaylistItemsSourceOrigin PlaylistOrigin { get; set; }

    [BsonField]
    public string Label { get; set; }

    [BsonField]
    public Uri ThumbnailImage { get; set; }


    [BsonField]
    public int PlaylistSortIndex { get; set; }

    [BsonField]
    public LocalMylistSortKey ItemsSortKey { get; set; }

    [BsonField]
    public LocalMylistSortOrder ItemsSortOrder { get; set; }

}


public class PlaylistItemEntity
{
    [BsonId(autoId: true)]
    public int Id { get; set; }

    [BsonField]
    public string PlaylistId { get; set; }
    [BsonField]
    public string ContentId { get; set; }
}



public sealed class LocalMylistRepository
{
    public sealed class PlaylistDbService : LiteDBServiceBase<PlaylistEntity>
    {
        public PlaylistDbService(LiteDatabase liteDatabase) : base(liteDatabase)
        {
        }

        public PlaylistEntity Get(string playlistId)
        {
            return _collection.FindById(playlistId);
        }

        public IEnumerable<PlaylistEntity> GetPlaylistsFromOrigin(PlaylistItemsSourceOrigin playlistOrigin)
        {
            return _collection.Find(x => x.PlaylistOrigin == playlistOrigin);
        }

        public PlaylistEntity[] GetAllPlaylist()
        {
            return _collection.FindAll().ToArray();
        }
    }

    public sealed class PlaylistItemsDbService : LiteDBServiceBase<PlaylistItemEntity>
    {
        public PlaylistItemsDbService(LiteDatabase liteDatabase)
            : base(liteDatabase)
        {
            _ = _collection.EnsureIndex(nameof(PlaylistItemEntity.PlaylistId));
            _ = _collection.EnsureIndex(nameof(PlaylistItemEntity.ContentId));
        }
        public List<PlaylistItemEntity> GetItems(string playlistId, int start, int count)
        {
            return _collection.Find(x => x.PlaylistId == playlistId).Skip(start).Take(count).ToList();
        }

        public int ClearPlaylistItems(string playlistId)
        {
            return _collection.DeleteMany(x => x.PlaylistId == playlistId);
        }

        public bool DeletePlaylistItem(string playlistId, VideoId contentId)
        {
            string strId = contentId.ToString();
            return _collection.DeleteMany(x => x.PlaylistId == playlistId && x.ContentId == strId) > 0;
        }

        public int DeletePlaylistItem(string playlistId, IEnumerable<VideoId> contentId)
        {
            HashSet<string> hashSet = contentId.Select(x => x.ToString()).ToHashSet();
            return _collection.DeleteMany(x => x.PlaylistId == playlistId && hashSet.Contains(x.ContentId));
        }

        public int CountPlaylisItems(string playlistId)
        {
            try
            {
                return _collection.Count(x => x.PlaylistId == playlistId);
            }
            catch
            {
                return 0;
            }
        }


        public bool ExistPlaylistItem(string playlistId, VideoId videoId)
        {
            string videoIdStr = videoId.ToString();
            return _collection.Exists(x => x.PlaylistId == playlistId && x.ContentId == videoIdStr);
        }
    }



    public LocalMylistRepository(PlaylistDbService playlistDbService, PlaylistItemsDbService playlistItemsDbService)
    {
        _playlistDbService = playlistDbService;
        _itemsDbService = playlistItemsDbService;
    }

    private readonly PlaylistDbService _playlistDbService;
    private readonly PlaylistItemsDbService _itemsDbService;

    public PlaylistEntity GetPlaylist(string playlistId)
    {
        return _playlistDbService.Get(playlistId);
    }

    public PlaylistEntity[] GetAllPlaylist()
    {
        return _playlistDbService.GetAllPlaylist();
    }

    public IEnumerable<PlaylistEntity> GetPlaylistsFromOrigin(PlaylistItemsSourceOrigin origin)
    {
        return _playlistDbService.GetPlaylistsFromOrigin(origin);
    }

    public void UpsertPlaylist(PlaylistEntity playlist)
    {
        _ = _playlistDbService.UpdateItem(playlist);
    }

    public bool DeletePlaylist(string playlistId)
    {
        bool result = _playlistDbService.DeleteItem(playlistId);
        _ = _itemsDbService.ClearPlaylistItems(playlistId);

        return result;
    }



    public List<PlaylistItemEntity> GetItems(string playlistId, int start = 0, int count = int.MaxValue)
    {
        return _itemsDbService.GetItems(playlistId, start, count);
    }

    public int GetCount(string playlistId)
    {
        return _itemsDbService.CountPlaylisItems(playlistId);
    }

    public int ClearItems(string playlistId)
    {
        return _itemsDbService.ClearPlaylistItems(playlistId);
    }

    public PlaylistItemEntity AddItem(string playlistId, string contentId, int index = -1)
    {
        if (_itemsDbService.Exists(x => x.PlaylistId == playlistId && x.ContentId == contentId))
        {
            return null;
        }

        PlaylistItemEntity item = new()
        {
            PlaylistId = playlistId,
            ContentId = contentId,
        };
        _ = _itemsDbService.CreateItem(item);
        return item;
    }

    public List<PlaylistItemEntity> AddItems(string playlistId, IEnumerable<VideoId> items)
    {
        List<PlaylistItemEntity> resultItems = new();
        HashSet<string> videoIds = new();
        foreach (VideoId itemId in items)
        {
            string videoId = itemId.StrId;
            if (videoIds.Contains(videoId)) { continue; }
            if (_itemsDbService.Exists(x => x.PlaylistId == playlistId && x.ContentId == videoId))
            {
                continue;
            }

            PlaylistItemEntity entity = new()
            {
                PlaylistId = playlistId,
                ContentId = videoId,
            };

            resultItems.Add(entity);
            _ = videoIds.Add(videoId);
        }

        _ = _itemsDbService.UpdateItem(resultItems);

        return resultItems;
    }

    public void UpdateItem(PlaylistItemEntity entity)
    {
        _ = _itemsDbService.UpdateItem(entity);
    }

    public bool DeleteItem(string playlistId, VideoId contentId)
    {
        return _itemsDbService.DeletePlaylistItem(playlistId, contentId);
    }

    public int DeleteItems(string playlistId, IEnumerable<VideoId> contentIdList)
    {
        return _itemsDbService.DeletePlaylistItem(playlistId, contentIdList);
    }

    public bool ExistItem(string playlistId, VideoId videoId)
    {
        return _itemsDbService.ExistPlaylistItem(playlistId, videoId);
    }
}

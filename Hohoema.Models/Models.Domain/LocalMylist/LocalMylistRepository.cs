using LiteDB;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiconicoToolkit.Video;
using Hohoema.Models.Domain.Playlist;

namespace Hohoema.Models.Domain.LocalMylist
{
    
    public class PlaylistEntity
    {
        [BsonId]
        public string Id { get; set; }

        [BsonField]
        public PlaylistOrigin PlaylistOrigin { get; set; }

        [BsonField]
        public string Label { get; set; }

        [BsonField]
        public int Count { get; set; }

        [BsonField]
        public Uri ThumbnailImage { get; set; }
    }


    public class PlaylistItemEntity
    {
        [BsonId(autoId: true)]
        public int Id { get; set; }

        [BsonField]
        public string PlaylistId { get; set; }

        [BsonField]
        public string ContentId { get; set; }

        [BsonField]
        public int Index { get; set; }
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

            public IEnumerable<PlaylistEntity> GetPlaylistsFromOrigin(PlaylistOrigin playlistOrigin)
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
                _collection.EnsureIndex(nameof(PlaylistItemEntity.PlaylistId));
                _collection.EnsureIndex(nameof(PlaylistItemEntity.ContentId));
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
                var strId = contentId.ToString();
                return _collection.DeleteMany(x => x.PlaylistId == playlistId && x.ContentId == strId) > 0;
            }

            public int DeletePlaylistItem(string playlistId, IEnumerable<VideoId> contentId)
            {
                var hashSet = contentId.Select(x => x.ToString()).ToHashSet();
                return _collection.DeleteMany(x => x.PlaylistId == playlistId && hashSet.Contains(x.ContentId));
            }

            public int CountPlaylisItems(string playlistId)
            {
                return _collection.Count(x => x.PlaylistId == playlistId);
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

        public IEnumerable<PlaylistEntity> GetPlaylistsFromOrigin(PlaylistOrigin origin)
        {
            return _playlistDbService.GetPlaylistsFromOrigin(origin);
        }

        public void UpsertPlaylist(PlaylistEntity playlist)
        {
            _playlistDbService.UpdateItem(playlist);
        }

        public bool DeletePlaylist(string playlistId)
        {
            return _playlistDbService.DeleteItem(playlistId);
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

        public void AddItem(string playlistId, string contentId)
        {
            if (_itemsDbService.Exists(x => x.PlaylistId == playlistId && x.ContentId == contentId))
            {
                return;
            }

            _itemsDbService.UpdateItem(new PlaylistItemEntity()
            {
                PlaylistId = playlistId,
                ContentId = contentId,
            });
        }

        public void AddItems(string playlistId, IEnumerable<VideoId> items)
        {
            _itemsDbService.UpdateItem(items.Where(itemId => !_itemsDbService.Exists(x => x.PlaylistId == playlistId && x.ContentId == itemId)).Select(item => new PlaylistItemEntity()
            {
                PlaylistId = playlistId,
                ContentId = item,
            }));
        }

        public bool DeleteItem(string playlistId, VideoId contentId)
        {
            return _itemsDbService.DeletePlaylistItem(playlistId, contentId);
        }

        public int DeleteItems(string playlistId, IEnumerable<VideoId> contentIdList)
        {
            return _itemsDbService.DeletePlaylistItem(playlistId, contentIdList);
        }

    }
}

using LiteDB;
using MonkeyCache;

using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Domain.Playlist
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



    public sealed class PlaylistRepository
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
        }

        public sealed class PlaylistItemsDbService : LiteDBServiceBase<PlaylistItemEntity>
        {
            public PlaylistItemsDbService(LiteDatabase liteDatabase)
                : base(liteDatabase)
            {
                _collection.EnsureIndex(nameof(PlaylistItemEntity.PlaylistId));
                _collection.EnsureIndex(nameof(PlaylistItemEntity.ContentId));
            }

            public IEnumerable<PlaylistItemEntity> GetItems(string playlistId)
            {
                return _collection.Find(x => x.PlaylistId == playlistId);
            }

            public int ClearPlaylistItems(string playlistId)
            {
                return _collection.DeleteMany(x => x.PlaylistId == playlistId);
            }

            public bool DeletePlaylistItem(string playlistId, string contentId)
            {
                return _collection.DeleteMany(x => x.PlaylistId == playlistId && x.ContentId == contentId) > 0;
            }

            public int DeletePlaylistItem(string playlistId, IEnumerable<string> contentId)
            {
                var hashSet = contentId.ToHashSet();
                return _collection.DeleteMany(x => x.PlaylistId == playlistId && hashSet.Contains(x.ContentId));
            }
        }



        public PlaylistRepository(PlaylistDbService playlistDbService, PlaylistItemsDbService playlistItemsDbService)
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

        public IEnumerable<PlaylistEntity> GetPlaylistsFromOrigin(PlaylistOrigin origin)
        {
            return _playlistDbService.GetPlaylistsFromOrigin(origin);
        }

        public void Upsert(PlaylistEntity playlist)
        {
            _playlistDbService.UpdateItem(playlist);
        }

        public bool Delete(string playlistId)
        {
            return _playlistDbService.DeleteItem(playlistId);
        }



        public IEnumerable<PlaylistItemEntity> GetItems(string playlistId)
        {
            return _itemsDbService.GetItems(playlistId);
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

        public void AddItems(string playlistId, IEnumerable<string> items)
        {
            _itemsDbService.UpdateItem(items.Where(itemId => !_itemsDbService.Exists(x => x.PlaylistId == playlistId && x.ContentId == itemId)).Select(item => new PlaylistItemEntity()
            {
                PlaylistId = playlistId,
                ContentId = item,
            }));
        }

        public bool DeleteItem(string playlistId, string contentId)
        {
            return _itemsDbService.DeletePlaylistItem(playlistId, contentId);
        }

        public int DeleteItems(string playlistId, IEnumerable<string> contentIdList)
        {
            return _itemsDbService.DeletePlaylistItem(playlistId, contentIdList);
        }

    }
}

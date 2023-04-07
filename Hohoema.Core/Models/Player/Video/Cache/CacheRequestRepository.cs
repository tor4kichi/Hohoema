using LiteDB;
using Hohoema.Infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Player.Video.Cache
{
    public class CacheRequest
    {
        public CacheRequest() { }

        [BsonId]
        public string VideoId { get; set; }

        [BsonField]
        public NicoVideoCacheState CacheState { get; set; }

        [BsonField]
        public DateTime RequestAt { get; set; }

        [BsonField]
        public NicoVideoQuality_Legacy PriorityQuality { get; set; }
    }

    public sealed class CacheRequestRepository : LiteDBServiceBase<CacheRequest>
    {
        public CacheRequestRepository(LiteDatabase database)
            : base(database)
        {
            this._collection.EnsureIndex(x => x.CacheState);
            this._collection.EnsureIndex(x => x.RequestAt);
        }

        public bool TryGetPendingFirstItem(out CacheRequest cacheRequest)
        {
            var req = this._collection.FindOne(x => x.CacheState == NicoVideoCacheState.Pending);
            cacheRequest = req;
            return req != null;
        }

        public bool TryGet(string videoId, out CacheRequest request)
        {
            var req = _collection.FindById(videoId);
            request = req;
            return req != null;
        }

        public bool TryRemove(string videoId, out CacheRequest request)
        {
            if (TryGet(videoId, out var req))
            {
                request = req;
                return _collection.Delete(req.VideoId);
            }
            else
            {
                request = default;
                return false;
            }
        }

        public List<CacheRequest> GetRange(int start, int length)
        {
            return _collection.Find(Query.All(nameof(CacheRequest.RequestAt), Query.Descending), start, length).ToList();
        }


        public void ClearAllItems()
        {
            _collection.DeleteAll();
        }
    }
}

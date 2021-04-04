using Hohoema.Models.Infrastructure;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Hohoema.Models.Domain.VideoCache
{
    public sealed class VideoCacheItemRepository
    {
        public class VideoCacheDbService : LiteDBServiceBase<VideoCacheEntity>
        {
            public VideoCacheDbService(LiteDatabase liteDatabase) : base(liteDatabase)
            {
            }

            public IEnumerable<VideoCacheEntity> FindByStatus(VideoCacheStatus status)
            {
                return _collection.Find(x => x.Status == status);
            }

            public long SumVideoCacheSize()
            {
                return _collection.FindAll().Sum(x => x.TotalBytes ?? 0);
            }
        }

        private readonly VideoCacheDbService _videoCacheDbService;

        public VideoCacheItemRepository(VideoCacheDbService videoCacheDbService)
        {
            _videoCacheDbService = videoCacheDbService;
        }

        public IEnumerable<VideoCacheEntity> FindByStatus(VideoCacheStatus status)
        {
            return _videoCacheDbService.FindByStatus(status);
        }


        public VideoCacheEntity GetVideoCache(string id)
        {
            return _videoCacheDbService.FindById(id);
        }

        public void UpdateVideoCache(VideoCacheEntity entity)
        {
            _videoCacheDbService.UpdateItem(entity);
        }

        public bool DeleteVideoCache(string videoId)
        {
            return _videoCacheDbService.DeleteItem(videoId);
        }

        public long SumVideoCacheSize()
        {
            return _videoCacheDbService.SumVideoCacheSize();
        }
    }
}

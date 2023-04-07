using Hohoema.Infra;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.VideoCache
{
    public sealed class VideoCacheItemRepository
    {
        public class VideoCacheDbService : LiteDBServiceBase<VideoCacheEntity>
        {
            public VideoCacheDbService(LiteDatabase liteDatabase) : base(liteDatabase)
            {
            }


            public bool ExistsByStatus(VideoCacheStatus status)
            {
                return _collection.Exists(x => x.Status == status);
            }

            public IEnumerable<VideoCacheEntity> FindByStatus(VideoCacheStatus status)
            {
                return _collection.Find(x => x.Status == status);
            }

            public long SumVideoCacheSize()
            {
                return _collection.FindAll().Sum(x => x.TotalBytes ?? 0);
            }

            public IEnumerable<VideoCacheEntity> GetRange(int head, int count, VideoCacheStatus status)
            {
                return _collection.FindAll().Where(x => x.Status == status).Skip(head).Take(count);
            }

            public IEnumerable<VideoCacheEntity> GetRangeOrderByRequestedAt(int head, int count, VideoCacheStatus? status, bool decsending)
            {
                var enumerable = _collection.FindAll();
                if (status is not null and VideoCacheStatus statusNotNull)
                {
                    enumerable = enumerable.Where(x => x.Status == statusNotNull);
                }

                if (decsending)
                {
                    enumerable = enumerable.OrderByDescending(x => x.RequestedAt);
                }
                else
                {
                    enumerable = enumerable.OrderBy(x => x.RequestedAt);
                }

                return enumerable.Skip(head).Take(count);
            }
        }

        private readonly VideoCacheDbService _videoCacheDbService;

        public VideoCacheItemRepository(VideoCacheDbService videoCacheDbService)
        {
            _videoCacheDbService = videoCacheDbService;
        }

        public bool ExistsByStatus(VideoCacheStatus status)
        {
            return _videoCacheDbService.ExistsByStatus(status);
        }

        public IEnumerable<VideoCacheEntity> FindByStatus(VideoCacheStatus status)
        {
            return _videoCacheDbService.FindByStatus(status);
        }


        public VideoCacheEntity GetVideoCache(string videoId)
        {
            return _videoCacheDbService.FindById(videoId);
        }

        public IEnumerable<VideoCacheEntity> GetItems(int head, int count, VideoCacheStatus status)
        {
            return _videoCacheDbService.GetRange(head, count, status);
        }

        public IEnumerable<VideoCacheEntity> GetItemsOrderByRequestedAt(int head, int count, VideoCacheStatus? status, bool decsending)
        {
            return _videoCacheDbService.GetRangeOrderByRequestedAt(head, count, status, decsending);
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

        public int GetTotalCount()
        {
            return _videoCacheDbService.Count();
        }
    }
}

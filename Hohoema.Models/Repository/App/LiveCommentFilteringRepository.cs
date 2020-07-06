using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.App
{
    public sealed class LiveCommentFilteringRepository : FlagsRepositoryBase
    {
        LiveNgCommentOwnerRepository _liveNgCommentOwnerRepository;
        public LiveCommentFilteringRepository(
            LiveNgCommentOwnerRepository liveNgCommentOwnerRepository
            )
        {
            _liveNgCommentOwnerRepository = liveNgCommentOwnerRepository;
        }

        bool _IsFilteringCommentOwnerIdEnabled;
        public bool IsFilteringCommentOwnerIdEnabled
        {
            get => _IsFilteringCommentOwnerIdEnabled;
            set => SetProperty(ref _IsFilteringCommentOwnerIdEnabled, value);
        }


        public void AddNgCommentOwner(string userId, string name = null)
        {
            _liveNgCommentOwnerRepository.CreateItem(new LiveNgCommentOwner { UserId = userId, Name = name });
        }

        public bool RemoveNgCommentOwner(string userId)
        {
            return _liveNgCommentOwnerRepository.DeleteItem(userId);
        }

        public bool IsNgCommentOwner(string userId)
        {
            return _liveNgCommentOwnerRepository.IsNgOwner(userId);
        }

        
        public class LiveNgCommentOwnerRepository : LiteDBServiceBase<LiveNgCommentOwner>
        {
            public LiveNgCommentOwnerRepository(ILiteDatabase liteDatabase)
            : base(liteDatabase)
            { }


            public bool IsNgOwner(string userId)
            {
                return _collection.Exists(x => x.UserId == userId);
            }
        }

        public class LiveNgCommentOwner
        {
            [BsonId]
            public string UserId { get; internal set; }

            [BsonField]
            public string Name { get; internal set; }
        }
    }
}

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
        public LiveCommentFilteringRepository()
        {
            _liveNgCommentOwnerRepository = new LiveNgCommentOwnerRepository();
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

        
        class LiveNgCommentOwnerRepository : LocalLiteDBService<LiveNgCommentOwner>
        {
            public bool IsNgOwner(string userId)
            {
                return _collection.Exists(x => x.UserId == userId);
            }
        }

        class LiveNgCommentOwner
        {
            [BsonId]
            public string UserId { get; internal set; }

            [BsonField]
            public string Name { get; internal set; }
        }
    }
}

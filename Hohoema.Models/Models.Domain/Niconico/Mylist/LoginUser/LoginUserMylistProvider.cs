using I18NPortable;
using LiteDB;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NiconicoToolkit.Mylist;
using Uno.Extensions;
using NiconicoToolkit.Account;
using NiconicoToolkit.Mylist.LoginUser;
using NiconicoToolkit.Video;

namespace Hohoema.Models.Domain.Niconico.Mylist.LoginUser
{
    public sealed class LoginUserMylistProvider : ProviderBase
    {
        public sealed class LoginUserMylistItemIdRepository : LiteDBServiceBase<LoginUserMylistItemIdEntry>
        {
            public LoginUserMylistItemIdRepository(LiteDatabase liteDatabase) : base(liteDatabase)
            {
                _collection.EnsureIndex(x => x.VideoId);
                _collection.EnsureIndex(x => x.MylistGroupId);
            }

            public void AddItem(long itemId, string mylistId, string videoId)
            {
                _collection.Upsert(new LoginUserMylistItemIdEntry() { ItemId = itemId, MylistGroupId = mylistId, VideoId = videoId });
            }

            public long GetItemId(string mylistId, string videoId)
            {
                return _collection.FindOne(x => x.MylistGroupId == mylistId && x.VideoId == videoId)?.ItemId ?? throw new InvalidOperationException();
            }

            public void Clear()
            {
                _collection.DeleteAll();
            }
        }

        public sealed class LoginUserMylistItemIdEntry
        {
            [BsonId]
            public long ItemId { get; set; }

            [BsonField]
            public string MylistGroupId { get; set; }

            [BsonField]
            public string VideoId { get; set; }
        }



        private readonly NicoVideoCacheRepository _nicoVideoRepository;
        private readonly NicoVideoProvider _nicoVideoProvider;
        private readonly LoginUserMylistItemIdRepository _loginUserMylistItemIdRepository;

        public LoginUserMylistProvider(
            NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider,
            LoginUserMylistItemIdRepository loginUserMylistItemIdRepository
            )
            : base(niconicoSession)
        {
            _nicoVideoProvider = nicoVideoProvider;
            _loginUserMylistItemIdRepository = loginUserMylistItemIdRepository;            
        }


        private async Task<LoginUserMylistPlaylist> GetDefaultMylistAsync()
        {
            if (!_niconicoSession.IsLoggedIn) { throw new System.Exception("");  }
            
            var defMylist = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetWatchAfterItemsAsync(0, 3, MylistSortKey.AddedAt, MylistSortOrder.Asc);

            // TODO: とりあえずマイリストのSortやOrderの取得

            return new LoginUserMylistPlaylist(MylistId.WatchAfterMylistId, this) 
            {
                Name = "WatchAfterMylist".Translate(),
                Count = (int)defMylist.Data.Mylist.TotalItemCount,
                UserId = _niconicoSession.UserId,
                ThumbnailImages = defMylist.Data.Mylist.Items.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
            };
        }

        public async Task<List<LoginUserMylistPlaylist>> GetLoginUserMylistGroups()
        {
            using var _ = await _niconicoSession.SigninLock.LockAsync();
            
            if (!_niconicoSession.IsLoggedIn)
            {
                return null;
            }

            List<LoginUserMylistPlaylist> mylistGroups = new List<LoginUserMylistPlaylist>();

            var defaultMylist = await GetDefaultMylistAsync();

            mylistGroups.Add(defaultMylist);

            var res = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetMylistGroupsAsync(sampleItemCount: 1);

            if (res.Meta.Status != 200)
            {
                return mylistGroups;
            }
            
            foreach (var mylistGroup in res.Data.Mylists)
            {
                var mylist = new LoginUserMylistPlaylist(mylistGroup.Id, this)
                {
                    Name = mylistGroup.Name,
                    Count = (int)mylistGroup.ItemsCount,
                    UserId = mylistGroup.Owner.Id,
                    Description = mylistGroup.Description,
                    IsPublic = mylistGroup.IsPublic,
                    //IconType = mylistGroup.co,
                    DefaultSortKey = mylistGroup.DefaultSortKey,
                    DefaultSortOrder = mylistGroup.DefaultSortOrder,
                    SortIndex = res.Data.Mylists.IndexOf(mylistGroup),
                    ThumbnailImages = mylistGroup.SampleItems.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
                };

                mylistGroups.Add(mylist);
            }

            return mylistGroups;
        }

        public async Task<List<(MylistItem MylistItem, NicoVideo NicoVideo)>> GetLoginUserMylistItemsAsync(IMylist mylist, int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            if (mylist.UserId != _niconicoSession.UserId)
            {
                throw new ArgumentException();
            }

            if (mylist.MylistId.IsWatchAfterMylist)
            {
                var mylistItemsRes = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetWatchAfterItemsAsync(page, pageSize, sortKey, sortOrder);
                var res = mylistItemsRes.Data.Mylist;
                var items = res.Items;
                foreach (var item in items)
                {
                    _loginUserMylistItemIdRepository.AddItem(item.ItemId, mylist.MylistId, item.WatchId);
                }

                return items.Select(x => (x, MylistDataToNicoVideoData(x))).ToList();

            }
            else
            {
                var mylistItemsRes = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetMylistItemsAsync(mylist.Id, (int)page, (int)pageSize, sortKey, sortOrder);
                var res = mylistItemsRes.Data.Mylist;
                var items = res.Items;
                foreach (var item in items)
                {
                    _loginUserMylistItemIdRepository.AddItem(item.ItemId, mylist.MylistId, item.WatchId);
                }

                return items.Select(x => (x, MylistDataToNicoVideoData(x))).ToList();
            }
        }


        private NicoVideo MylistDataToNicoVideoData(MylistItem item)
        {
            return _nicoVideoProvider.UpdateCache(item.WatchId, item.Video, item.IsDeleted);
        }


        public async Task<string> AddMylist(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            var result = await _niconicoSession.ToolkitContext.Mylist.LoginUser.CreateMylistAsync(name, description, isPublic, sortKey, sortOrder);
            return result.Data.MylistId.ToString();
        }

        public async Task<bool> UpdateMylist(LoginUserMylistPlaylist mylist, string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            var result = await _niconicoSession.ToolkitContext.Mylist.LoginUser.UpdateMylistAsync(mylist.MylistId, name, description, isPublic, sortKey, sortOrder);
            try
            {
                mylist.Name = name;
                mylist.IsPublic = isPublic;
                mylist.DefaultSortKey = sortKey;
                mylist.DefaultSortOrder = sortOrder;
                mylist.Description = description;
            }
            catch { }
            return result;
        }


        public async Task<bool> RemoveMylist(MylistId mylistId)
        {
            return await _niconicoSession.ToolkitContext.Mylist.LoginUser.RemoveMylistAsync(mylistId);
        }




        public Task<ContentManageResult> AddMylistItem(MylistId mylistId, VideoId videoId, string mylistComment = "")
        {
            if (mylistId.IsWatchAfterMylist)
            {
                return _niconicoSession.ToolkitContext.Mylist.LoginUser.AddWatchAfterMylistItemAsync(
                    videoId
                    , mylistComment
                    );
            }
            else
            {
                return _niconicoSession.ToolkitContext.Mylist.LoginUser.AddMylistItemAsync(
                    mylistId
                    , videoId
                    , mylistComment
                    );
            }
        }


        public async Task<ContentManageResult> RemoveMylistItem(MylistId mylistId, VideoId videoId)
        {
            var itemId = _loginUserMylistItemIdRepository.GetItemId(mylistId, videoId);

            if (mylistId.IsWatchAfterMylist)
            {
                return await _niconicoSession.ToolkitContext.Mylist.LoginUser.RemoveWatchAfterItemsAsync(new[] { itemId });
            }
            else
            {
                return await _niconicoSession.ToolkitContext.Mylist.LoginUser.RemoveMylistItemsAsync(mylistId, new[] { itemId });
            }
        }

        public async Task<MoveOrCopyMylistItemsResponse> CopyMylistTo(MylistId sourceMylistGroupId, MylistId targetGroupId, IEnumerable<VideoId> videoIdList)
        {
            var items = videoIdList.Select(x => _loginUserMylistItemIdRepository.GetItemId(sourceMylistGroupId, x));
            return await _niconicoSession.ToolkitContext.Mylist.LoginUser.CopyMylistItemsAsync(sourceMylistGroupId, targetGroupId, items.ToArray());
        }


        public async Task<MoveOrCopyMylistItemsResponse> MoveMylistTo(MylistId sourceMylistGroupId, MylistId targetGroupId, IEnumerable<VideoId> videoIdList)
        {
            var items = videoIdList.Select(x => _loginUserMylistItemIdRepository.GetItemId(sourceMylistGroupId, x));
            return await _niconicoSession.ToolkitContext.Mylist.LoginUser.MoveMylistItemsAsync(sourceMylistGroupId, targetGroupId, items.ToArray());
        }
    }

}

#nullable enable
using Hohoema.Contracts.Services;
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using LiteDB;
using NiconicoToolkit.Account;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.Mylist.LoginUser;
using NiconicoToolkit.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Mylist.LoginUser;

public sealed class LoginUserMylistProvider : ProviderBase
{
    public sealed class LoginUserMylistItemIdRepository : LiteDBServiceBase<LoginUserMylistItemIdEntry>
    {
        public LoginUserMylistItemIdRepository(LiteDatabase liteDatabase) : base(liteDatabase)
        {
            _ = _collection.EnsureIndex(x => x.VideoId);
            _ = _collection.EnsureIndex(x => x.MylistGroupId);
        }

        public void AddItem(long itemId, string mylistId, string videoId)
        {
            _ = _collection.Upsert(new LoginUserMylistItemIdEntry() { ItemId = itemId, MylistGroupId = mylistId, VideoId = videoId });
        }

        public long GetItemId(string mylistId, string videoId)
        {
            return _collection.FindOne(x => x.MylistGroupId == mylistId && x.VideoId == videoId)?.ItemId ?? throw new InvalidOperationException();
        }

        public void Clear()
        {
            _ = _collection.DeleteAll();
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

    private readonly ILocalizeService _localizeService;
    private readonly NicoVideoProvider _nicoVideoProvider;
    private readonly LoginUserMylistItemIdRepository _loginUserMylistItemIdRepository;

    public LoginUserMylistProvider(
        ILocalizeService localizeService,
        NiconicoSession niconicoSession,
        NicoVideoProvider nicoVideoProvider,
        LoginUserMylistItemIdRepository loginUserMylistItemIdRepository
        )
        : base(niconicoSession)
    {
        _localizeService = localizeService;
        _nicoVideoProvider = nicoVideoProvider;
        _loginUserMylistItemIdRepository = loginUserMylistItemIdRepository;
    }


    private async Task<LoginUserMylistPlaylist> GetDefaultMylistAsync()
    {
        if (!_niconicoSession.IsLoggedIn) { throw new System.Exception(""); }

        WatchAfterItemsResponse defMylist = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetWatchAfterItemsAsync(0, 3, MylistSortKey.AddedAt, MylistSortOrder.Asc);

        // TODO: とりあえずマイリストのSortやOrderの取得

        return new LoginUserMylistPlaylist(MylistId.WatchAfterMylistId, this)
        {
            Name = _localizeService.Translate("WatchAfterMylist"),
            Count = (int)defMylist.Data.Mylist.TotalCount,
            UserId = _niconicoSession.UserId,
            ThumbnailImages = defMylist.Data.Mylist.Items.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
        };
    }

    public async Task<List<LoginUserMylistPlaylist>> GetLoginUserMylistGroups()
    {
        using IDisposable _ = await _niconicoSession.SigninLock.LockAsync();

        if (!_niconicoSession.IsLoggedIn)
        {
            return null;
        }

        List<LoginUserMylistPlaylist> mylistGroups = new();

        LoginUserMylistPlaylist defaultMylist = await GetDefaultMylistAsync();

        mylistGroups.Add(defaultMylist);

        LoginUserMylistsResponse res = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetMylistGroupsAsync(sampleItemCount: 1);

        if (res.Meta.Status != 200)
        {
            return mylistGroups;
        }

        foreach (NvapiMylistItem mylistGroup in res.Data.Mylists)
        {
            LoginUserMylistPlaylist mylist = new(mylistGroup.Id, this)
            {
                Name = mylistGroup.Name,
                Count = (int)mylistGroup.ItemsCount,
                UserId = mylistGroup.Owner.Id,
                Description = mylistGroup.Description,
                IsPublic = mylistGroup.IsPublic,
                //IconType = mylistGroup.co,
                DefaultSortKey = mylistGroup.DefaultSortKey,
                DefaultSortOrder = mylistGroup.DefaultSortOrder,
                SortIndex = Array.IndexOf(res.Data.Mylists, mylistGroup),
                ThumbnailImages = mylistGroup.SampleItems.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
            };

            mylistGroups.Add(mylist);
        }

        return mylistGroups;
    }

    public async Task<MylistItemsGetResult> GetLoginUserMylistItemsAsync(IMylist mylist, int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
    {
        if (mylist.UserId != _niconicoSession.UserId)
        {
            throw new ArgumentException();
        }

        if (mylist.MylistId.IsWatchAfterMylist)
        {
            WatchAfterItemsResponse mylistItemsRes = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetWatchAfterItemsAsync(page, pageSize, sortKey, sortOrder);
            WatchAfterMylist res = mylistItemsRes.Data.Mylist;
            MylistItem[] items = res.Items;
            foreach (MylistItem item in items)
            {
                _loginUserMylistItemIdRepository.AddItem(item.ItemId, mylist.MylistId, item.WatchId);
            }

            return new MylistItemsGetResult()
            {
                MylistId = mylist.MylistId,
                IsSuccess = true,
                Items = items,
                NicoVideoItems = items.Select(MylistDataToNicoVideoData).ToArray(),
                TotalCount = (int)mylistItemsRes.Data.Mylist.TotalCount,
                HeadPosition = page * pageSize,
            };
        }
        else
        {
            GetMylistItemsResponse mylistItemsRes = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetMylistItemsAsync(mylist.PlaylistId.Id, page, pageSize, sortKey, sortOrder);
            PagenatedNvapiMylistItem res = mylistItemsRes.Data.Mylist;
            MylistItem[] items = res.Items;
            foreach (MylistItem item in items)
            {
                _loginUserMylistItemIdRepository.AddItem(item.ItemId, mylist.MylistId, item.WatchId);
            }

            return new MylistItemsGetResult()
            {
                MylistId = mylist.MylistId,
                IsSuccess = true,
                Items = items,
                NicoVideoItems = items.Select(MylistDataToNicoVideoData).ToArray(),
                TotalCount = (int)mylistItemsRes.Data.Mylist.TotalItemCount,
                HeadPosition = page * pageSize,
            };
        }
    }


    private NicoVideo MylistDataToNicoVideoData(MylistItem item)
    {
        return _nicoVideoProvider.UpdateCache(item.WatchId, item.Video, item.IsDeleted);
    }


    public async Task<string> AddMylist(string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
    {
        CreateMylistResponse result = await _niconicoSession.ToolkitContext.Mylist.LoginUser.CreateMylistAsync(name, description, isPublic, sortKey, sortOrder);
        return result.Data.MylistId.ToString();
    }

    public async Task<bool> UpdateMylist(LoginUserMylistPlaylist mylist, string name, string description, bool isPublic, MylistSortKey sortKey, MylistSortOrder sortOrder)
    {
        bool result = await _niconicoSession.ToolkitContext.Mylist.LoginUser.UpdateMylistAsync(mylist.MylistId, name, description, isPublic, sortKey, sortOrder);
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
        return mylistId.IsWatchAfterMylist
            ? _niconicoSession.ToolkitContext.Mylist.LoginUser.AddWatchAfterMylistItemAsync(
                videoId
                , mylistComment
                )
            : _niconicoSession.ToolkitContext.Mylist.LoginUser.AddMylistItemAsync(
                mylistId
                , videoId
                , mylistComment
                );
    }


    public async Task<ContentManageResult> RemoveMylistItem(MylistId mylistId, VideoId videoId)
    {
        long itemId = _loginUserMylistItemIdRepository.GetItemId(mylistId, videoId);

        return mylistId.IsWatchAfterMylist
            ? await _niconicoSession.ToolkitContext.Mylist.LoginUser.RemoveWatchAfterItemsAsync(new[] { itemId })
            : await _niconicoSession.ToolkitContext.Mylist.LoginUser.RemoveMylistItemsAsync(mylistId, new[] { itemId });
    }

    public async Task<MoveOrCopyMylistItemsResponse> CopyMylistTo(MylistId sourceMylistGroupId, MylistId targetGroupId, IEnumerable<VideoId> videoIdList)
    {
        IEnumerable<long> items = videoIdList.Select(x => _loginUserMylistItemIdRepository.GetItemId(sourceMylistGroupId, x));
        return await _niconicoSession.ToolkitContext.Mylist.LoginUser.CopyMylistItemsAsync(sourceMylistGroupId, targetGroupId, items.ToArray());
    }


    public async Task<MoveOrCopyMylistItemsResponse> MoveMylistTo(MylistId sourceMylistGroupId, MylistId targetGroupId, IEnumerable<VideoId> videoIdList)
    {
        IEnumerable<long> items = videoIdList.Select(x => _loginUserMylistItemIdRepository.GetItemId(sourceMylistGroupId, x));
        return await _niconicoSession.ToolkitContext.Mylist.LoginUser.MoveMylistItemsAsync(sourceMylistGroupId, targetGroupId, items.ToArray());
    }
}

#nullable enable
using Hohoema.Contracts.Services;
using Hohoema.Infra;
using Hohoema.Models.Niconico.Video;
using NiconicoToolkit.Mylist;
using NiconicoToolkit.User;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Hohoema.Models.Niconico.Mylist;

public sealed class MylistProvider : ProviderBase
{
    private readonly ILocalizeService _localizeService;
    private readonly NicoVideoProvider _nicoVideoProvider;

    public MylistProvider(
        ILocalizeService localizeService,
        NiconicoSession niconicoSession,
        NicoVideoProvider nicoVideoProvider
        )
        : base(niconicoSession)
    {
        _localizeService = localizeService;
        _nicoVideoProvider = nicoVideoProvider;
    }




    private async Task<IMylistItem> GetMylistGroupDetail(MylistId mylistId)
    {
        if (mylistId.IsWatchAfterMylist)
        {
            throw new NotSupportedException();
        }
        else
        {
            GetMylistItemsResponse res = await _niconicoSession.ToolkitContext.Mylist.GetMylistItemsAsync(mylistId, 0, 1);

            if (res.Data?.Mylist != null) { return res.Data.Mylist; }

            res = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetMylistItemsAsync(mylistId, 0, 1);

            return res.Data?.Mylist;
        }
    }

    public async Task<MylistPlaylist?> GetMylist(MylistId mylistId)
    {
        if (mylistId.IsWatchAfterMylist)
        {
            NiconicoToolkit.Mylist.LoginUser.WatchAfterItemsResponse res = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetWatchAfterItemsAsync(0, 1);
            NiconicoToolkit.Mylist.LoginUser.WatchAfterMylist detail = res.Data.Mylist;
            MylistPlaylist mylist = new(mylistId, this)
            {
                Name = _localizeService.Translate("WatchAfterMylist"),
                Count = (int)detail.TotalCount,
                IsPublic = true,
                ThumbnailImages = detail.Items?.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
            };
            return mylist;
        }
        else
        {
            IMylistItem detail = await GetMylistGroupDetail(mylistId);

            if (detail == null) { return null; }

            MylistPlaylist mylist = new(detail.Id.ToString(), this)
            {
                Name = detail.Name,
                Count = (int)detail.ItemsCount,
                CreateTime = detail.CreatedAt.DateTime,
                DefaultSortOrder = detail.DefaultSortOrder,
                DefaultSortKey = detail.DefaultSortKey,
                IsPublic = detail.IsPublic,
                SortIndex = 0,
                UserId = detail.Owner.Id,
                Description = detail.Description,
                ThumbnailImages = detail.SampleItems?.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
            };
            return mylist;
        }
    }



    public async Task<List<MylistPlaylist>> GetMylistsByUser(UserId userId, int sampleItemCount = 0)
    {
        GetUserMylistGroupsResponse groups = await _niconicoSession.ToolkitContext.Mylist.GetUserMylistGroupsAsync(userId, sampleItemCount);

        if (groups == null) { return null; }

        List<MylistPlaylist> list = groups.Data.MylistGroups.Select((x, i) =>
        {
            return new MylistPlaylist(x.Id.ToString(), this)
            {
                Name = x.Name,
                Count = (int)x.ItemsCount,
                SortIndex = i,
                UserId = x.Owner.Id,
                Description = x.Description,
                IsPublic = x.IsPublic,
                CreateTime = x.CreatedAt.DateTime,
                DefaultSortKey = x.DefaultSortKey,
                DefaultSortOrder = x.DefaultSortOrder,
                ThumbnailImages = x.SampleItems.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray()
            };
        }
        ).ToList();

        return list;
    }



    public async Task<MylistItemsGetResult> GetMylistVideoItems(MylistId mylistId, int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
    {
        GetMylistItemsResponse res = await _niconicoSession.ToolkitContext.Mylist.GetMylistItemsAsync(mylistId, page, pageSize, sortKey, sortOrder);

        if (res.Meta.IsSuccess is false) { return new MylistItemsGetResult() { IsSuccess = false, MylistId = mylistId }; }

        MylistItem[] videos = res.Data.Mylist.Items;
        List<MylistItem> resultItems = new();
        List<NicoVideo> nicoVideoList = new();

        foreach (MylistItem item in videos)
        {
            NicoVideo nicoVideo = _nicoVideoProvider.UpdateCache(item.WatchId, item.Video, item.IsDeleted);

            nicoVideoList.Add(nicoVideo);
            resultItems.Add(item);
        }

        return new MylistItemsGetResult()
        {
            IsSuccess = true,
            MylistId = mylistId,
            HeadPosition = pageSize * page,
            Items = new ReadOnlyCollection<MylistItem>(resultItems),
            NicoVideoItems = new ReadOnlyCollection<NicoVideo>(nicoVideoList),
            TotalCount = (int)res.Data.Mylist.TotalItemCount,
        };
    }

}

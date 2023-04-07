using NiconicoToolkit.Mylist;
using Hohoema.Models.Niconico.Video;
using Hohoema.Infra;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using I18NPortable;
using NiconicoToolkit.Video;
using NiconicoToolkit.User;

namespace Hohoema.Models.Niconico.Mylist
{
    


    public sealed class MylistProvider : ProviderBase
    {
        private readonly NicoVideoProvider _nicoVideoProvider;

        public MylistProvider(
            NiconicoSession niconicoSession,
            NicoVideoProvider nicoVideoProvider
            )
            : base(niconicoSession)
        {
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
                var res = await _niconicoSession.ToolkitContext.Mylist.GetMylistItemsAsync(mylistId, 0, 1);
                
                if (res.Data?.Mylist != null) { return res.Data.Mylist; }

                res = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetMylistItemsAsync(mylistId, 0, 1);

                return res.Data?.Mylist;
            }
        }

        public async Task<MylistPlaylist?> GetMylist(MylistId mylistId)
        {
            if (mylistId.IsWatchAfterMylist)
            {
                var res = await _niconicoSession.ToolkitContext.Mylist.LoginUser.GetWatchAfterItemsAsync(0, 1);
                var detail = res.Data.Mylist;
                var mylist = new MylistPlaylist(mylistId, this)
                {
                    Name = "WatchAfterMylist".Translate(),
                    Count = (int)detail.TotalCount,
                    IsPublic = true,
                    ThumbnailImages = detail.Items?.Take(3).Select(x => x.Video.Thumbnail.ListingUrl).ToArray(),
                };
                return mylist;
            }
            else
            {
                var detail = await GetMylistGroupDetail(mylistId);

                if (detail == null) { return null; }

                var mylist = new MylistPlaylist(detail.Id.ToString(), this)
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
            var groups = await _niconicoSession.ToolkitContext.Mylist.GetUserMylistGroupsAsync(userId, sampleItemCount);

            if (groups == null) { return null; }

            var list = groups.Data.MylistGroups.Select((x, i) =>
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
            var res = await _niconicoSession.ToolkitContext.Mylist.GetMylistItemsAsync(mylistId, page, pageSize, sortKey, sortOrder);
            
            if (res.Meta.IsSuccess is false) { return new MylistItemsGetResult() { IsSuccess = false, MylistId = mylistId }; }

            var videos = res.Data.Mylist.Items;
            var resultItems = new List<MylistItem>();
            var nicoVideoList = new List<NicoVideo>();

            foreach (var item in videos)
            {
                var nicoVideo = _nicoVideoProvider.UpdateCache(item.WatchId, item.Video, item.IsDeleted);

                nicoVideoList.Add(nicoVideo);
                resultItems.Add(item);
            }

            return new MylistItemsGetResult()
            {
                IsSuccess = true,
                MylistId = mylistId,
                HeadPosition = (int)(pageSize * page),
                Items = new ReadOnlyCollection<MylistItem>(resultItems),
                NicoVideoItems = new ReadOnlyCollection<NicoVideo>(nicoVideoList),
                TotalCount = (int)res.Data.Mylist.TotalItemCount,
            };
        }

    }
}

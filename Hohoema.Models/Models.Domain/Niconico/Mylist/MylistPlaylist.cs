

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using NiconicoToolkit;
using NiconicoToolkit.Mylist;

namespace Hohoema.Models.Domain.Niconico.Mylist
{
    public class MylistPlaylist : IMylist
    {
        private readonly MylistProvider _mylistProvider;

        public MylistPlaylist(MylistId id)
        {
            MylistId = id;
        }

        public MylistPlaylist(MylistId id, MylistProvider mylistProvider)
        {
            MylistId = id;
            _mylistProvider = mylistProvider;
        }

        public MylistId MylistId { get; }

        public string Name { get; internal set; }

        public int Count { get; internal set; }

        public int SortIndex { get; internal set; }

        public string Description { get; internal set; }

        public string UserId { get; internal set; }

        public bool IsPublic { get; internal set; }

        public MylistSortKey DefaultSortKey { get; internal set; }
        public MylistSortOrder DefaultSortOrder { get; internal set; }

        public Uri[] ThumbnailImages { get; internal set; }

        public Uri ThumbnailImage => ThumbnailImages?.FirstOrDefault();

        public DateTime CreateTime { get; internal set; }

        string IPlaylist.Id => MylistId.ToString();

        public async Task<MylistItemsGetResult> GetItemsAsync(int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
           
            //if (this.MylistId.IsWatchAfterMylist)
            {
                //throw new ArgumentException("とりあえずマイリストはログインしていなければアクセスできません。");
            }

            if (!IsPublic)
            {
                throw new ArgumentException("非公開マイリストはアクセスできません。");
            }

            // 他ユーザーマイリストとして取得を実行
            try
            {
                var result = await GetMylistItemsWithRangeAsync(page, pageSize, sortKey, sortOrder);

                return new MylistItemsGetResult()
                {
                    IsSuccess = true,
                    IsDefaultMylist = this.MylistId.IsWatchAfterMylist,
                    Mylist = this,
                    IsLoginUserMylist = false,
                    NicoVideoItems = result.NicoVideoItems,
                    Items = result.Items,
                    ItemsHeadPosition = result.HeadPosition,
                    TotalCount = result.TotalCount,
                };
            }
            catch
            {

            }

            return new MylistItemsGetResult() { IsSuccess = false };
        }

        public Task<MylistProvider.MylistItemsGetResult> GetMylistItemsWithRangeAsync(int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            return _mylistProvider.GetMylistVideoItems(MylistId, page, pageSize, sortKey, sortOrder);
        }

        public async Task<MylistProvider.MylistItemsGetResult> GetMylistAllItems(MylistSortKey sortKey = MylistSortKey.AddedAt, MylistSortOrder sortOrder = MylistSortOrder.Asc)
        {
            int page = 0;
            const int pageSize = 25;

            var firstResult = await _mylistProvider.GetMylistVideoItems(MylistId, page, pageSize, sortKey, sortOrder);
            if (!firstResult.IsSuccess || firstResult.TotalCount == firstResult.Items.Count)
            {
                return firstResult;
            }

            page++;

            var nicovideoItemsList = new List<NicoVideo>(firstResult.NicoVideoItems);
            var itemsList = new List<MylistItem>(firstResult.Items);
            var totalCount = firstResult.TotalCount;
            var currentCount = firstResult.Items.Count;
            do
            {
                await Task.Delay(500);
                var result = await _mylistProvider.GetMylistVideoItems(MylistId, page, pageSize, sortKey, sortOrder);
                if (result.IsSuccess)
                {
                    itemsList.AddRange(result.Items);
                    nicovideoItemsList.AddRange(result.NicoVideoItems);
                }

                page++;
                currentCount += result.Items.Count;
            }
            while (currentCount < totalCount);

            return new MylistProvider.MylistItemsGetResult()
            {
                MylistId = MylistId,
                HeadPosition = 0,
                TotalCount = totalCount,
                IsSuccess = true,
                Items = new ReadOnlyCollection<MylistItem>(itemsList),
                NicoVideoItems = new ReadOnlyCollection<NicoVideo>(nicovideoItemsList)
            };
        }
    }
}

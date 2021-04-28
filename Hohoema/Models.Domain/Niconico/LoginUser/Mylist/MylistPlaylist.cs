

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Mntone.Nico2.Users.Mylist;
using Hohoema.Models.Domain.Niconico.Video;

namespace Hohoema.Models.Domain.Niconico.LoginUser.Mylist
{
    public class MylistPlaylist : IMylist
    {
        private readonly MylistProvider _mylistProvider;

        public MylistPlaylist(string id)
        {
            Id = id;
        }

        public MylistPlaylist(string id, MylistProvider mylistProvider)
        {
            Id = id;
            _mylistProvider = mylistProvider;
        }

        public string Id { get; }

        public string Label { get; internal set; }

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


        public async Task<MylistItemsGetResult> GetItemsAsync(MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
        {
           
            //if (this.IsDefaultMylist())
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
                var result = await GetMylistItemsWithRangeAsync(sortKey, sortOrder, pageSize, page);

                return new MylistItemsGetResult()
                {
                    IsSuccess = true,
                    IsDefaultMylist = this.IsDefaultMylist(),
                    Mylist = this,
                    IsLoginUserMylist = false,
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

        public Task<MylistProvider.MylistItemsGetResult> GetMylistItemsWithRangeAsync(MylistSortKey sortKey, MylistSortOrder sortOrder, uint pageSize, uint page)
        {
            return _mylistProvider.GetMylistGroupVideo(Id, sortKey, sortOrder, pageSize, page);
        }

        public async Task<MylistProvider.MylistItemsGetResult> GetMylistAllItems(MylistSortKey sortKey = MylistSortKey.AddedAt, MylistSortOrder sortOrder = MylistSortOrder.Asc)
        {
            uint page = 0;
            const int pageSize = 25;

            var firstResult = await _mylistProvider.GetMylistGroupVideo(Id, sortKey, sortOrder, pageSize, page);
            if (!firstResult.IsSuccess || firstResult.TotalCount == firstResult.Items.Count)
            {
                return firstResult;
            }

            page++;

            var itemsList = new List<NicoVideo>(firstResult.Items);
            var totalCount = firstResult.TotalCount;
            var currentCount = firstResult.Items.Count;
            do
            {
                await Task.Delay(500);
                var result = await _mylistProvider.GetMylistGroupVideo(Id, sortKey, sortOrder, pageSize, page);
                if (result.IsSuccess)
                {
                    itemsList.AddRange(result.Items);
                }

                page++;
                currentCount += result.Items.Count;
            }
            while (currentCount < totalCount);

            return new MylistProvider.MylistItemsGetResult()
            {
                MylistId = Id,
                HeadPosition = 0,
                TotalCount = totalCount,
                IsSuccess = true,
                Items = new ReadOnlyCollection<NicoVideo>(itemsList)
            };
        }
    }
}

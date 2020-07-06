using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Models.Repository.Niconico.Mylist
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

        public string Label { get; protected internal set; }

        public int Count { get; protected internal set; }

        public int SortIndex { get; protected internal set; }

        public string Description { get; protected internal set; }

        public string UserId { get; protected internal set; }

        public bool IsPublic { get; protected internal set; }

        public MylistGroupIconType IconType { get; protected internal set; }

        public Order Order { get; protected internal set; }

        public Sort Sort { get; protected internal set; }

        public DateTime UpdateTime { get; protected internal set; }

        public DateTime CreateTime { get; protected internal set; }


        public async Task<MylistItemsGetResult> GetItemsAsync(int start, int count)
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
                var result = await GetMylistItemsWithRangeAsync(start, count);

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

        public Task<MylistProvider.MylistItemsGetResult> GetMylistItemsWithRangeAsync(int start, int count)
        {
            return _mylistProvider.GetMylistGroupVideo(Id, start, count);
        }

        public async Task<MylistProvider.MylistItemsGetResult> GetMylistAllItems()
        {
            var firstResult = await _mylistProvider.GetMylistGroupVideo(Id, 0, 150);
            if (!firstResult.IsSuccess || firstResult.TotalCount == firstResult.Items.Count)
            {
                return firstResult;
            }

            var itemsList = new List<Database.NicoVideo>(firstResult.Items);
            var totalCount = firstResult.TotalCount;
            var currentCount = firstResult.Items.Count;
            do
            {
                await Task.Delay(500);
                var result = await _mylistProvider.GetMylistGroupVideo(Id, currentCount, 150);
                if (result.IsSuccess)
                {
                    itemsList.AddRange(result.Items);
                }

                currentCount += result.Items.Count;
            }
            while (currentCount < totalCount);

            return new MylistProvider.MylistItemsGetResult()
            {
                MylistId = Id,
                HeadPosition = 0,
                TotalCount = totalCount,
                IsSuccess = true,
                Items = new ReadOnlyCollection<Database.NicoVideo>(itemsList)
            };
        }
    }
}

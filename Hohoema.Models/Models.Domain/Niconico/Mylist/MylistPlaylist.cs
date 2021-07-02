

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Hohoema.Models.Domain.Niconico.Video;
using Hohoema.Models.Domain.Playlist;
using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit;
using NiconicoToolkit.Mylist;

namespace Hohoema.Models.Domain.Niconico.Mylist
{
    public record MylistPlaylistSortOptions(MylistSortKey SortKey, MylistSortOrder SortOrder) : IPlaylistSortOptions
    {
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }

        public static MylistPlaylistSortOptions Deserialize(string serializedText)
        {
            if (string.IsNullOrEmpty(serializedText)) { return new MylistPlaylistSortOptions(MylistSortKey.RegisteredAt, MylistSortOrder.Desc); }

            return JsonSerializer.Deserialize<MylistPlaylistSortOptions>(serializedText);
        }
    }

    public class MylistPlaylist : IMylist, IPlaylist
    {
        private readonly MylistProvider _mylistProvider;

        public MylistPlaylist(MylistId id)
        {
            MylistId = id;
            PlaylistId = new PlaylistId() { Id = id, Origin = PlaylistItemsSourceOrigin.Mylist };
        }

        public MylistPlaylist(MylistId id, MylistProvider mylistProvider)
            : this(id)
        {
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

        // TODO: 
        MylistPlaylistSortOptions _SortOptions;
        public MylistPlaylistSortOptions SortOptions 
        {
            get => _SortOptions ??= new MylistPlaylistSortOptions(DefaultSortKey, DefaultSortOrder);
            set => _SortOptions = value;
        }

        IPlaylistSortOptions IPlaylist.SortOptions
        {
            get => SortOptions;
            set => SortOptions = (MylistPlaylistSortOptions)value;
        }
        

        public Uri[] ThumbnailImages { get; internal set; }

        public Uri ThumbnailImage => ThumbnailImages?.FirstOrDefault();

        public DateTime CreateTime { get; internal set; }

        public PlaylistId PlaylistId { get; }

        public virtual async Task<MylistItemsGetResult> GetItemsAsync(int page, int pageSize, MylistSortKey sortKey, MylistSortOrder sortOrder)
        {
            Guard.IsTrue(IsPublic, nameof(IsPublic));

            // 他ユーザーマイリストとして取得を実行
            try
            {
                return await _mylistProvider.GetMylistVideoItems(MylistId, page, pageSize, sortKey, sortOrder);
            }
            catch
            {

            }

            return new MylistItemsGetResult() { IsSuccess = false };
        }


        const int _pageSize = 100;

        public async Task<MylistItemsGetResult> GetAllItemsAsync(MylistSortKey sortKey = MylistSortKey.AddedAt, MylistSortOrder sortOrder = MylistSortOrder.Asc)
        {
            int page = 0;
            var firstResult = await GetItemsAsync(page, _pageSize, sortKey, sortOrder);
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
                var result = await GetItemsAsync(page, _pageSize, sortKey, sortOrder);
                if (result.IsSuccess)
                {
                    itemsList.AddRange(result.Items);
                    nicovideoItemsList.AddRange(result.NicoVideoItems);
                }

                page++;
                currentCount += result.Items.Count;
            }
            while (currentCount < totalCount);

            return new MylistItemsGetResult()
            {
                MylistId = MylistId,
                HeadPosition = 0,
                TotalCount = totalCount,
                IsSuccess = true,
                Items = new ReadOnlyCollection<MylistItem>(itemsList),
                NicoVideoItems = new ReadOnlyCollection<NicoVideo>(nicovideoItemsList)
            };
        }

        public int OneTimeItemsCount => _pageSize;

        List<NicoVideo> _MylistItems;        

        public int IndexOf(IVideoContent video)
        {
            Guard.IsNotNull(_MylistItems, nameof(_MylistItems));
            return _MylistItems.FindIndex(x => x.VideoId == video.VideoId);
        }

        public bool Contains(IVideoContent video)
        {
            return IndexOf(video) >= 0;
        }

        async Task PreparePagenation(CancellationToken ct = default)
        {
            if (_MylistItems == null)
            {
                var sortOptions = SortOptions;
                var result = await GetAllItemsAsync(sortOptions.SortKey, sortOptions.SortOrder);
                _MylistItems = result.NicoVideoItems.ToList();
            }
        }

        public virtual async Task<IEnumerable<IVideoContent>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken ct = default)
        {
            await PreparePagenation();

            var start = pageIndex * pageSize;
            return _MylistItems.Skip(start).Take(pageSize);
        }
    }
}

using Mntone.Nico2;
using Mntone.Nico2.Searches;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using Mntone.Nico2.Searches.Mylist;
using Mntone.Nico2.Searches.Video;
using NicoPlayerHohoema.Repository.Playlist;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class SearchProvider : ProviderBase
    {
        private readonly MylistProvider _mylistProvider;

        // TODO: タグによる生放送検索を別メソッドに分ける

        public SearchProvider(
            NiconicoSession niconicoSession,
            MylistProvider mylistProvider
            )
            : base(niconicoSession)
        {
            _mylistProvider = mylistProvider;
        }



        public async Task<VideoListingResponse> GetKeywordSearch(string keyword, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Search.VideoSearchWithKeywordAsync(keyword, from, limit, sort, order);
            });
            
        }

        public async Task<VideoListingResponse> GetTagSearch(string tag, uint from, uint limit, Sort sort = Sort.FirstRetrieve, Order order = Order.Descending)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Search.VideoSearchWithTagAsync(tag, from, limit, sort, order);
            });
            
        }

        public async Task<Mntone.Nico2.Searches.Live.LiveSearchResponse> LiveSearchAsync(
            string q,
            int offset,
            int limit,
            SearchTargetType targets = SearchTargetType.All,
            LiveSearchFieldType fields = LiveSearchFieldType.All,
            LiveSearchSortType sortType = LiveSearchSortType.StartTime | LiveSearchSortType.SortDecsending,
            Expression<Func<SearchFilterField, bool>> filterExpression = null
            )
        {
            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Search.LiveSearchAsync(
                    q, offset, limit, targets, fields, sortType, filterExpression
                );

            });
            
        }


        public async Task<Mntone.Nico2.Searches.Suggestion.SuggestionResponse> GetSearchSuggestKeyword(string keyword)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Search.GetSuggestionAsync(keyword);
            });
            
        }







        public async Task<CommunitySearchResponse> SearchCommunity(
            string keyword
            , uint page
            , CommunitySearchSort sort = CommunitySearchSort.CreatedAt
            , Order order = Order.Descending
            , CommunitySearchMode mode = CommunitySearchMode.Keyword
            )
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Search.CommunitySearchAsync(keyword, page, sort, order, mode);
            });
            
        }

        public class MylistSearchResult
        {
            public bool IsSuccess { get; set; }
            public List<MylistPlaylist> Items { get; set; }
            public int TotalCount { get; set; }
        }

        public async Task<MylistSearchResult> MylistSearchAsync(string keyword, uint head, uint count, Sort? sort, Order? order)
        {
            var res = await ContextActionAsync(async context =>
            {
                return await context.Search.MylistSearchAsync(keyword, head, count, sort, order);
            });

            if (res.MylistGroupItems?.Any() ?? false)
            {
                var items = res.MylistGroupItems
                    .Select(x => new MylistPlaylist(x.Id, _mylistProvider) 
                    {
                        Label = x.Name,
                        Count = (int)x.ItemCount,
                        Description = x.Description, UpdateTime = x.UpdateTime 
                    }
                    )
                    .ToList();

                return new MylistSearchResult()
                {
                    IsSuccess = true,
                    Items = items,
                    TotalCount = (int)res.GetTotalCount()
                };
            }
            else
            {
                return new MylistSearchResult() { IsSuccess = false };
            }
                
        }
    }
}

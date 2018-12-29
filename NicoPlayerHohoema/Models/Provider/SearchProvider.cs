using Mntone.Nico2;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Mylist;
using Mntone.Nico2.Searches.Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Provider
{
    public sealed class SearchProvider : ProviderBase
    {
        // TODO: タグによる生放送検索を別メソッドに分ける

        public SearchProvider(NiconicoSession niconicoSession)
            : base(niconicoSession)
        {
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

        public async Task<Mntone.Nico2.Searches.Live.NicoliveVideoResponse> LiveSearchAsync(
            string word,
            bool isTagSearch,
            Mntone.Nico2.Live.CommunityType? provider = null,
            uint from = 0,
            uint length = 30,
            Order? order = null,
            Mntone.Nico2.Searches.Live.NicoliveSearchSort? sort = null,
            Mntone.Nico2.Searches.Live.NicoliveSearchMode? mode = null
            )
        {
            return await ContextActionWithPageAccessWaitAsync(async context =>
            {
                return await context.Search.LiveSearchAsync(
                word,
                isTagSearch,
                provider,
                from,
                length,
                order,
                sort,
                mode
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

        public async Task<MylistSearchResponse> MylistSearchAsync(string keyword, uint head, uint count, Sort? sort, Order? order)
        {
            return await ContextActionAsync(async context =>
            {
                return await context.Search.MylistSearchAsync(keyword, head, count, sort, order);
            });
                
        }
    }
}

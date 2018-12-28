using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.Services.Page
{
    public static class PageManagerSearchExtention
    {
        public static void Search(this PageManager pageManager, ISearchPagePayloadContent searchPayload, bool forgetLastSearch = false)
        {
            HohoemaPageType resultPageType = HohoemaPageType.Search;
            if (searchPayload is KeywordSearchPagePayloadContent)
            {
                resultPageType = HohoemaPageType.SearchResultKeyword;
            }
            else if (searchPayload is TagSearchPagePayloadContent)
            {
                resultPageType = HohoemaPageType.SearchResultTag;
            }
            else if (searchPayload is MylistSearchPagePayloadContent)
            {
                resultPageType = HohoemaPageType.SearchResultMylist;
            }
            else if (searchPayload is CommunitySearchPagePayloadContent)
            {
                resultPageType = HohoemaPageType.SearchResultCommunity;
            }
            else if (searchPayload is LiveSearchPagePayloadContent)
            {
                resultPageType = HohoemaPageType.SearchResultLive;
            }

            pageManager.OpenPage(resultPageType, searchPayload.ToParameterString(), forgetLastSearch);
         }

        public static void SearchKeyword(this PageManager pageManager, string content, Mntone.Nico2.Order order, Mntone.Nico2.Sort sort, bool isForgetNavigation = false)
        {
            var payload = new KeywordSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultKeyword, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchTag(this PageManager pageManager, string content, Mntone.Nico2.Order order, Mntone.Nico2.Sort sort, bool isForgetNavigation = false)
        {
            var payload = new KeywordSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultTag, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchCommunity(this PageManager pageManager, string content, Mntone.Nico2.Order order, Mntone.Nico2.Sort sort, bool isForgetNavigation = false)
        {
            var payload = new KeywordSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultKeyword, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchMylist(this PageManager pageManager, string content, Mntone.Nico2.Order order, Mntone.Nico2.Sort sort, bool isForgetNavigation = false)
        {
            var payload = new MylistSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultMylist, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchCommunity(this PageManager pageManager, string content, Mntone.Nico2.Order order, CommunitySearchSort sort, CommunitySearchMode mode, bool isForgetNavigation = false)
        {
            var payload = new CommunitySearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
                Mode = mode
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultCommunity, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchLive(this PageManager pageManager, string content, bool isTagSearch, CommunityType? provider, Mntone.Nico2.Order order, NicoliveSearchSort sort, NicoliveSearchMode? mode, bool isForgetNavigation = false)
        {
            var payload = new LiveSearchPagePayloadContent()
            {
                Keyword = content,
                Mode = mode,
                IsTagSearch = isTagSearch,
                Order = order,
                Sort = sort,
                Provider = provider
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultLive, payload.ToParameterString(), isForgetNavigation);
        }
    }
}

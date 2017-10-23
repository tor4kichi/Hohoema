using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using Mntone.Nico2.Live;
using NicoPlayerHohoema.Models;

namespace NicoPlayerHohoema.Helpers
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

            bool isRequireForgetLastNavigation = false;
            if (pageManager.CurrentPageType == HohoemaPageType.SearchResultCommunity ||
                pageManager.CurrentPageType == HohoemaPageType.SearchResultKeyword ||
                pageManager.CurrentPageType == HohoemaPageType.SearchResultTag ||
                pageManager.CurrentPageType == HohoemaPageType.SearchResultLive ||
                pageManager.CurrentPageType == HohoemaPageType.SearchResultMylist )
            {
                isRequireForgetLastNavigation = true;
            }

            pageManager.OpenPage(resultPageType, searchPayload.ToParameterString(), isRequireForgetLastNavigation);
         }

        public static void SearchTag(this PageManager pageManager, string content, Mntone.Nico2.Order order, Mntone.Nico2.Sort sort)
        {
            var payload = new KeywordSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultTag, payload.ToParameterString());
        }

        public static void SearchCommunity(this PageManager pageManager, string content, Mntone.Nico2.Order order, Mntone.Nico2.Sort sort)
        {
            var payload = new KeywordSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultKeyword, payload.ToParameterString());
        }

        public static void SearchMylist(this PageManager pageManager, string content, Mntone.Nico2.Order order, Mntone.Nico2.Sort sort)
        {
            var payload = new MylistSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultMylist, payload.ToParameterString());
        }

        public static void SearchCommunity(this PageManager pageManager, string content, Mntone.Nico2.Order order, CommunitySearchSort sort, CommunitySearchMode mode)
        {
            var payload = new CommunitySearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
                Mode = mode
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultCommunity, payload.ToParameterString());
        }

        public static void SearchLive(this PageManager pageManager, string content, bool isTagSearch, CommunityType? provider, Mntone.Nico2.Order order, NicoliveSearchSort sort, NicoliveSearchMode? mode)
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

            pageManager.OpenPage(HohoemaPageType.SearchResultLive, payload.ToParameterString());
        }
    }
}

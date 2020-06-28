using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mntone.Nico2.Searches.Community;
using Mntone.Nico2.Searches.Live;
using Mntone.Nico2.Live;
using Hohoema.Models;
using Prism.Navigation;

namespace Hohoema.Services.Page
{
    public static class PageManagerSearchExtention
    {
        public static void Search(this PageManager pageManager, SearchTarget target, string keyword, bool forgetLastSearch = false)
        {
            HohoemaPageType resultPageType = HohoemaPageType.Search;
            switch (target)
            {
                case SearchTarget.Keyword:
                    resultPageType = HohoemaPageType.SearchResultKeyword;
                    break;
                case SearchTarget.Tag:
                    resultPageType = HohoemaPageType.SearchResultTag;
                    break;
                case SearchTarget.Niconama:
                    resultPageType = HohoemaPageType.SearchResultLive;
                    break;
                case SearchTarget.Mylist:
                    resultPageType = HohoemaPageType.SearchResultMylist;
                    break;
                case SearchTarget.Community:
                    resultPageType = HohoemaPageType.SearchResultCommunity;
                    break;
                default:
                    break;
            }

            var p = new NavigationParameters
            {
                { "keyword", System.Net.WebUtility.UrlEncode(keyword) },
                { "target", resultPageType }
            };

            pageManager.OpenPage(resultPageType, p, forgetLastSearch ? NavigationStackBehavior.NotRemember : NavigationStackBehavior.Push);
        }
        /*
        public static void SearchKeyword(this PageManager pageManager, string content, bool isForgetNavigation = false)
        {
            var p = new NavigationParameters
            {
                { "keyword", content },
                { "target", SearchTarget.Keyword }
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultKeyword, p, isForgetNavigation);
        }

        public static void SearchTag(this PageManager pageManager, string content, bool isForgetNavigation = false)
        {
            var payload = new KeywordSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultTag, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchCommunity(this PageManager pageManager, string content, bool isForgetNavigation = false)
        {
            var payload = new KeywordSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultKeyword, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchMylist(this PageManager pageManager, string content, bool isForgetNavigation = false)
        {
            var payload = new MylistSearchPagePayloadContent()
            {
                Keyword = content,
                Order = order,
                Sort = sort,
            };

            pageManager.OpenPage(HohoemaPageType.SearchResultMylist, payload.ToParameterString(), isForgetNavigation);
        }

        public static void SearchCommunity(this PageManager pageManager, string content, bool isForgetNavigation = false)
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

        public static void SearchLive(this PageManager pageManager, string content, bool isForgetNavigation = false)
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
        */
    }
}

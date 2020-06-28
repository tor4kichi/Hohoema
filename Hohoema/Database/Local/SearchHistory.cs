using LiteDB;
using Hohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hohoema.Database
{
    public static class SearchHistoryDb
    {

        public static int Count()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.Query<SearchHistory>()
                    .Count();
            }
        }

        public static List<SearchHistory> GetAll()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.Query<SearchHistory>().ToList();
            }
        }

        public static List<SearchHistory> Get(int start, int count)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.Query<SearchHistory>().Skip(start).Limit(count).ToList();
            }
        }

        public static SearchTarget? LastSearchedTarget(string keyword)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                var searchHistory = db.Fetch<SearchHistory>(x => x.Keyword == keyword).OrderByDescending(x => x.LastUpdated).FirstOrDefault();
                return searchHistory?.Target;
            }
        }

        public static SearchHistory Searched(string keyword, SearchTarget target)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                var searchHistory = db.SingleOrDefault<SearchHistory>(x => x.Keyword == keyword && x.Target == target);
                if (searchHistory == null)
                {
                    searchHistory = new SearchHistory
                    {
                        Keyword = keyword,
                        Target = target,
                        LastUpdated = DateTime.Now,
                        SearchCount = 1
                    };
                }
                else
                {
                    searchHistory.LastUpdated = DateTime.Now;
                    searchHistory.SearchCount++;
                }

                db.Upsert<SearchHistory>(searchHistory);

                return searchHistory;
            }
        }

        public static bool Remove(string keyword, SearchTarget target)
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                return db.Delete<SearchHistory>(x => x.Keyword == keyword && x.Target == target) > 0;
            }
        }

        public static void Clear()
        {
            var db = HohoemaLiteDb.GetLocalLiteRepository();
            {
                db.Delete<SearchHistory>(Query.All());
            }
        }
    }

    public class SearchHistory
    {
        public ObjectId SearchHistoryId { get; set; }

        public string Keyword { get; set; }

        public SearchTarget Target { get; set; }

        public uint SearchCount { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}

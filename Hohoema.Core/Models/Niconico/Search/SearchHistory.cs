using Hohoema.Infra;
using Hohoema.Models.PageNavigation;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hohoema.Models.Niconico.Search;

public class SearchHistoryRepository : LiteDBServiceBase<SearchHistory>
{
    public SearchHistoryRepository(LiteDatabase liteDatabase) : base(liteDatabase)
    {
        _ = _collection.EnsureIndex(x => x.Keyword);
        _ = _collection.EnsureIndex(x => x.Target);
    }

    public List<SearchHistory> Get(int start, int count)
    {
        return _collection.Find(Query.All(), start, count).ToList();
    }

    public SearchTarget? LastSearchedTarget(string keyword)
    {
        return _collection
            .Find(x => x.Keyword == keyword)
            .OrderByDescending(x => x.LastUpdated)
            .FirstOrDefault()?.Target;
    }

    public SearchHistory Searched(string keyword, SearchTarget target)
    {
        SearchHistory searchHistory = _collection.FindOne(x => x.Keyword == keyword && x.Target == target);
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

        _ = _collection.Upsert(searchHistory);

        return searchHistory;
    }

    public bool Remove(string keyword, SearchTarget target)
    {
        return _collection.DeleteMany(x => x.Keyword == keyword && x.Target == target) > 0;
    }

    public void Clear()
    {
        _ = _collection.DeleteAll();
    }
}

public class SearchHistory
{
    [BsonId(autoId: true)]
    public ObjectId SearchHistoryId { get; set; }

    public string Keyword { get; set; }

    public SearchTarget Target { get; set; }

    public uint SearchCount { get; set; }

    public DateTime LastUpdated { get; set; }
}
